using College_Library_Management_System.Models.Enums;

namespace College_Library_Management_System.Models
{
    public class Loan
    {
        public int LoanId { get; set; }
        public int StudentId { get; set; }
        public int BookCopyId { get; set; }
        public DateTime IssuedAt { get; set; }
        public DateTime DueAt { get; set; }
        public DateTime? ReturnedAt { get; set; }
        public LoanStatus Status { get; set; } = LoanStatus.Active;
        public int RenewalCount { get; set; } = 0;
        public string IssuedByUserId { get; set; } = string.Empty;
        public string? ReceivedByUserId { get; set; }
        public byte[]? RowVersion { get; set; }

        public virtual Student Student { get; set; } = null!;
        public virtual BookCopy BookCopy { get; set; } = null!;
        public virtual ApplicationUser IssuedByUser { get; set; } = null!;
        public virtual ApplicationUser? ReceivedByUser { get; set; }
        public virtual Fine? Fine { get; set; }
    }
}
