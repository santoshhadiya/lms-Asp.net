using System.Security.Claims;
using College_Library_Management_System.Services.Common;
using College_Library_Management_System.Models.Enums;
using College_Library_Management_System.Services.Interfaces;
using College_Library_Management_System.ViewModels.Circulation;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace College_Library_Management_System.Controllers
{
    [Authorize(Roles = "Admin,Librarian")]
    public class CirculationController : Controller
    {
        private readonly ILoanService _loanService;
        private readonly IPdfReportService _pdfReportService;
        private readonly IDateTimeProvider _dateTimeProvider;

        public CirculationController(
            ILoanService loanService,
            IPdfReportService pdfReportService,
            IDateTimeProvider dateTimeProvider)
        {
            _loanService = loanService;
            _pdfReportService = pdfReportService;
            _dateTimeProvider = dateTimeProvider;
        }

        [HttpGet]
        public async Task<IActionResult> ActiveLoans(ActiveLoanListFilterViewModel filter)
        {
            filter.DepartmentOptions = await _loanService.GetDepartmentOptionsAsync();
            var loans = await _loanService.GetActiveLoansAsync(filter);
            ViewBag.Filter = filter;
            return View(loans);
        }

        [HttpGet]
        public async Task<IActionResult> Issue(string? enrollment, string? accession)
        {
            var setting = await _loanService.GetLibrarySettingAsync();
            var model = new IssueBookViewModel
            {
                StudentEnrollment = enrollment ?? string.Empty,
                AccessionNumber = accession ?? string.Empty,
                LoanPeriodDays = setting.LoanPeriodDays
            };

            if (!string.IsNullOrWhiteSpace(enrollment) || !string.IsNullOrWhiteSpace(accession))
            {
                var check = await _loanService.CheckIssueEligibilityAsync(enrollment ?? string.Empty, accession ?? string.Empty);
                model.StudentId = check.StudentId;
                model.StudentName = check.StudentName;
                model.DepartmentName = check.DepartmentName;
                model.ActiveLoansCount = check.CurrentActiveLoans;
                model.HasOverdueLoans = check.HasOverdueLoans;
                model.BookCopyId = check.BookCopyId;
                model.BookTitle = check.BookTitle;
                model.ShelfLocation = check.ShelfLocation;
                model.CopyStatus = check.CopyStatus;
                if (!check.IsEligible)
                {
                    ModelState.AddModelError(string.Empty, check.Message);
                }
            }

            return View(model);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Issue(IssueBookViewModel model)
        {
            if (!ModelState.IsValid)
            {
                return View(model);
            }

            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier) ?? string.Empty;
            var userEmail = User.FindFirstValue(ClaimTypes.Email) ?? string.Empty;

            var (success, message, loanId) = await _loanService.IssueBookAsync(model, userId, userEmail);

            if (!success)
            {
                ModelState.AddModelError(string.Empty, message);
                // Reload eligibility for preview display
                var check = await _loanService.CheckIssueEligibilityAsync(model.StudentEnrollment, model.AccessionNumber);
                model.StudentName = check.StudentName;
                model.DepartmentName = check.DepartmentName;
                model.BookTitle = check.BookTitle;
                model.ShelfLocation = check.ShelfLocation;
                model.ActiveLoansCount = check.CurrentActiveLoans;
                return View(model);
            }

            TempData["Success"] = message;
            return RedirectToAction(nameof(ActiveLoans));
        }

        [HttpGet]
        public async Task<IActionResult> Return(int? id, string? accession)
        {
            ReturnBookViewModel? model = null;

            if (id.HasValue && id.Value > 0)
            {
                model = await _loanService.GetLoanForReturnAsync(id.Value);
            }
            else if (!string.IsNullOrWhiteSpace(accession))
            {
                model = await _loanService.GetActiveLoanByAccessionAsync(accession);
                if (model == null)
                {
                    TempData["Error"] = $"No active loan found for copy accession number '{accession}'.";
                }
            }

            return View(model);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Return(ReturnBookViewModel model)
        {
            if (!ModelState.IsValid)
            {
                return View(model);
            }

            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier) ?? string.Empty;
            var userEmail = User.FindFirstValue(ClaimTypes.Email) ?? string.Empty;

            var (success, message, fineAmount) = await _loanService.ReturnBookAsync(model, userId, userEmail);

            if (!success)
            {
                ModelState.AddModelError(string.Empty, message);
                return View(model);
            }

            TempData["Success"] = message;
            return RedirectToAction(nameof(ActiveLoans));
        }

        [HttpGet]
        public async Task<IActionResult> Renew(int id)
        {
            var model = await _loanService.GetLoanForRenewalAsync(id);
            if (model == null)
            {
                TempData["Error"] = "Loan not found.";
                return RedirectToAction(nameof(ActiveLoans));
            }

            return View(model);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> RenewConfirm(int loanId)
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier) ?? string.Empty;
            var userEmail = User.FindFirstValue(ClaimTypes.Email) ?? string.Empty;

            var (success, message, _) = await _loanService.RenewLoanAsync(loanId, userId, userEmail);

            if (!success)
            {
                TempData["Error"] = message;
                return RedirectToAction(nameof(Renew), new { id = loanId });
            }

            TempData["Success"] = message;
            return RedirectToAction(nameof(ActiveLoans));
        }

        [HttpGet]
        public async Task<IActionResult> CheckEligibility(string enrollment, string accession)
        {
            var result = await _loanService.CheckIssueEligibilityAsync(enrollment, accession);
            return Json(result);
        }

        [HttpGet]
        public async Task<IActionResult> History(LoanHistoryFilterViewModel filter)
        {
            filter.DepartmentOptions = await _loanService.GetDepartmentOptionsAsync();
            filter.Summary = await _loanService.GetLoanHistorySummaryAsync();
            var loans = await _loanService.GetLoanHistoryAsync(filter);
            ViewBag.Filter = filter;
            return View(loans);
        }

        [HttpGet]
        public async Task<IActionResult> ExportHistory(LoanHistoryFilterViewModel filter)
        {
            var csvBytes = await _loanService.ExportLoanHistoryCsvAsync(filter);
            var fileName = $"Circulation_History_{DateTime.Now:yyyyMMdd_HHmmss}.csv";
            return File(csvBytes, "text/csv", fileName);
        }

        [HttpGet]
        public async Task<IActionResult> ExportHistoryPdf(LoanHistoryFilterViewModel filter)
        {
            var exportFilter = new LoanHistoryFilterViewModel
            {
                SearchTerm = filter.SearchTerm,
                DepartmentId = filter.DepartmentId,
                Status = filter.Status,
                ActiveTab = filter.ActiveTab,
                FromDate = filter.FromDate,
                ToDate = filter.ToDate,
                DateFilterType = filter.DateFilterType,
                PageNumber = 1,
                PageSize = 2000
            };

            exportFilter.DepartmentOptions = await _loanService.GetDepartmentOptionsAsync();
            exportFilter.Summary = await _loanService.GetLoanHistorySummaryAsync();
            var loans = await _loanService.GetLoanHistoryAsync(exportFilter);

            var now = _dateTimeProvider.Now;
            var pdfBytes = _pdfReportService.GenerateCirculationHistoryPdf(loans, exportFilter, now);
            var fileName = $"Circulation_History_{now:yyyyMMdd_HHmmss}.pdf";
            return File(pdfBytes, "application/pdf", fileName);
        }
    }
}
