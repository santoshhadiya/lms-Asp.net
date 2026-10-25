using System.Security.Claims;
using College_Library_Management_System.Services.Interfaces;
using College_Library_Management_System.ViewModels.MasterData;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace College_Library_Management_System.Controllers
{
    [Authorize(Roles = "Admin,Librarian")]
    public class PublishersController : Controller
    {
        private readonly IMasterDataService _masterDataService;

        public PublishersController(IMasterDataService masterDataService)
        {
            _masterDataService = masterDataService;
        }

        public async Task<IActionResult> Index()
        {
            var publishers = await _masterDataService.GetAllPublishersAsync();
            return View(publishers);
        }

        [HttpGet]
        public IActionResult Create()
        {
            return View(new PublisherViewModel());
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(PublisherViewModel model)
        {
            if (!ModelState.IsValid) return View(model);

            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            var userEmail = User.FindFirstValue(ClaimTypes.Email);

            var (success, message, _) = await _masterDataService.CreatePublisherAsync(model, userId, userEmail);
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
            var model = await _masterDataService.GetPublisherForEditAsync(id);
            if (model == null)
            {
                TempData["Error"] = "Publisher not found.";
                return RedirectToAction(nameof(Index));
            }
            return View(model);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(PublisherViewModel model)
        {
            if (!ModelState.IsValid) return View(model);

            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            var userEmail = User.FindFirstValue(ClaimTypes.Email);

            var (success, message) = await _masterDataService.UpdatePublisherAsync(model, userId, userEmail);
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

            var (success, message) = await _masterDataService.DeletePublisherAsync(id, userId, userEmail);
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
