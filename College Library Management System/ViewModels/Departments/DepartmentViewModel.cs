using System.ComponentModel.DataAnnotations;

namespace College_Library_Management_System.ViewModels.Departments
{
    public class DepartmentFormViewModel
    {
        public int DepartmentId { get; set; }

        [Required(ErrorMessage = "Department Name is required.")]
        [StringLength(100, ErrorMessage = "Department Name cannot exceed 100 characters.")]
        [Display(Name = "Department Name")]
        public string DepartmentName { get; set; } = string.Empty;

        [Required(ErrorMessage = "Department Code is required.")]
        [StringLength(20, ErrorMessage = "Department Code cannot exceed 20 characters.")]
        [RegularExpression(@"^[A-Za-z0-9\-_]+$", ErrorMessage = "Department Code can only contain alphanumeric characters, hyphens, and underscores.")]
        [Display(Name = "Department Code")]
        public string DepartmentCode { get; set; } = string.Empty;
    }

    public class DepartmentItemViewModel
    {
        public int DepartmentId { get; set; }
        public string DepartmentName { get; set; } = string.Empty;
        public string DepartmentCode { get; set; } = string.Empty;
        public int StudentCount { get; set; }
    }
}
