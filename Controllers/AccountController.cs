using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using projekt_cyber.Models;
using projekt_cyber.ViewModels;

namespace projekt_cyber.Controllers
{
    public class AccountController : Controller
    {
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly SignInManager<ApplicationUser> _signInManager;

        public AccountController(UserManager<ApplicationUser> userManager, SignInManager<ApplicationUser> signInManager)
        {
            _userManager = userManager;
            _signInManager = signInManager;
        }

        [AllowAnonymous]
        [HttpGet]
        public IActionResult Register() => View();

        [AllowAnonymous]
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Register(RegisterViewModel model)
        {
            if (!ModelState.IsValid) return View(model);

            var user = new ApplicationUser
            {
                UserName = model.UserName,
                Email = model.Email,
                MustChangePassword = false
            };

            var result = await _userManager.CreateAsync(user, model.Password);

            if (!result.Succeeded)
            {
                foreach (var error in result.Errors) ModelState.AddModelError("", error.Description);
                return View(model);
            }

            var roleResult = await _userManager.AddToRoleAsync(user, "Czytelnik");

            if (!roleResult.Succeeded)
            {
                await _userManager.DeleteAsync(user);
                ModelState.AddModelError("", "Nie udało się utworzyć konta.");
                return View(model);
            }

            await _signInManager.SignInAsync(user, isPersistent: false);
            return RedirectToAction("Index", "Home");
        }

        [AllowAnonymous]
        [HttpGet]
        public IActionResult Login(string? returnUrl = null)
        {
            ViewData["ReturnUrl"] = returnUrl;
            return View();
        }

        [AllowAnonymous]
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Login(LoginViewModel model, string? returnUrl = null)
        {
            ViewData["ReturnUrl"] = returnUrl;

            if (!ModelState.IsValid) return View(model);

            var user = await _userManager.FindByNameAsync(model.UserName);

            if (user == null || user.IsBlocked)
            {
                ModelState.AddModelError("", "Login lub hasło niepoprawne");
                return View(model);
            }

            var result = await _signInManager.PasswordSignInAsync(
                model.UserName,
                model.Password,
                isPersistent: false,
                lockoutOnFailure: true);

            if (result.Succeeded)
            {
                if (!string.IsNullOrEmpty(returnUrl) && Url.IsLocalUrl(returnUrl)) return LocalRedirect(returnUrl);
                return RedirectToAction("Index", "Home");
            }

            ModelState.AddModelError("", "Login lub hasło niepoprawne");
            return View(model);
        }

        [Authorize]
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Logout()
        {
            await _signInManager.SignOutAsync();
            return RedirectToAction("Login");
        }

        [AllowAnonymous]
        public IActionResult AccessDenied() => Content("Brak uprawnień.");
    }
}
