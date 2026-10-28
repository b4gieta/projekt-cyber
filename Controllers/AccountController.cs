using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using projekt_cyber.Data;
using projekt_cyber.Models;
using projekt_cyber.Services;
using projekt_cyber.ViewModels;

namespace projekt_cyber.Controllers
{
    public class AccountController : Controller
    {
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly SignInManager<ApplicationUser> _signInManager;
        private readonly ApplicationDbContext _context;

        public AccountController(UserManager<ApplicationUser> userManager, SignInManager<ApplicationUser> signInManager, ApplicationDbContext context)
        {
            _userManager = userManager;
            _signInManager = signInManager;
            _context = context;
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
                    if (user.OneTimePasswordExpiresAt.HasValue && user.OneTimePasswordExpiresAt.Value <= DateTime.UtcNow)
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

            if (user.OneTimePasswordExpiresAt.HasValue && user.OneTimePasswordExpiresAt.Value <= DateTime.UtcNow)
            {
                await _signInManager.SignOutAsync();
                return RedirectToAction(nameof(Login));
            }

            if (await HasInvalidPasswordAsync(model.NewPassword))
            {
                ModelState.AddModelError(nameof(model.NewPassword), "Hasło nie może zawierać trzech identycznych znaków pod rząd.");
                return View(model);
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

        [Authorize]
        [HttpGet]
        public async Task<IActionResult> ChangePassword()
        {
            var user = await _userManager.GetUserAsync(User);

            if (user == null) return Challenge();
            if (user.MustChangePassword) return RedirectToAction(nameof(ChangeInitialPassword));

            return View(new ChangePasswordViewModel());
        }

        [Authorize]
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ChangePassword(ChangePasswordViewModel model)
        {
            if (!ModelState.IsValid) return View(model);

            var user = await _userManager.GetUserAsync(User);

            if (user == null) return Challenge();
            if (user.MustChangePassword) return RedirectToAction(nameof(ChangeInitialPassword));

            if (await HasInvalidPasswordAsync(model.NewPassword))
            {
                ModelState.AddModelError(nameof(model.NewPassword), "Hasło nie może zawierać trzech identycznych znaków pod rząd.");
                return View(model);
            }

            var result = await _userManager.ChangePasswordAsync(user, model.CurrentPassword, model.NewPassword);

            if (!result.Succeeded)
            {
                foreach (var error in result.Errors)
                {
                    if (error.Code == "PasswordMismatch")
                        ModelState.AddModelError(nameof(model.CurrentPassword), "Aktualne hasło jest nieprawidłowe.");
                    else
                        ModelState.AddModelError(nameof(model.NewPassword), error.Description);
                }

                return View(model);
            }

            await _signInManager.RefreshSignInAsync(user);

            TempData["SuccessMessage"] = "Hasło zostało zmienione.";
            return RedirectToAction("Index", "Home");
        }

        private async Task<bool> HasInvalidPasswordAsync(string password)
        {
            var settings = await _context.PasswordPolicySettings.FindAsync(1);

            return settings?.RejectThreeRepeatedChars == true &&
                   PasswordPolicyValidator.HasThreeRepeatedChars(password);
        }
    }
}
