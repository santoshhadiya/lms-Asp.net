using College_Library_Management_System.Services.Common;
using College_Library_Management_System.Services.Interfaces;
using College_Library_Management_System.ViewModels.Reports;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace College_Library_Management_System.Controllers
{
    [Authorize(Roles = "Admin,Librarian")]
    public class ReportsController : Controller
    {
        private readonly IReportService _reportService;
        private readonly IPdfReportService _pdfReportService;
        private readonly IDateTimeProvider _dateTimeProvider;

        public ReportsController(
            IReportService reportService,
            IPdfReportService pdfReportService,
            IDateTimeProvider dateTimeProvider)
        {
            _reportService = reportService;
            _pdfReportService = pdfReportService;
            _dateTimeProvider = dateTimeProvider;
        }

        public IActionResult Index()
        {
            return View();
        }

        [HttpGet]
        public async Task<IActionResult> Overdue(OverdueReportFilterViewModel filter)
        {
            var model = await _reportService.GetOverdueReportAsync(filter);
            return View(model);
        }

        [HttpGet]
        public async Task<IActionResult> ExportOverduePdf(OverdueReportFilterViewModel filter)
        {
            var report = await _reportService.GetOverdueReportAsync(filter);
            var now = _dateTimeProvider.Now;
            var pdfBytes = _pdfReportService.GenerateOverduePdf(report, now);
            var fileName = $"Overdue_Report_{now:yyyyMMdd_HHmmss}.pdf";
            return File(pdfBytes, "application/pdf", fileName);
        }

        [HttpGet]
        public async Task<IActionResult> Inventory()
        {
            var model = await _reportService.GetInventoryReportAsync();
            return View(model);
        }

        [HttpGet]
        public async Task<IActionResult> ExportInventoryPdf()
        {
            var report = await _reportService.GetInventoryReportAsync();
            var now = _dateTimeProvider.Now;
            var pdfBytes = _pdfReportService.GenerateInventoryPdf(report, now);
            var fileName = $"Inventory_Status_Report_{now:yyyyMMdd_HHmmss}.pdf";
            return File(pdfBytes, "application/pdf", fileName);
        }

        [HttpGet]
        public async Task<IActionResult> Collections(CollectionReportFilterViewModel filter)
        {
            var model = await _reportService.GetCollectionReportAsync(filter);
            return View(model);
        }

        [HttpGet]
        public async Task<IActionResult> ExportCollectionsPdf(CollectionReportFilterViewModel filter)
        {
            var report = await _reportService.GetCollectionReportAsync(filter);
            var now = _dateTimeProvider.Now;
            var pdfBytes = _pdfReportService.GenerateCollectionsPdf(report, now);
            var fileName = $"Collections_Report_{now:yyyyMMdd_HHmmss}.pdf";
            return File(pdfBytes, "application/pdf", fileName);
        }
    }
}

