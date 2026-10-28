using System.ComponentModel.DataAnnotations;
using College_Library_Management_System.Models.Enums;
using Microsoft.AspNetCore.Mvc.Rendering;

namespace College_Library_Management_System.ViewModels.Circulation
{
    public class IssueBookViewModel
    {
        [Required(ErrorMessage = "Student enrollment number is required.")]
        [Display(Name = "Student Enrollment No.")]
        public string StudentEnrollment { get; set; } = string.Empty;

        [Required(ErrorMessage = "Book copy accession number is required.")]
        [Display(Name = "Book Copy Accession No.")]
        public string AccessionNumber { get; set; } = string.Empty;

        [Display(Name = "Loan Period (Days)")]
        [Range(1, 90, ErrorMessage = "Loan period must be between 1 and 90 days.")]
        public int LoanPeriodDays { get; set; } = 14;

        [Display(Name = "Remarks / Notes")]
        [StringLength(250, ErrorMessage = "Remarks cannot exceed 250 characters.")]
        public string? Remarks { get; set; }

        // Preview properties for dynamic UI lookup
        public int? StudentId { get; set; }
        public string? StudentName { get; set; }
        public string? DepartmentName { get; set; }
        public int ActiveLoansCount { get; set; }
        public bool HasOverdueLoans { get; set; }
        public string? OverdueWarning { get; set; }

        public int? BookCopyId { get; set; }
        public string? BookTitle { get; set; }
        public string? ShelfLocation { get; set; }
        public BookCopyStatus? CopyStatus { get; set; }
    }

    public class ReturnBookViewModel
    {
        public int LoanId { get; set; }
        public int BookCopyId { get; set; }
        public string AccessionNumber { get; set; } = string.Empty;
        public string BookTitle { get; set; } = string.Empty;
        public string StudentEnrollment { get; set; } = string.Empty;
        public string StudentName { get; set; } = string.Empty;
        public string DepartmentCode { get; set; } = string.Empty;
        public DateTime IssuedAt { get; set; }
        public DateTime DueAt { get; set; }
        public DateTime ReturnDate { get; set; }

        public bool IsOverdue { get; set; }
        public int OverdueDays { get; set; }
        public decimal DailyFineRate { get; set; }
        public decimal CalculatedFine { get; set; }

        [Required(ErrorMessage = "Please select copy return status.")]
        [Display(Name = "Returned Book Condition")]
        public BookCopyStatus ReturnCondition { get; set; } = BookCopyStatus.Available;

        [StringLength(500, ErrorMessage = "Condition notes cannot exceed 500 characters.")]
        [Display(Name = "Return Condition Notes")]
        public string? ConditionNotes { get; set; }
    }

    public class RenewLoanViewModel
    {
        public int LoanId { get; set; }
        public string AccessionNumber { get; set; } = string.Empty;
        public string BookTitle { get; set; } = string.Empty;
        public string StudentEnrollment { get; set; } = string.Empty;
        public string StudentName { get; set; } = string.Empty;
        public DateTime CurrentDueAt { get; set; }
        public DateTime NewDueAt { get; set; }
        public int RenewalCount { get; set; }
        public int MaxRenewals { get; set; } = 1;
        public bool IsEligible { get; set; }
        public string? IneligibilityReason { get; set; }
    }

    public class ActiveLoanListFilterViewModel
    {
        public string? SearchTerm { get; set; }
        public int? DepartmentId { get; set; }
        public bool OverdueOnly { get; set; }
        public int PageNumber { get; set; } = 1;
        public int PageSize { get; set; } = 15;
        public List<SelectListItem> DepartmentOptions { get; set; } = new();
    }

    public class ActiveLoanListItemViewModel
    {
        public int LoanId { get; set; }
        public int BookCopyId { get; set; }
        public string AccessionNumber { get; set; } = string.Empty;
        public string BookTitle { get; set; } = string.Empty;
        public int StudentId { get; set; }
        public string StudentEnrollment { get; set; } = string.Empty;
        public string StudentName { get; set; } = string.Empty;
        public string DepartmentCode { get; set; } = string.Empty;
        public DateTime IssuedAt { get; set; }
        public DateTime DueAt { get; set; }
        public int RenewalCount { get; set; }
        public bool IsOverdue { get; set; }
        public int OverdueDays { get; set; }
        public decimal EstimatedFine { get; set; }
        public string IssuedBy { get; set; } = string.Empty;
    }

    public class CirculationEligibilityResult
    {
        public bool IsEligible { get; set; }
        public string Message { get; set; } = string.Empty;

        // Student details
        public int? StudentId { get; set; }
        public string? StudentName { get; set; }
        public string? EnrollmentNumber { get; set; }
        public string? DepartmentName { get; set; }
        public bool IsStudentActive { get; set; }
        public int CurrentActiveLoans { get; set; }
        public int MaxAllowedLoans { get; set; } = 3;
        public bool HasOverdueLoans { get; set; }
        public List<string> OverdueBookTitles { get; set; } = new();

        // Copy details
        public int? BookCopyId { get; set; }
        public string? AccessionNumber { get; set; }
        public string? BookTitle { get; set; }
        public string? ShelfLocation { get; set; }
        public BookCopyStatus? CopyStatus { get; set; }
    }

    public class LoanHistoryFilterViewModel
    {
        public string? SearchTerm { get; set; }
        public int? DepartmentId { get; set; }
        public LoanStatus? Status { get; set; }
        public string ActiveTab { get; set; } = "all"; // "all", "issued", "returned"

        [DataType(DataType.Date)]
        public DateTime? FromDate { get; set; }

        [DataType(DataType.Date)]
        public DateTime? ToDate { get; set; }

        public string DateFilterType { get; set; } = "Issued"; // "Issued", "Returned"
        public int PageNumber { get; set; } = 1;
        public int PageSize { get; set; } = 15;
        public List<SelectListItem> DepartmentOptions { get; set; } = new();
        public LoanHistorySummaryViewModel Summary { get; set; } = new();
    }

    public class LoanHistoryItemViewModel
    {
        public int LoanId { get; set; }
        public int BookCopyId { get; set; }
        public string AccessionNumber { get; set; } = string.Empty;
        public string BookTitle { get; set; } = string.Empty;
        public int BookId { get; set; }
        public string? ShelfLocation { get; set; }

        public int StudentId { get; set; }
        public string StudentEnrollment { get; set; } = string.Empty;
        public string StudentName { get; set; } = string.Empty;
        public string DepartmentCode { get; set; } = string.Empty;
        public string DepartmentName { get; set; } = string.Empty;

        public DateTime IssuedAt { get; set; }
        public DateTime DueAt { get; set; }
        public DateTime? ReturnedAt { get; set; }
        public LoanStatus Status { get; set; }
        public int RenewalCount { get; set; }

        public string IssuedBy { get; set; } = string.Empty;
        public string? ReceivedBy { get; set; }

        public bool IsOverdue { get; set; }
        public int OverdueDays { get; set; }
        public decimal EstimatedOrActualFine { get; set; }
        public FineStatus? FineStatus { get; set; }
        public decimal? AmountPaid { get; set; }
        public decimal? WaivedAmount { get; set; }

        public BookCopyStatus? ReturnCondition { get; set; }
        public string? ConditionNotes { get; set; }
    }

    public class LoanHistorySummaryViewModel
    {
        public int TotalLoans { get; set; }
        public int ActiveLoansCount { get; set; }
        public int ReturnedLoansCount { get; set; }
        public int OverdueLoansCount { get; set; }
        public int LostLoansCount { get; set; }
        public decimal TotalFinesAssessed { get; set; }
    }
}
