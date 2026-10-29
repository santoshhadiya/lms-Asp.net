using College_Library_Management_System.ViewModels.Common;
using College_Library_Management_System.ViewModels.Reports;

namespace College_Library_Management_System.Services.Interfaces
{
    public interface IReportService
    {
        Task<DashboardViewModel> GetDashboardMetricsAsync();
        Task<OverdueReportFilterViewModel> GetOverdueReportAsync(OverdueReportFilterViewModel filter);
        Task<InventoryReportViewModel> GetInventoryReportAsync();
        Task<CollectionReportFilterViewModel> GetCollectionReportAsync(CollectionReportFilterViewModel filter);
    }
}
