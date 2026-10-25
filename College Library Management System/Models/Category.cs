namespace College_Library_Management_System.Models
{
    public class Category
    {
        public int CategoryId { get; set; }
        public string Name { get; set; } = string.Empty;
        public string? Description { get; set; }

        public virtual ICollection<Book> Books { get; set; } = new List<Book>();
    }
}
