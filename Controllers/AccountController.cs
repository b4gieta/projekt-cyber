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
                if (user.MustChangePassword)
                {
                    if (user.OneTimePasswordExpiresAt == null || user.OneTimePasswordExpiresAt <= DateTime.UtcNow)
                    {
                        await _signInManager.SignOutAsync();
                        ModelState.AddModelError("", "Hasło jednorazowe wygasło. Skontaktuj się z administratorem.");
                        return View(model);
                    }

                    return RedirectToAction(nameof(ChangeInitialPassword));
                }

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

        [Authorize]
        [HttpGet]
        public async Task<IActionResult> ChangeInitialPassword()
        {
            var user = await _userManager.GetUserAsync(User);

            if (user == null) return Challenge();
            if (!user.MustChangePassword) return RedirectToAction("Index", "Home");

            return View(new ChangeInitialPasswordViewModel());
        }

        [Authorize]
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ChangeInitialPassword(ChangeInitialPasswordViewModel model)
        {
            if (!ModelState.IsValid) return View(model);

            var user = await _userManager.GetUserAsync(User);

            if (user == null) return Challenge();
            if (!user.MustChangePassword) return RedirectToAction("Index", "Home");

            if (user.OneTimePasswordExpiresAt == null || user.OneTimePasswordExpiresAt <= DateTime.UtcNow)
            {
                await _signInManager.SignOutAsync();
                return RedirectToAction(nameof(Login));
            }

            var token = await _userManager.GeneratePasswordResetTokenAsync(user);
            var result = await _userManager.ResetPasswordAsync(user, token, model.NewPassword);

            if (!result.Succeeded)
            {
                foreach (var error in result.Errors) ModelState.AddModelError("", error.Description);
                return View(model);
            }

            user.MustChangePassword = false;
            user.OneTimePasswordExpiresAt = null;

            await _userManager.UpdateAsync(user);
            await _signInManager.RefreshSignInAsync(user);

            return RedirectToAction("Index", "Home");
        }
    }
}
