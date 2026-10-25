namespace College_Library_Management_System.Models
{
    public class Publisher
    {
        public int PublisherId { get; set; }
        public string Name { get; set; } = string.Empty;
        public string? Address { get; set; }
        public string? ContactEmail { get; set; }

        public virtual ICollection<Book> Books { get; set; } = new List<Book>();
    }
}
