using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Mvc.Rendering;

namespace College_Library_Management_System.ViewModels.Books
{
    public class BookListFilterViewModel
    {
        public string? SearchTerm { get; set; }
        public int? CategoryId { get; set; }
        public int? PublisherId { get; set; }
        public int PageNumber { get; set; } = 1;
        public int PageSize { get; set; } = 10;
        public List<SelectListItem> CategoryOptions { get; set; } = new();
        public List<SelectListItem> PublisherOptions { get; set; } = new();
    }

    public class BookListItemViewModel
    {
        public int BookId { get; set; }
        public string? ISBN { get; set; }
        public string Title { get; set; } = string.Empty;
        public string CategoryName { get; set; } = string.Empty;
        public string PublisherName { get; set; } = string.Empty;
        public string AuthorsString { get; set; } = string.Empty;
        public int? PublicationYear { get; set; }
        public int TotalCopiesCount { get; set; }
        public int AvailableCopiesCount { get; set; }
        public int IssuedCopiesCount { get; set; }
    }

    public class BookFormViewModel
    {
        public int BookId { get; set; }

        [Required(ErrorMessage = "Book Title is required.")]
        [StringLength(250, ErrorMessage = "Title cannot exceed 250 characters.")]
        [Display(Name = "Book Title")]
        public string Title { get; set; } = string.Empty;

        [StringLength(20, ErrorMessage = "ISBN cannot exceed 20 characters.")]
        [Display(Name = "ISBN (Optional)")]
        public string? ISBN { get; set; }

        [Range(1000, 2100, ErrorMessage = "Please enter a valid publication year between 1000 and 2100.")]
        [Display(Name = "Publication Year")]
        public int? PublicationYear { get; set; }

        [StringLength(1000, ErrorMessage = "Description cannot exceed 1000 characters.")]
        [Display(Name = "Description / Summary")]
        public string? Description { get; set; }

        [Required(ErrorMessage = "Category is required.")]
        [Display(Name = "Category")]
        public int CategoryId { get; set; }

        [Required(ErrorMessage = "Publisher is required.")]
        [Display(Name = "Publisher")]
        public int PublisherId { get; set; }

        [Required(ErrorMessage = "At least one author must be selected.")]
        [MinLength(1, ErrorMessage = "At least one author must be selected.")]
        [Display(Name = "Authors")]
        public List<int> SelectedAuthorIds { get; set; } = new();

        public List<SelectListItem> CategoryOptions { get; set; } = new();
        public List<SelectListItem> PublisherOptions { get; set; } = new();
        public List<SelectListItem> AuthorOptions { get; set; } = new();
    }

    public class BookDetailsViewModel
    {
        public int BookId { get; set; }
        public string? ISBN { get; set; }
        public string Title { get; set; } = string.Empty;
        public int? PublicationYear { get; set; }
        public string? Description { get; set; }
        public string CategoryName { get; set; } = string.Empty;
        public string PublisherName { get; set; } = string.Empty;
        public DateTime CreatedAt { get; set; }

        public List<string> Authors { get; set; } = new();
        public List<BookCopyItemViewModel> Copies { get; set; } = new();

        public int TotalCopies => Copies.Count;
        public int AvailableCopies => Copies.Count(c => c.Status == Models.Enums.BookCopyStatus.Available);
        public int IssuedCopies => Copies.Count(c => c.Status == Models.Enums.BookCopyStatus.Issued);
    }

    public class BookCopyItemViewModel
    {
        public int BookCopyId { get; set; }
        public string AccessionNumber { get; set; } = string.Empty;
        public string ShelfLocation { get; set; } = string.Empty;
        public Models.Enums.BookCopyStatus Status { get; set; }
        public string? ConditionNotes { get; set; }
        public string? CurrentBorrowerName { get; set; }
        public DateTime? DueDate { get; set; }
    }
}
