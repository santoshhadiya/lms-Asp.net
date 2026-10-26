using System.ComponentModel.DataAnnotations;

namespace College_Library_Management_System.ViewModels.Librarians
{
    public class LibrarianListItemViewModel
    {
        public string Id { get; set; } = string.Empty;
        public string FullName { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public string? PhoneNumber { get; set; }
        public bool IsActive { get; set; }
        public DateTime CreatedAt { get; set; }
        public int IssuedLoansCount { get; set; }
        public int ReceivedLoansCount { get; set; }
    }

    public class LibrarianFormViewModel
    {
        public string? Id { get; set; }

        [Required(ErrorMessage = "Full Name is required.")]
        [StringLength(150, ErrorMessage = "Full Name cannot exceed 150 characters.")]
        [Display(Name = "Full Name")]
        public string FullName { get; set; } = string.Empty;

        [Required(ErrorMessage = "Email address is required.")]
        [EmailAddress(ErrorMessage = "Please enter a valid email address.")]
        [StringLength(150, ErrorMessage = "Email cannot exceed 150 characters.")]
        [Display(Name = "Email Address (Login Username)")]
        public string Email { get; set; } = string.Empty;

        [Phone(ErrorMessage = "Please enter a valid phone number.")]
        [StringLength(20, ErrorMessage = "Phone cannot exceed 20 characters.")]
        [Display(Name = "Phone Number")]
        public string? PhoneNumber { get; set; }

        [Display(Name = "Account Status")]
        public bool IsActive { get; set; } = true;

        [StringLength(100, MinimumLength = 6, ErrorMessage = "Temporary password must be at least 6 characters.")]
        [DataType(DataType.Password)]
        [Display(Name = "Temporary Password")]
        public string? TemporaryPassword { get; set; }
    }
}
