namespace College_Library_Management_System.ViewModels.StudentDashboard
{
    public class StudentProfileViewModel
    {
        public int StudentId { get; set; }
        public string FullName { get; set; } = string.Empty;
        public string EnrollmentNumber { get; set; } = string.Empty;
        public string DepartmentName { get; set; } = string.Empty;
        public string DepartmentCode { get; set; } = string.Empty;
        public int Semester { get; set; }
        public string? Email { get; set; }
        public string? Phone { get; set; }
        public bool IsActive { get; set; }
        public DateTime MemberSince { get; set; }
    }

    public class StudentLoanItemViewModel
    {
        public int LoanId { get; set; }
        public string BookTitle { get; set; } = string.Empty;
        public string AccessionNumber { get; set; } = string.Empty;
        public DateTime IssuedAt { get; set; }
        public DateTime DueAt { get; set; }
        public DateTime? ReturnedAt { get; set; }
        public string Status { get; set; } = string.Empty;
        public int RenewalCount { get; set; }
        public bool IsOverdue { get; set; }
        public int DaysOverdue { get; set; }
        public decimal EstimatedFine { get; set; }
    }

    public class StudentFineItemViewModel
    {
        public int FineId { get; set; }
        public string BookTitle { get; set; } = string.Empty;
        public string AccessionNumber { get; set; } = string.Empty;
        public decimal FineAmount { get; set; }
        public decimal AmountPaid { get; set; }
        public decimal WaivedAmount { get; set; }
        public decimal RemainingBalance => Math.Max(0m, FineAmount - AmountPaid - WaivedAmount);
        public string Status { get; set; } = string.Empty;
        public DateTime CalculatedAt { get; set; }
        public string? ReceiptNumber { get; set; }
        public DateTime? PaidAt { get; set; }
        public string? PaymentMethod { get; set; }
    }

    public class StudentDashboardViewModel
    {
        public StudentProfileViewModel Profile { get; set; } = new();
        public int ActiveLoansCount { get; set; }
        public int MaxBorrowingLimit { get; set; } = 3;
        public int OverdueLoansCount { get; set; }
        public decimal OutstandingFinesTotal { get; set; }
        public int TotalCompletedLoans { get; set; }
        public List<StudentLoanItemViewModel> ActiveLoans { get; set; } = new();
        public List<StudentLoanItemViewModel> BorrowingHistory { get; set; } = new();
        public List<StudentFineItemViewModel> FinesAndPayments { get; set; } = new();
    }
}
