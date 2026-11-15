
import requests
from bs4 import BeautifulSoup
from urllib.parse import urljoin
from datetime import datetime
import time

BASE_URL = "http://localhost:5139"
LOGIN_URL = BASE_URL + "/Account/Login"

TEST_USERNAME = "user1"
WRONG_PASSWORD = "WrongPassword123!"

results = []


class LoginBot:
    def __init__(self):
        self.session = requests.Session()

    def get_form(self):
        response = self.session.get(LOGIN_URL)
        response.raise_for_status()

        soup = BeautifulSoup(response.text, "html.parser")
        form = soup.find("form")

        if form is None:
            raise Exception("Nie znaleziono formularza logowania")

        action = urljoin(LOGIN_URL, form.get("action", LOGIN_URL))
        data = {}

        # Pobieranie ukrytych pól formularza
        for field in form.find_all("input", {"type": "hidden"}):
            name = field.get("name")
            if name:
                data[name] = field.get("value", "")

        return action, data

    def login(self, username, password, trap="", captcha=None):
        action, data = self.get_form()

        data["UserName"] = username
        data["Password"] = password
        data["Website"] = trap

        # Opcjonalny token CAPTCHA
        if captcha is not None:
            data["g-recaptcha-response"] = captcha

        response = self.session.post(
            action,
            data=data,
            allow_redirects=True
        )

        return response


def record_result(name, success, details):
    results.append({
        "test": name,
        "result": "PASS" if success else "FAIL",
        "details": details
    })

    print(f"[{'PASS' if success else 'FAIL'}] {name}: {details}")


def test_lockout():
    print("\n=== TEST 1: LOCKOUT ===")

    bot = LoginBot()
    blocked = False

    # Wykonujemy 6 prób na dedykowanym koncie testowym
    for i in range(1, 7):
        print(f"Próba {i}/6")

        response = bot.login(TEST_USERNAME, WRONG_PASSWORD)

        if "zablok" in response.text.lower():
            blocked = True
            break

        time.sleep(0.5)

    record_result(
        "Limit błędnych logowań",
        blocked,
        "Wykryto komunikat o blokadzie" if blocked
        else "Nie wykryto komunikatu o blokadzie"
    )


def test_captcha():
    print("\n=== TEST 2: CAPTCHA ===")

    bot = LoginBot()

    # Test bez tokenu CAPTCHA
    response = bot.login(TEST_USERNAME, WRONG_PASSWORD)

    rejected = (
        "captcha" in response.text.lower()
        or "recaptcha" in response.text.lower()
    )

    record_result(
        "Brak tokenu CAPTCHA",
        rejected,
        "Aplikacja zwróciła komunikat CAPTCHA" if rejected
        else "Nie wykryto komunikatu CAPTCHA"
    )

    # Test z nieprawidłowym tokenem
    bot = LoginBot()

    response = bot.login(
        TEST_USERNAME,
        WRONG_PASSWORD,
        captcha="invalid-token"
    )

    rejected = (
        "captcha" in response.text.lower()
        or "recaptcha" in response.text.lower()
    )

    record_result(
        "Nieprawidłowy token CAPTCHA",
        rejected,
        "Aplikacja odrzuciła token" if rejected
        else "Nie wykryto odrzucenia CAPTCHA"
    )


def test_trap():
    print("\n=== TEST 3: TRAP ===")

    bot = LoginBot()

    response = bot.login(
        TEST_USERNAME,
        WRONG_PASSWORD,
        trap="I am a bot"
    )

    print(response.status_code)
    print("I am a bot" in response.text)

    # Weryfikacja na podstawie oczekiwanego komunikatu
    rejected = "i am a bot" in response.text.lower()

    record_result(
        "Wypełnione pole pułapki",
        rejected,
        "Aplikacja wykryła pułapkę" if rejected
        else "Nie wykryto komunikatu pułapki"
    )


def generate_report():
    print("\n========== RAPORT ==========")
    print("Data:", datetime.now().strftime("%Y-%m-%d %H:%M:%S"))

    for result in results:
        print(
            f"{result['test']}: "
            f"{result['result']} - {result['details']}"
        )

    passed = sum(1 for r in results if r["result"] == "PASS")

    print(f"\nWynik: {passed}/{len(results)} testów zaliczonych")


if __name__ == "__main__":
    print("BOT TESTUJĄCY ZABEZPIECZENIA APLIKACJI")
    print("Adres:", LOGIN_URL)

    test_lockout()
    test_captcha()
    test_trap()

    generate_report()
