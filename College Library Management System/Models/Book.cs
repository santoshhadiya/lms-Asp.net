namespace College_Library_Management_System.Models
{
    public class Book
    {
        public int BookId { get; set; }
        public string? ISBN { get; set; }
        public string Title { get; set; } = string.Empty;
        public int? PublicationYear { get; set; }
        public string? Description { get; set; }
        public int CategoryId { get; set; }
        public int PublisherId { get; set; }
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        public virtual Category Category { get; set; } = null!;
        public virtual Publisher Publisher { get; set; } = null!;
        public virtual ICollection<BookAuthor> BookAuthors { get; set; } = new List<BookAuthor>();
        public virtual ICollection<BookCopy> Copies { get; set; } = new List<BookCopy>();
    }
}
