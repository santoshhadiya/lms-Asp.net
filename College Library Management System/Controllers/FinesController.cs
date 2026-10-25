using System.Security.Claims;
using College_Library_Management_System.Services.Interfaces;
using College_Library_Management_System.ViewModels.Fines;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace College_Library_Management_System.Controllers
{
    [Authorize(Roles = "Admin,Librarian")]
    public class FinesController : Controller
    {
        private readonly IFineService _fineService;

        public FinesController(IFineService fineService)
        {
            _fineService = fineService;
        }

        [HttpGet]
        public async Task<IActionResult> Index(FineListFilterViewModel filter)
        {
            filter.DepartmentOptions = await _fineService.GetDepartmentOptionsAsync();
            var fines = await _fineService.GetFinesAsync(filter);
            ViewBag.Filter = filter;
            return View(fines);
        }

        [HttpGet]
        public async Task<IActionResult> CollectPayment(int id)
        {
            var model = await _fineService.GetFineForPaymentAsync(id);
            if (model == null)
            {
                TempData["Error"] = "Fine record not found.";
                return RedirectToAction(nameof(Index));
            }

            if (model.RemainingBalance <= 0)
            {
                TempData["Error"] = "This fine has no remaining balance to collect.";
                return RedirectToAction(nameof(Index));
            }

            return View(model);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> CollectPayment(CollectFinePaymentViewModel model)
        {
            if (!ModelState.IsValid)
            {
                return View(model);
            }

            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier) ?? string.Empty;
            var userEmail = User.FindFirstValue(ClaimTypes.Email) ?? string.Empty;

            var (success, message, receiptNumber) = await _fineService.CollectFullPaymentAsync(model, userId, userEmail);

            if (!success)
            {
                ModelState.AddModelError(string.Empty, message);
                return View(model);
            }

            TempData["Success"] = message;
            return RedirectToAction(nameof(Receipt), new { receiptNumber });
        }

        [HttpGet]
        public async Task<IActionResult> Receipt(string receiptNumber)
        {
            if (string.IsNullOrWhiteSpace(receiptNumber))
            {
                TempData["Error"] = "Invalid receipt number.";
                return RedirectToAction(nameof(Index));
            }

            var receipt = await _fineService.GetReceiptByNumberAsync(receiptNumber);
            if (receipt == null)
            {
                TempData["Error"] = $"Receipt #{receiptNumber} not found.";
                return RedirectToAction(nameof(Index));
            }

            return View(receipt);
        }

        [HttpGet]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> Waive(int id)
        {
            var model = await _fineService.GetFineForWaiverAsync(id);
            if (model == null)
            {
                TempData["Error"] = "Fine record not found.";
                return RedirectToAction(nameof(Index));
            }

            if (model.RemainingBalance <= 0)
            {
                TempData["Error"] = "This fine has no remaining balance to waive.";
                return RedirectToAction(nameof(Index));
            }

            return View(model);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> Waive(WaiveFineViewModel model)
        {
            if (!ModelState.IsValid)
            {
                return View(model);
            }

            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier) ?? string.Empty;
            var userEmail = User.FindFirstValue(ClaimTypes.Email) ?? string.Empty;

            var (success, message) = await _fineService.WaiveFineAsync(model, userId, userEmail);

            if (!success)
            {
                ModelState.AddModelError(string.Empty, message);
                return View(model);
            }

            TempData["Success"] = message;
            return RedirectToAction(nameof(Index));
        }
    }
}
