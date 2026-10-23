using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using projekt_cyber.Data;
using projekt_cyber.Models;
using projekt_cyber.ViewModels;
using System.Security.Cryptography;

namespace projekt_cyber.Controllers
{
    [Authorize(Roles = "Administrator")]
    public class AdminController : Controller
    {
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly ApplicationDbContext _context;

        public AdminController(UserManager<ApplicationUser> userManager, ApplicationDbContext context)
        {
            _userManager = userManager;
            _context = context;
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
                    Id = user.Id,
                    UserName = user.UserName ?? "",
                    Email = user.Email ?? "",
                    Role = string.Join(", ", await _userManager.GetRolesAsync(user)),
                    IsBlocked = user.IsBlocked,
                    MustChangePassword = user.MustChangePassword,
                    PasswordExpiresAt = user.PasswordExpiresAt
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
            if (model.Role != "Redaktor" && model.Role != "Czytelnik") ModelState.AddModelError(nameof(model.Role), "Nieprawidłowa rola.");

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

            for (var i = 3; i < password.Length; i++) password[i] = all[RandomNumberGenerator.GetInt32(all.Length)];

            for (var i = password.Length - 1; i > 0; i--)
            {
                var j = RandomNumberGenerator.GetInt32(i + 1);
                (password[i], password[j]) = (password[j], password[i]);
            }

            return new string(password);
        }

        [HttpGet]
        public async Task<IActionResult> Edit(string id)
        {
            var user = await _userManager.FindByIdAsync(id);

            if (user == null) return NotFound();

            return View(new EditUserViewModel
            {
                Id = user.Id,
                UserName = user.UserName ?? "",
                Email = user.Email ?? ""
            });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(EditUserViewModel model)
        {
            if (!ModelState.IsValid) return View(model);

            var user = await _userManager.FindByIdAsync(model.Id);

            if (user == null) return NotFound();

            var result = await _userManager.SetUserNameAsync(user, model.UserName.Trim());

            if (!result.Succeeded)
            {
                foreach (var error in result.Errors)
                    ModelState.AddModelError(nameof(model.UserName), error.Description);

                return View(model);
            }

            var emailResult = await _userManager.SetEmailAsync(user, model.Email);

            if (!emailResult.Succeeded)
            {
                foreach (var error in emailResult.Errors)
                    ModelState.AddModelError(nameof(model.Email), error.Description);

                return View(model);
            }

            TempData["SuccessMessage"] = "Login został zmieniony.";
            return RedirectToAction(nameof(Index));
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ToggleBlock(string id)
        {
            var user = await _userManager.FindByIdAsync(id);

            if (user == null) return NotFound();

            var currentUser = await _userManager.GetUserAsync(User);

            if (currentUser?.Id == user.Id)
            {
                TempData["ErrorMessage"] = "Nie możesz zablokować własnego konta.";
                return RedirectToAction(nameof(Index));
            }

            if (!user.IsBlocked && await IsLastAdministratorAsync(user))
            {
                TempData["ErrorMessage"] = "Nie można zablokować ostatniego administratora.";
                return RedirectToAction(nameof(Index));
            }

            user.IsBlocked = !user.IsBlocked;

            var result = await _userManager.UpdateAsync(user);

            TempData[result.Succeeded ? "SuccessMessage" : "ErrorMessage"] =
                result.Succeeded
                    ? (user.IsBlocked ? "Konto zostało zablokowane." : "Konto zostało odblokowane.")
                    : "Nie udało się zmienić statusu konta.";

            return RedirectToAction(nameof(Index));
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Delete(string id)
        {
            var user = await _userManager.FindByIdAsync(id);

            if (user == null) return NotFound();

            var currentUser = await _userManager.GetUserAsync(User);

            if (currentUser?.Id == user.Id)
            {
                TempData["ErrorMessage"] = "Nie możesz usunąć własnego konta.";
                return RedirectToAction(nameof(Index));
            }

            if (await IsLastAdministratorAsync(user))
            {
                TempData["ErrorMessage"] = "Nie można usunąć ostatniego administratora.";
                return RedirectToAction(nameof(Index));
            }

            var result = await _userManager.DeleteAsync(user);

            TempData[result.Succeeded ? "SuccessMessage" : "ErrorMessage"] =
                result.Succeeded ? "Użytkownik został usunięty." : "Nie udało się usunąć użytkownika.";

            return RedirectToAction(nameof(Index));
        }

        private async Task<bool> IsLastAdministratorAsync(ApplicationUser user)
        {
            if (!await _userManager.IsInRoleAsync(user, "Administrator")) return false;

            var admins = await _userManager.GetUsersInRoleAsync("Administrator");
            return admins.Count == 1;
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ForcePasswordChange(string id)
        {
            var user = await _userManager.FindByIdAsync(id);

            if (user == null) return NotFound();

            user.MustChangePassword = true;
            user.OneTimePasswordExpiresAt = null;

            var result = await _userManager.UpdateAsync(user);

            TempData[result.Succeeded ? "SuccessMessage" : "ErrorMessage"] =
                result.Succeeded
                    ? $"Wymuszono zmianę hasła dla użytkownika {user.UserName}."
                    : "Nie udało się wymusić zmiany hasła.";

            return RedirectToAction(nameof(Index));
        }

        [HttpGet]
        public async Task<IActionResult> PasswordPolicy()
        {
            var settings = await _context.PasswordPolicySettings.FindAsync(1);

            if (settings == null)
            {
                settings = new PasswordPolicySettings();
                _context.PasswordPolicySettings.Add(settings);
                await _context.SaveChangesAsync();
            }

            return View(new PasswordPolicyViewModel
            {
                RejectThreeRepeatedChars = settings.RejectThreeRepeatedChars,
                IdleTimeoutMinutes = settings.IdleTimeoutMinutes
            });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> PasswordPolicy(PasswordPolicyViewModel model)
        {
            var settings = await _context.PasswordPolicySettings.FindAsync(1);

            if (settings == null)
            {
                settings = new PasswordPolicySettings();
                _context.PasswordPolicySettings.Add(settings);
            }

            settings.RejectThreeRepeatedChars = model.RejectThreeRepeatedChars;
            settings.IdleTimeoutMinutes = model.IdleTimeoutMinutes;
            await _context.SaveChangesAsync();

            TempData["SuccessMessage"] = "Zapisano ustawienia polityki haseł.";
            return RedirectToAction(nameof(PasswordPolicy));
        }
    }
}
