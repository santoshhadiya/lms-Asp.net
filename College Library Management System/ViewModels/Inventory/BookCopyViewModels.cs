using System.ComponentModel.DataAnnotations;
using College_Library_Management_System.Models.Enums;
using Microsoft.AspNetCore.Mvc.Rendering;

namespace College_Library_Management_System.ViewModels.Inventory
{
    public class BookCopyListFilterViewModel
    {
        public string? SearchTerm { get; set; }
        public int? BookId { get; set; }
        public BookCopyStatus? Status { get; set; }
        public string? ShelfLocation { get; set; }
        public int PageNumber { get; set; } = 1;
        public int PageSize { get; set; } = 15;
        public List<SelectListItem> BookOptions { get; set; } = new();
    }

    public class BookCopyListItemViewModel
    {
        public int BookCopyId { get; set; }
        public int BookId { get; set; }
        public string BookTitle { get; set; } = string.Empty;
        public string? ISBN { get; set; }
        public string AccessionNumber { get; set; } = string.Empty;
        public string ShelfLocation { get; set; } = string.Empty;
        public BookCopyStatus Status { get; set; }
        public string? ConditionNotes { get; set; }
        public bool HasActiveLoan { get; set; }
        public string? CurrentBorrowerName { get; set; }
        public DateTime? DueDate { get; set; }
    }

    public class BookCopyFormViewModel
    {
        public int BookCopyId { get; set; }

        [Required(ErrorMessage = "Book is required.")]
        [Display(Name = "Book Title")]
        public int BookId { get; set; }

        public string? BookTitle { get; set; }

        [Required(ErrorMessage = "Accession Number is required.")]
        [StringLength(50, ErrorMessage = "Accession Number cannot exceed 50 characters.")]
        [Display(Name = "Accession Number")]
        public string AccessionNumber { get; set; } = string.Empty;

        [Required(ErrorMessage = "Shelf Location is required.")]
        [StringLength(50, ErrorMessage = "Shelf Location cannot exceed 50 characters.")]
        [Display(Name = "Shelf Location / Rack")]
        public string ShelfLocation { get; set; } = string.Empty;

        [Required(ErrorMessage = "Copy Status is required.")]
        [Display(Name = "Current Status")]
        public BookCopyStatus Status { get; set; } = BookCopyStatus.Available;

        [StringLength(500, ErrorMessage = "Condition notes cannot exceed 500 characters.")]
        [Display(Name = "Physical Condition Notes")]
        public string? ConditionNotes { get; set; }

        public List<SelectListItem> BookOptions { get; set; } = new();
    }

    public class BatchCopyFormViewModel
    {
        [Required(ErrorMessage = "Book is required.")]
        public int BookId { get; set; }

        public string BookTitle { get; set; } = string.Empty;

        [Required(ErrorMessage = "Accession Prefix is required.")]
        [StringLength(20, ErrorMessage = "Prefix cannot exceed 20 characters.")]
        [Display(Name = "Accession Prefix (e.g. ACC, CS, ENG)")]
        public string Prefix { get; set; } = "ACC-";

        [Required(ErrorMessage = "Starting number is required.")]
        [Range(1, 999999, ErrorMessage = "Starting number must be at least 1.")]
        [Display(Name = "Starting Number")]
        public int StartingNumber { get; set; } = 1;

        [Required(ErrorMessage = "Number of copies is required.")]
        [Range(1, 100, ErrorMessage = "You can add between 1 and 100 copies at once.")]
        [Display(Name = "Number of Copies to Add")]
        public int NumberOfCopies { get; set; } = 1;

        [Required(ErrorMessage = "Shelf Location is required.")]
        [StringLength(50, ErrorMessage = "Shelf location cannot exceed 50 characters.")]
        [Display(Name = "Shelf Location / Rack")]
        public string ShelfLocation { get; set; } = "Main Stack A-1";

        [StringLength(500, ErrorMessage = "Condition notes cannot exceed 500 characters.")]
        [Display(Name = "Initial Condition Notes")]
        public string? ConditionNotes { get; set; } = "New Copy - In Good Condition";
    }

    public class BookCopyHistoryViewModel
    {
        public int BookCopyId { get; set; }
        public int BookId { get; set; }
        public string BookTitle { get; set; } = string.Empty;
        public string? ISBN { get; set; }
        public string AccessionNumber { get; set; } = string.Empty;
        public string ShelfLocation { get; set; } = string.Empty;
        public BookCopyStatus Status { get; set; }
        public string? ConditionNotes { get; set; }

        public List<CopyLoanHistoryItemViewModel> LoanHistory { get; set; } = new();
    }

    public class CopyLoanHistoryItemViewModel
    {
        public int LoanId { get; set; }
        public string StudentEnrollment { get; set; } = string.Empty;
        public string StudentName { get; set; } = string.Empty;
        public string DepartmentCode { get; set; } = string.Empty;
        public DateTime IssuedAt { get; set; }
        public DateTime DueAt { get; set; }
        public DateTime? ReturnedAt { get; set; }
        public LoanStatus LoanStatus { get; set; }
        public int RenewalCount { get; set; }
        public string IssuedBy { get; set; } = string.Empty;
        public string? ReceivedBy { get; set; }
        public decimal? FineAmount { get; set; }
        public FineStatus? FineStatus { get; set; }
    }

    public class ChangeCopyStatusViewModel
    {
        public int BookCopyId { get; set; }
        public string AccessionNumber { get; set; } = string.Empty;
        public string BookTitle { get; set; } = string.Empty;
        public BookCopyStatus CurrentStatus { get; set; }

        [Required(ErrorMessage = "New status is required.")]
        [Display(Name = "New Status")]
        public BookCopyStatus NewStatus { get; set; }

        [StringLength(500, ErrorMessage = "Condition notes cannot exceed 500 characters.")]
        [Display(Name = "Reason / Condition Update")]
        public string? ConditionNotes { get; set; }
    }
}
