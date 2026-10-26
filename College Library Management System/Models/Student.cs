namespace College_Library_Management_System.Models
{
    public class Student
    {
        public int StudentId { get; set; }
        public string EnrollmentNumber { get; set; } = string.Empty;
        public string FullName { get; set; } = string.Empty;
        public string? Email { get; set; }
        public string? Phone { get; set; }
        public int DepartmentId { get; set; }
        public int Semester { get; set; }
        public bool IsActive { get; set; } = true;
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        public string? UserId { get; set; }
        public virtual ApplicationUser? User { get; set; }

        public virtual Department Department { get; set; } = null!;
        public virtual ICollection<Loan> Loans { get; set; } = new List<Loan>();
    }
}
