using College_Library_Management_System.ViewModels.Circulation;
using College_Library_Management_System.ViewModels.Common;
using College_Library_Management_System.ViewModels.Fines;
using College_Library_Management_System.ViewModels.Reports;

namespace College_Library_Management_System.Services.Interfaces
{
    public interface IPdfReportService
    {
        byte[] GenerateCollectionsPdf(CollectionReportFilterViewModel report, DateTime generatedAt);
        byte[] GenerateOverduePdf(OverdueReportFilterViewModel report, DateTime generatedAt);
        byte[] GenerateInventoryPdf(InventoryReportViewModel report, DateTime generatedAt);
        byte[] GenerateFineReceiptPdf(FineReceiptViewModel receipt, DateTime generatedAt);
        byte[] GenerateCirculationHistoryPdf(PaginatedList<LoanHistoryItemViewModel> items, LoanHistoryFilterViewModel filter, DateTime generatedAt);
    }
}
