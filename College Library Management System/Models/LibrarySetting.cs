namespace College_Library_Management_System.Models
{
    public class LibrarySetting
    {
        public int LibrarySettingId { get; set; }
        public int LoanPeriodDays { get; set; } = 14;
        public decimal DailyFineRate { get; set; } = 5.00m;
        public int MaxBooksPerStudent { get; set; } = 3;
        public bool BlockIssueWhenOverdue { get; set; } = true;
        public bool AllowRenewal { get; set; } = true;
        public int MaxRenewalsPerLoan { get; set; } = 1;
        public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
        public string? UpdatedByUserId { get; set; }
    }
}
