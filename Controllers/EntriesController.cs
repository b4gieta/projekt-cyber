using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using projekt_cyber.Data;
using projekt_cyber.Models;
using projekt_cyber.ViewModels;

namespace projekt_cyber.Controllers
{
    [Authorize(Roles = "Administrator,Redaktor")]
    public class EntriesController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly UserManager<ApplicationUser> _userManager;

        public EntriesController(ApplicationDbContext context, UserManager<ApplicationUser> userManager)
        {
            _context = context;
            _userManager = userManager;
        }

        [HttpGet]
        public IActionResult Create()
        {
            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(EntryCreateViewModel model)
        {
            if (!ModelState.IsValid)
                return View(model);

            var user = await _userManager.GetUserAsync(User);

            if (user == null)
                return Challenge();

            var entry = new Entry
            {
                Title = model.Title,
                Content = model.Content,
                CreatedAt = DateTime.UtcNow,
                AuthorId = user.Id
            };

            _context.Entries.Add(entry);
            await _context.SaveChangesAsync();

            TempData["Message"] = "Dodano wpis.";
            return RedirectToAction("Index", "Home");
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Delete(int id)
        {
            var entry = await _context.Entries.FindAsync(id);

            if (entry == null)
                return NotFound();

            _context.Entries.Remove(entry);
            await _context.SaveChangesAsync();

            TempData["Message"] = "Usunięto wpis.";
            return RedirectToAction("Index", "Home");
        }
    }
}
