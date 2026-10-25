using Microsoft.AspNetCore.Identity;

namespace College_Library_Management_System.Models
{
    public class ApplicationUser : IdentityUser
    {
        public string FullName { get; set; } = string.Empty;
        public bool IsActive { get; set; } = true;
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        // Navigation properties
        public virtual ICollection<Loan> IssuedLoans { get; set; } = new List<Loan>();
        public virtual ICollection<Loan> ReceivedLoans { get; set; } = new List<Loan>();
        public virtual ICollection<FinePayment> ReceivedPayments { get; set; } = new List<FinePayment>();
        public virtual ICollection<Fine> WaivedFines { get; set; } = new List<Fine>();
    }
}
