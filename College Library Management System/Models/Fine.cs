using College_Library_Management_System.Models.Enums;

namespace College_Library_Management_System.Models
{
    public class Fine
    {
        public int FineId { get; set; }
        public int LoanId { get; set; }
        public decimal FineAmount { get; set; }
        public decimal DailyFineRateApplied { get; set; }
        public decimal AmountPaid { get; set; } = 0m;
        public decimal WaivedAmount { get; set; } = 0m;
        public FineStatus Status { get; set; } = FineStatus.Pending;
        public DateTime CalculatedAt { get; set; }
        public string? WaivedByUserId { get; set; }
        public string? WaiverReason { get; set; }

        public virtual Loan Loan { get; set; } = null!;
        public virtual ApplicationUser? WaivedByUser { get; set; }
        public virtual FinePayment? FinePayment { get; set; }

        public decimal RemainingBalance => Math.Max(0m, FineAmount - AmountPaid - WaivedAmount);
    }
}
