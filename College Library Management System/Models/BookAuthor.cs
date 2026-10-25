namespace College_Library_Management_System.Models
{
    public class BookAuthor
    {
        public int BookId { get; set; }
        public int AuthorId { get; set; }

        public virtual Book Book { get; set; } = null!;
        public virtual Author Author { get; set; } = null!;
    }
}
