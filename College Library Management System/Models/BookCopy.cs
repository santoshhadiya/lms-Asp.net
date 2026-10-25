using College_Library_Management_System.Models.Enums;

namespace College_Library_Management_System.Models
{
    public class BookCopy
    {
        public int BookCopyId { get; set; }
        public int BookId { get; set; }
        public string AccessionNumber { get; set; } = string.Empty;
        public string ShelfLocation { get; set; } = string.Empty;
        public BookCopyStatus Status { get; set; } = BookCopyStatus.Available;
        public string? ConditionNotes { get; set; }
        public byte[]? RowVersion { get; set; }

        public virtual Book Book { get; set; } = null!;
        public virtual ICollection<Loan> Loans { get; set; } = new List<Loan>();
    }
}
