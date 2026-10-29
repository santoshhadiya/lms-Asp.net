using College_Library_Management_System.Models.Enums;
using College_Library_Management_System.ViewModels.Circulation;
using College_Library_Management_System.ViewModels.Common;
using College_Library_Management_System.ViewModels.Fines;
using Microsoft.AspNetCore.Mvc.Rendering;

namespace College_Library_Management_System.ViewModels.Reports
{
    public class DashboardViewModel
    {
        public int TotalBooks { get; set; }
        public int TotalCopies { get; set; }
        public int AvailableCopies { get; set; }
        public int IssuedCopies { get; set; }
        public int DamagedCopies { get; set; }
        public int TotalStudents { get; set; }
        public int ActiveLoans { get; set; }
        public int OverdueLoans { get; set; }
        public decimal TotalPendingFines { get; set; }
        public decimal TotalFinesCollected { get; set; }

        public List<ActiveLoanListItemViewModel> RecentLoans { get; set; } = new();
        public List<FineListItemViewModel> RecentPayments { get; set; } = new();
        public List<ActiveLoanListItemViewModel> OverdueAlerts { get; set; } = new();
    }

    public class OverdueReportItemViewModel
    {
        public int LoanId { get; set; }
        public string StudentName { get; set; } = string.Empty;
        public string EnrollmentNumber { get; set; } = string.Empty;
        public string DepartmentCode { get; set; } = string.Empty;
        public string? Phone { get; set; }
        public string? Email { get; set; }
        public string BookTitle { get; set; } = string.Empty;
        public string AccessionNumber { get; set; } = string.Empty;
        public string ShelfLocation { get; set; } = string.Empty;
        public DateTime IssuedAt { get; set; }
        public DateTime DueAt { get; set; }
        public int OverdueDays { get; set; }
        public decimal EstimatedFine { get; set; }
    }

    public class OverdueReportFilterViewModel
    {
        public int? DepartmentId { get; set; }
        public int? MinDaysOverdue { get; set; }
        public List<SelectListItem> DepartmentOptions { get; set; } = new();
        public List<OverdueReportItemViewModel> Items { get; set; } = new();
        public int TotalOverdueCount { get; set; }
        public decimal TotalEstimatedFine { get; set; }
    }

    public class InventoryReportViewModel
    {
        public int TotalCatalogTitles { get; set; }
        public int TotalPhysicalCopies { get; set; }
        public int AvailableCount { get; set; }
        public int IssuedCount { get; set; }
        public int DamagedCount { get; set; }
        public int LostCount { get; set; }
        public int WithdrawnCount { get; set; }

        public List<CategoryInventoryBreakdownViewModel> CategoryBreakdown { get; set; } = new();
        public List<DepartmentStudentBreakdownViewModel> DepartmentBreakdown { get; set; } = new();
    }

    public class CategoryInventoryBreakdownViewModel
    {
        public string CategoryName { get; set; } = string.Empty;
        public int BookCount { get; set; }
        public int CopyCount { get; set; }
        public int AvailableCopies { get; set; }
        public int IssuedCopies { get; set; }
    }

    public class DepartmentStudentBreakdownViewModel
    {
        public string DepartmentName { get; set; } = string.Empty;
        public string DepartmentCode { get; set; } = string.Empty;
        public int StudentCount { get; set; }
        public int ActiveLoansCount { get; set; }
    }

    public class CollectionReportFilterViewModel
    {
        public DateTime? StartDate { get; set; }
        public DateTime? EndDate { get; set; }
        public PaymentMethod? PaymentMethod { get; set; }
        public decimal TotalCollected { get; set; }
        public int TransactionsCount { get; set; }
        public List<CollectionTransactionItemViewModel> Transactions { get; set; } = new();
    }

    public class CollectionTransactionItemViewModel
    {
        public string ReceiptNumber { get; set; } = string.Empty;
        public DateTime PaymentDate { get; set; }
        public decimal Amount { get; set; }
        public PaymentMethod PaymentMethod { get; set; }
        public string StudentName { get; set; } = string.Empty;
        public string EnrollmentNumber { get; set; } = string.Empty;
        public string DepartmentCode { get; set; } = string.Empty;
        public string BookTitle { get; set; } = string.Empty;
        public string AccessionNumber { get; set; } = string.Empty;
        public string CashierName { get; set; } = string.Empty;
    }
}
