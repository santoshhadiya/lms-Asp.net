using System.ComponentModel.DataAnnotations;

namespace College_Library_Management_System.ViewModels.MasterData
{
    public class CategoryViewModel
    {
        public int CategoryId { get; set; }

        [Required(ErrorMessage = "Category Name is required.")]
        [StringLength(100, ErrorMessage = "Category Name cannot exceed 100 characters.")]
        [Display(Name = "Category Name")]
        public string Name { get; set; } = string.Empty;

        [StringLength(500, ErrorMessage = "Description cannot exceed 500 characters.")]
        [Display(Name = "Description")]
        public string? Description { get; set; }

        public int BookCount { get; set; }
    }

    public class PublisherViewModel
    {
        public int PublisherId { get; set; }

        [Required(ErrorMessage = "Publisher Name is required.")]
        [StringLength(150, ErrorMessage = "Publisher Name cannot exceed 150 characters.")]
        [Display(Name = "Publisher Name")]
        public string Name { get; set; } = string.Empty;

        [StringLength(250, ErrorMessage = "Address cannot exceed 250 characters.")]
        [Display(Name = "Address / Location")]
        public string? Address { get; set; }

        [EmailAddress(ErrorMessage = "Please enter a valid email address.")]
        [StringLength(150, ErrorMessage = "Email cannot exceed 150 characters.")]
        [Display(Name = "Contact Email")]
        public string? ContactEmail { get; set; }

        public int BookCount { get; set; }
    }

    public class AuthorViewModel
    {
        public int AuthorId { get; set; }

        [Required(ErrorMessage = "Author Name is required.")]
        [StringLength(150, ErrorMessage = "Author Name cannot exceed 150 characters.")]
        [Display(Name = "Author Name")]
        public string Name { get; set; } = string.Empty;

        [StringLength(1000, ErrorMessage = "Biography cannot exceed 1000 characters.")]
        [Display(Name = "Biography / Notes")]
        public string? Biography { get; set; }

        public int BookCount { get; set; }
    }
}
