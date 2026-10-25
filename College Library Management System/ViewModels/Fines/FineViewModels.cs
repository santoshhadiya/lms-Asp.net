using System.ComponentModel.DataAnnotations;
using College_Library_Management_System.Models.Enums;
using Microsoft.AspNetCore.Mvc.Rendering;

namespace College_Library_Management_System.ViewModels.Fines
{
    public class FineListFilterViewModel
    {
        public string? SearchTerm { get; set; }
        public FineStatus? Status { get; set; }
        public int? DepartmentId { get; set; }
        public int PageNumber { get; set; } = 1;
        public int PageSize { get; set; } = 15;
        public List<SelectListItem> DepartmentOptions { get; set; } = new();
    }

    public class FineListItemViewModel
    {
        public int FineId { get; set; }
        public int LoanId { get; set; }
        public int StudentId { get; set; }
        public string StudentEnrollment { get; set; } = string.Empty;
        public string StudentName { get; set; } = string.Empty;
        public string DepartmentCode { get; set; } = string.Empty;
        public string BookTitle { get; set; } = string.Empty;
        public string AccessionNumber { get; set; } = string.Empty;
        public decimal FineAmount { get; set; }
        public decimal DailyFineRate { get; set; }
        public decimal AmountPaid { get; set; }
        public decimal WaivedAmount { get; set; }
        public decimal RemainingBalance { get; set; }
        public FineStatus Status { get; set; }
        public DateTime CalculatedAt { get; set; }
        public string? ReceiptNumber { get; set; }
        public DateTime? PaymentDate { get; set; }
    }

    public class CollectFinePaymentViewModel
    {
        public int FineId { get; set; }
        public string StudentEnrollment { get; set; } = string.Empty;
        public string StudentName { get; set; } = string.Empty;
        public string DepartmentName { get; set; } = string.Empty;
        public string BookTitle { get; set; } = string.Empty;
        public string AccessionNumber { get; set; } = string.Empty;
        public DateTime LoanIssuedAt { get; set; }
        public DateTime LoanDueAt { get; set; }
        public DateTime? LoanReturnedAt { get; set; }
        public decimal FineAmount { get; set; }
        public decimal AmountPaid { get; set; }
        public decimal WaivedAmount { get; set; }
        public decimal RemainingBalance { get; set; }

        [Required(ErrorMessage = "Payment amount is required.")]
        [Display(Name = "Payment Amount (₹)")]
        public decimal PaymentAmount { get; set; }

        [Required(ErrorMessage = "Payment method is required.")]
        [Display(Name = "Payment Method")]
        public PaymentMethod PaymentMethod { get; set; } = PaymentMethod.Cash;

        [StringLength(250, ErrorMessage = "Remarks cannot exceed 250 characters.")]
        [Display(Name = "Cashier Remarks / Transaction Ref")]
        public string? Remarks { get; set; }
    }

    public class WaiveFineViewModel
    {
        public int FineId { get; set; }
        public string StudentEnrollment { get; set; } = string.Empty;
        public string StudentName { get; set; } = string.Empty;
        public string BookTitle { get; set; } = string.Empty;
        public decimal FineAmount { get; set; }
        public decimal RemainingBalance { get; set; }

        [Required(ErrorMessage = "Waiver justification reason is mandatory.")]
        [StringLength(500, MinimumLength = 10, ErrorMessage = "Waiver reason must be between 10 and 500 characters.")]
        [Display(Name = "Mandatory Justification / Administrative Reason")]
        public string WaiverReason { get; set; } = string.Empty;
    }

    public class FineReceiptViewModel
    {
        public string ReceiptNumber { get; set; } = string.Empty;
        public int FineId { get; set; }
        public int PaymentId { get; set; }
        public DateTime PaymentDate { get; set; }
        public PaymentMethod PaymentMethod { get; set; }
        public decimal AmountPaid { get; set; }
        public string StudentEnrollment { get; set; } = string.Empty;
        public string StudentName { get; set; } = string.Empty;
        public string DepartmentName { get; set; } = string.Empty;
        public string BookTitle { get; set; } = string.Empty;
        public string AccessionNumber { get; set; } = string.Empty;
        public int OverdueDays { get; set; }
        public decimal DailyFineRate { get; set; }
        public string ReceivedByStaffName { get; set; } = string.Empty;
        public string? Remarks { get; set; }
    }
}
