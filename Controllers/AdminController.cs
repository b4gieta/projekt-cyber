using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using projekt_cyber.Models;
using projekt_cyber.ViewModels;
using System.Security.Cryptography;

namespace projekt_cyber.Controllers
{
    [Authorize(Roles = "Administrator")]
    public class AdminController : Controller
    {
        private readonly UserManager<ApplicationUser> _userManager;

        public AdminController(UserManager<ApplicationUser> userManager)
        {
            _userManager = userManager;
        }

        [HttpGet]
        public async Task<IActionResult> Index()
        {
            var users = _userManager.Users.ToList();
            var model = new List<AdminUserRowViewModel>();

            foreach (var user in users)
            {
                model.Add(new AdminUserRowViewModel
                {
                    UserName = user.UserName ?? "",
                    Email = user.Email ?? "",
                    Role = string.Join(", ", await _userManager.GetRolesAsync(user)),
                    IsBlocked = user.IsBlocked,
                    MustChangePassword = user.MustChangePassword
                });
            }

            return View(model);
        }

        [HttpGet]
        public IActionResult Create() => View(new CreateUserViewModel());

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(CreateUserViewModel model)
        {
            if (model.Role != "Redaktor" && model.Role != "Czytelnik")
                ModelState.AddModelError(nameof(model.Role), "Nieprawidłowa rola.");

            if (!ModelState.IsValid) return View(model);

            var temporaryPassword = GenerateOneTimePassword();

            var user = new ApplicationUser
            {
                UserName = model.UserName.Trim(),
                Email = model.Email.Trim(),
                EmailConfirmed = true,
                MustChangePassword = true,
                OneTimePasswordExpiresAt = DateTime.UtcNow.AddHours(24)
            };

            var result = await _userManager.CreateAsync(user, temporaryPassword);

            if (!result.Succeeded)
            {
                foreach (var error in result.Errors) ModelState.AddModelError("", error.Description);
                return View(model);
            }

            var roleResult = await _userManager.AddToRoleAsync(user, model.Role);

            if (!roleResult.Succeeded)
            {
                await _userManager.DeleteAsync(user);

                foreach (var error in roleResult.Errors) ModelState.AddModelError("", error.Description);

                return View(model);
            }

            return View("Created", new CreatedUserViewModel
            {
                UserName = user.UserName!,
                OneTimePassword = temporaryPassword,
                ExpiresAt = user.OneTimePasswordExpiresAt.Value
            });
        }

        private static string GenerateOneTimePassword()
        {
            const string lower = "abcdefghijklmnopqrstuvwxyz";
            const string upper = "ABCDEFGHIJKLMNOPQRSTUVWXYZ";
            const string digits = "0123456789";
            const string all = lower + upper + digits;

            var password = new char[12];

            password[0] = lower[RandomNumberGenerator.GetInt32(lower.Length)];
            password[1] = upper[RandomNumberGenerator.GetInt32(upper.Length)];
            password[2] = digits[RandomNumberGenerator.GetInt32(digits.Length)];

            for (var i = 3; i < password.Length; i++)
                password[i] = all[RandomNumberGenerator.GetInt32(all.Length)];

            for (var i = password.Length - 1; i > 0; i--)
            {
                var j = RandomNumberGenerator.GetInt32(i + 1);
                (password[i], password[j]) = (password[j], password[i]);
            }

            return new string(password);
        }
    }
}
