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

        public ReportsController(IReportService reportService)
        {
            _reportService = reportService;
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
        public async Task<IActionResult> Inventory()
        {
            var model = await _reportService.GetInventoryReportAsync();
            return View(model);
        }

        [HttpGet]
        public async Task<IActionResult> Collections(CollectionReportFilterViewModel filter)
        {
            var model = await _reportService.GetCollectionReportAsync(filter);
            return View(model);
        }

        [HttpGet]
        public async Task<IActionResult> AuditLogs(AuditLogReportFilterViewModel filter)
        {
            filter.Logs = await _reportService.GetAuditLogsAsync(filter);
            return View(filter);
        }
    }
}
