namespace College_Library_Management_System.Models
{
    public class Department
    {
        public int DepartmentId { get; set; }
        public string DepartmentName { get; set; } = string.Empty;
        public string DepartmentCode { get; set; } = string.Empty;

        public virtual ICollection<Student> Students { get; set; } = new List<Student>();
    }
}
