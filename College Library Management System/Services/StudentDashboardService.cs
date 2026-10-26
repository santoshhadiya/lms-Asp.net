using College_Library_Management_System.Data;
using College_Library_Management_System.Models.Enums;
using College_Library_Management_System.Services.Common;
using College_Library_Management_System.Services.Interfaces;
using College_Library_Management_System.ViewModels.StudentDashboard;
using Microsoft.EntityFrameworkCore;

namespace College_Library_Management_System.Services
{
    public class StudentDashboardService : IStudentDashboardService
    {
        private readonly ApplicationDbContext _context;
        private readonly IDateTimeProvider _dateTimeProvider;

        public StudentDashboardService(ApplicationDbContext context, IDateTimeProvider dateTimeProvider)
        {
            _context = context;
            _dateTimeProvider = dateTimeProvider;
        }

        public async Task<StudentDashboardViewModel?> GetStudentDashboardDataAsync(string userId, string? userEmail)
        {
            // Security guarantee: Strictly match either the student's UserId or verified Email
            var student = await _context.Students
                .Include(s => s.Department)
                .Include(s => s.Loans)
                    .ThenInclude(l => l.BookCopy)
                        .ThenInclude(bc => bc.Book)
                .Include(s => s.Loans)
                    .ThenInclude(l => l.Fine)
                        .ThenInclude(f => f!.FinePayment)
                .FirstOrDefaultAsync(s => s.UserId == userId || (s.Email != null && userEmail != null && s.Email.ToLower() == userEmail.ToLower()));

            if (student == null)
            {
                return null;
            }

            var localNow = _dateTimeProvider.Now;
            var setting = await _context.LibrarySettings.AsNoTracking().FirstOrDefaultAsync() 
                          ?? new Models.LibrarySetting { DailyFineRate = 5.00m, MaxBooksPerStudent = 3 };

            var allLoans = student.Loans.OrderByDescending(l => l.IssuedAt).ToList();
            var activeLoansEntities = allLoans.Where(l => l.Status == LoanStatus.Active).ToList();
            var historyLoansEntities = allLoans.Where(l => l.Status == LoanStatus.Returned).ToList();

            var activeLoans = activeLoansEntities.Select(l =>
            {
                var dueLocal = _dateTimeProvider.ToLocalDateTime(l.DueAt);
                var isOverdue = dueLocal.Date < localNow.Date;
                var overdueDays = isOverdue ? (localNow.Date - dueLocal.Date).Days : 0;
                var estimatedFine = overdueDays * setting.DailyFineRate;

                return new StudentLoanItemViewModel
                {
                    LoanId = l.LoanId,
                    BookTitle = l.BookCopy.Book.Title,
                    AccessionNumber = l.BookCopy.AccessionNumber,
                    IssuedAt = _dateTimeProvider.ToLocalDateTime(l.IssuedAt),
                    DueAt = dueLocal,
                    ReturnedAt = null,
                    Status = isOverdue ? "Overdue" : "Active",
                    RenewalCount = l.RenewalCount,
                    IsOverdue = isOverdue,
                    DaysOverdue = overdueDays,
                    EstimatedFine = estimatedFine
                };
            }).ToList();

            var historyLoans = historyLoansEntities.Select(l => new StudentLoanItemViewModel
            {
                LoanId = l.LoanId,
                BookTitle = l.BookCopy.Book.Title,
                AccessionNumber = l.BookCopy.AccessionNumber,
                IssuedAt = _dateTimeProvider.ToLocalDateTime(l.IssuedAt),
                DueAt = _dateTimeProvider.ToLocalDateTime(l.DueAt),
                ReturnedAt = l.ReturnedAt.HasValue ? _dateTimeProvider.ToLocalDateTime(l.ReturnedAt.Value) : null,
                Status = l.Status.ToString(),
                RenewalCount = l.RenewalCount,
                IsOverdue = false,
                DaysOverdue = 0,
                EstimatedFine = 0m
            }).ToList();

            var finesAndPayments = allLoans
                .Where(l => l.Fine != null)
                .Select(l =>
                {
                    var fine = l.Fine!;
                    return new StudentFineItemViewModel
                    {
                        FineId = fine.FineId,
                        BookTitle = l.BookCopy.Book.Title,
                        AccessionNumber = l.BookCopy.AccessionNumber,
                        FineAmount = fine.FineAmount,
                        AmountPaid = fine.AmountPaid,
                        WaivedAmount = fine.WaivedAmount,
                        Status = fine.Status.ToString(),
                        CalculatedAt = _dateTimeProvider.ToLocalDateTime(fine.CalculatedAt),
                        ReceiptNumber = fine.FinePayment?.ReceiptNumber,
                        PaidAt = fine.FinePayment != null ? _dateTimeProvider.ToLocalDateTime(fine.FinePayment.ReceivedAt) : null,
                        PaymentMethod = fine.FinePayment?.PaymentMethod.ToString()
                    };
                })
                .OrderByDescending(f => f.CalculatedAt)
                .ToList();

            var outstandingTotal = finesAndPayments
                .Where(f => f.Status != FineStatus.Paid.ToString() && f.Status != FineStatus.Waived.ToString())
                .Sum(f => f.RemainingBalance);

            var overdueCount = activeLoans.Count(l => l.IsOverdue);

            return new StudentDashboardViewModel
            {
                Profile = new StudentProfileViewModel
                {
                    StudentId = student.StudentId,
                    FullName = student.FullName,
                    EnrollmentNumber = student.EnrollmentNumber,
                    DepartmentName = student.Department.DepartmentName,
                    DepartmentCode = student.Department.DepartmentCode,
                    Semester = student.Semester,
                    Email = student.Email,
                    Phone = student.Phone,
                    IsActive = student.IsActive,
                    MemberSince = _dateTimeProvider.ToLocalDateTime(student.CreatedAt)
                },
                ActiveLoansCount = activeLoans.Count,
                MaxBorrowingLimit = setting.MaxBooksPerStudent,
                OverdueLoansCount = overdueCount,
                OutstandingFinesTotal = outstandingTotal,
                TotalCompletedLoans = historyLoans.Count,
                ActiveLoans = activeLoans,
                BorrowingHistory = historyLoans,
                FinesAndPayments = finesAndPayments
            };
        }
    }
}
