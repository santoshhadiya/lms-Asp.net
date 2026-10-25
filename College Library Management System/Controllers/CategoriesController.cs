using System.Security.Claims;
using College_Library_Management_System.Services.Interfaces;
using College_Library_Management_System.ViewModels.MasterData;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace College_Library_Management_System.Controllers
{
    [Authorize(Roles = "Admin,Librarian")]
    public class CategoriesController : Controller
    {
        private readonly IMasterDataService _masterDataService;

        public CategoriesController(IMasterDataService masterDataService)
        {
            _masterDataService = masterDataService;
        }

        public async Task<IActionResult> Index()
        {
            var categories = await _masterDataService.GetAllCategoriesAsync();
            return View(categories);
        }

        [HttpGet]
        public IActionResult Create()
        {
            return View(new CategoryViewModel());
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(CategoryViewModel model)
        {
            if (!ModelState.IsValid) return View(model);

            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            var userEmail = User.FindFirstValue(ClaimTypes.Email);

            var (success, message, _) = await _masterDataService.CreateCategoryAsync(model, userId, userEmail);
            if (!success)
            {
                ModelState.AddModelError(string.Empty, message);
                return View(model);
            }

            TempData["Success"] = message;
            return RedirectToAction(nameof(Index));
        }

        [HttpGet]
        public async Task<IActionResult> Edit(int id)
        {
            var model = await _masterDataService.GetCategoryForEditAsync(id);
            if (model == null)
            {
                TempData["Error"] = "Category not found.";
                return RedirectToAction(nameof(Index));
            }
            return View(model);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(CategoryViewModel model)
        {
            if (!ModelState.IsValid) return View(model);

            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            var userEmail = User.FindFirstValue(ClaimTypes.Email);

            var (success, message) = await _masterDataService.UpdateCategoryAsync(model, userId, userEmail);
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
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> Delete(int id)
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            var userEmail = User.FindFirstValue(ClaimTypes.Email);

            var (success, message) = await _masterDataService.DeleteCategoryAsync(id, userId, userEmail);
            if (!success)
            {
                TempData["Error"] = message;
            }
            else
            {
                TempData["Success"] = message;
            }

            return RedirectToAction(nameof(Index));
        }
    }
}
