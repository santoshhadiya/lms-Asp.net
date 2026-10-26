using System.Security.Claims;
using College_Library_Management_System.Services.Interfaces;
using College_Library_Management_System.ViewModels.Librarians;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace College_Library_Management_System.Controllers
{
    [Authorize(Roles = "Admin")]
    public class LibrariansController : Controller
    {
        private readonly ILibrarianService _librarianService;

        public LibrariansController(ILibrarianService librarianService)
        {
            _librarianService = librarianService;
        }

        [HttpGet]
        public async Task<IActionResult> Index(string? searchTerm, bool? isActive)
        {
            ViewData["SearchTerm"] = searchTerm;
            ViewData["IsActive"] = isActive;

            var librarians = await _librarianService.GetLibrariansAsync(searchTerm, isActive);
            return View(librarians);
        }

        [HttpGet]
        public IActionResult Create()
        {
            return View(new LibrarianFormViewModel { IsActive = true });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(LibrarianFormViewModel model)
        {
            if (!ModelState.IsValid)
            {
                return View(model);
            }

            var adminUserId = User.FindFirstValue(ClaimTypes.NameIdentifier) ?? string.Empty;
            var adminEmail = User.FindFirstValue(ClaimTypes.Email) ?? string.Empty;

            var (success, message, tempPassword) = await _librarianService.CreateLibrarianAsync(model, adminUserId, adminEmail);

            if (!success)
            {
                ModelState.AddModelError(string.Empty, message);
                return View(model);
            }

            TempData["Success"] = message;
            TempData["GeneratedPassword"] = tempPassword;
            TempData["LibrarianEmail"] = model.Email;

            return RedirectToAction(nameof(Index));
        }

        [HttpGet]
        public async Task<IActionResult> Edit(string id)
        {
            var model = await _librarianService.GetLibrarianForEditAsync(id);
            if (model == null)
            {
                TempData["Error"] = "Librarian record not found.";
                return RedirectToAction(nameof(Index));
            }

            return View(model);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(LibrarianFormViewModel model)
        {
            if (!ModelState.IsValid)
            {
                return View(model);
            }

            var adminUserId = User.FindFirstValue(ClaimTypes.NameIdentifier) ?? string.Empty;
            var adminEmail = User.FindFirstValue(ClaimTypes.Email) ?? string.Empty;

            var (success, message) = await _librarianService.UpdateLibrarianAsync(model, adminUserId, adminEmail);

            if (!success)
            {
                ModelState.AddModelError(string.Empty, message);
                return View(model);
            }

            TempData["Success"] = message;
            return RedirectToAction(nameof(Index));
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ToggleStatus(string id)
        {
            var adminUserId = User.FindFirstValue(ClaimTypes.NameIdentifier) ?? string.Empty;
            var adminEmail = User.FindFirstValue(ClaimTypes.Email) ?? string.Empty;

            var (success, message) = await _librarianService.ToggleLibrarianStatusAsync(id, adminUserId, adminEmail);

            if (success)
            {
                TempData["Success"] = message;
            }
            else
            {
                TempData["Error"] = message;
            }

            return RedirectToAction(nameof(Index));
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ResetPassword(string id, string? newPassword)
        {
            var adminUserId = User.FindFirstValue(ClaimTypes.NameIdentifier) ?? string.Empty;
            var adminEmail = User.FindFirstValue(ClaimTypes.Email) ?? string.Empty;

            var (success, message, generatedPassword) = await _librarianService.ResetLibrarianPasswordAsync(id, newPassword, adminUserId, adminEmail);

            if (success)
            {
                TempData["Success"] = message;
                TempData["GeneratedPassword"] = generatedPassword;
            }
            else
            {
                TempData["Error"] = message;
            }

            return RedirectToAction(nameof(Index));
        }
    }
}
