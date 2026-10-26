using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Mvc.Rendering;

namespace College_Library_Management_System.ViewModels.Students
{
    public class StudentListFilterViewModel
    {
        public string? SearchTerm { get; set; }
        public int? DepartmentId { get; set; }
        public bool? IsActive { get; set; }
        public int PageNumber { get; set; } = 1;
        public int PageSize { get; set; } = 10;
        public List<SelectListItem> DepartmentOptions { get; set; } = new();
    }

    public class StudentListItemViewModel
    {
        public int StudentId { get; set; }
        public string EnrollmentNumber { get; set; } = string.Empty;
        public string FullName { get; set; } = string.Empty;
        public string? Email { get; set; }
        public string? Phone { get; set; }
        public string DepartmentName { get; set; } = string.Empty;
        public string DepartmentCode { get; set; } = string.Empty;
        public int Semester { get; set; }
        public bool IsActive { get; set; }
        public int ActiveLoansCount { get; set; }
        public decimal OutstandingFinesTotal { get; set; }
    }

    public class StudentFormViewModel
    {
        public int StudentId { get; set; }

        [Required(ErrorMessage = "Enrollment Number is required.")]
        [StringLength(50, ErrorMessage = "Enrollment Number cannot exceed 50 characters.")]
        [Display(Name = "Enrollment Number")]
        public string EnrollmentNumber { get; set; } = string.Empty;

        [Required(ErrorMessage = "Student Full Name is required.")]
        [StringLength(150, ErrorMessage = "Full Name cannot exceed 150 characters.")]
        [Display(Name = "Full Name")]
        public string FullName { get; set; } = string.Empty;

        [EmailAddress(ErrorMessage = "Please enter a valid email address.")]
        [StringLength(150, ErrorMessage = "Email cannot exceed 150 characters.")]
        [Display(Name = "Email Address")]
        public string? Email { get; set; }

        [Phone(ErrorMessage = "Please enter a valid phone number.")]
        [StringLength(20, ErrorMessage = "Phone cannot exceed 20 characters.")]
        [Display(Name = "Phone Number")]
        public string? Phone { get; set; }

        [Required(ErrorMessage = "Department is required.")]
        [Display(Name = "Department")]
        public int DepartmentId { get; set; }

        [Required(ErrorMessage = "Semester is required.")]
        [Range(1, 8, ErrorMessage = "Semester must be between 1 and 8.")]
        [Display(Name = "Semester")]
        public int Semester { get; set; } = 1;

        [Display(Name = "Active Status")]
        public bool IsActive { get; set; } = true;

        [StringLength(100, MinimumLength = 6, ErrorMessage = "Temporary password must be at least 6 characters.")]
        [DataType(DataType.Password)]
        [Display(Name = "Temporary Password")]
        public string? TemporaryPassword { get; set; }

        public List<SelectListItem> DepartmentOptions { get; set; } = new();
    }

    public class StudentDetailsViewModel
    {
        public int StudentId { get; set; }
        public string EnrollmentNumber { get; set; } = string.Empty;
        public string FullName { get; set; } = string.Empty;
        public string? Email { get; set; }
        public string? Phone { get; set; }
        public string DepartmentName { get; set; } = string.Empty;
        public int Semester { get; set; }
        public bool IsActive { get; set; }
        public DateTime CreatedAt { get; set; }

        public int ActiveLoansCount { get; set; }
        public decimal OutstandingFinesTotal { get; set; }
        public bool HasOverdueLoans { get; set; }

        public List<StudentLoanHistoryItemViewModel> LoanHistory { get; set; } = new();
    }

    public class StudentLoanHistoryItemViewModel
    {
        public int LoanId { get; set; }
        public string BookTitle { get; set; } = string.Empty;
        public string AccessionNumber { get; set; } = string.Empty;
        public DateTime IssuedAt { get; set; }
        public DateTime DueAt { get; set; }
        public DateTime? ReturnedAt { get; set; }
        public string Status { get; set; } = string.Empty;
        public int RenewalCount { get; set; }
        public decimal? FineAmount { get; set; }
        public string? FineStatus { get; set; }
    }
}
