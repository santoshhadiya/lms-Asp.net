using College_Library_Management_System.Data;
using College_Library_Management_System.Models.Enums;
using College_Library_Management_System.Services.Common;
using College_Library_Management_System.Services.Interfaces;
using College_Library_Management_System.ViewModels.Circulation;
using College_Library_Management_System.ViewModels.Common;
using College_Library_Management_System.ViewModels.Fines;
using College_Library_Management_System.ViewModels.Reports;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;

namespace College_Library_Management_System.Services
{
    public class ReportService : IReportService
    {
        private readonly ApplicationDbContext _context;
        private readonly IDateTimeProvider _dateTimeProvider;

        public ReportService(ApplicationDbContext context, IDateTimeProvider dateTimeProvider)
        {
            _context = context;
            _dateTimeProvider = dateTimeProvider;
        }

        public async Task<DashboardViewModel> GetDashboardMetricsAsync()
        {
            var today = _dateTimeProvider.Now.Date;
            var setting = await _context.LibrarySettings.FirstOrDefaultAsync();
            decimal dailyRate = setting?.DailyFineRate ?? 5.00m;

            var model = new DashboardViewModel
            {
                TotalBooks = await _context.Books.CountAsync(),
                TotalCopies = await _context.BookCopies.CountAsync(),
                AvailableCopies = await _context.BookCopies.CountAsync(c => c.Status == BookCopyStatus.Available),
                IssuedCopies = await _context.BookCopies.CountAsync(c => c.Status == BookCopyStatus.Issued),
                DamagedCopies = await _context.BookCopies.CountAsync(c => c.Status == BookCopyStatus.Damaged),
                TotalStudents = await _context.Students.CountAsync(s => s.IsActive),
                ActiveLoans = await _context.Loans.CountAsync(l => l.Status == LoanStatus.Active),
                OverdueLoans = await _context.Loans.CountAsync(l => l.Status == LoanStatus.Active && l.DueAt.Date < today),
                TotalPendingFines = await _context.Fines
                    .Where(f => f.Status == FineStatus.Pending)
                    .SumAsync(f => f.FineAmount - f.AmountPaid - f.WaivedAmount),
                TotalFinesCollected = await _context.FinePayments.SumAsync(p => (decimal?)p.Amount) ?? 0m
            };

            // Recent Active Loans
            model.RecentLoans = await _context.Loans
                .AsNoTracking()
                .Include(l => l.Student)
                    .ThenInclude(s => s.Department)
                .Include(l => l.BookCopy)
                    .ThenInclude(c => c.Book)
                .Include(l => l.IssuedByUser)
                .Where(l => l.Status == LoanStatus.Active)
                .OrderByDescending(l => l.IssuedAt)
                .Take(5)
                .Select(l => new ActiveLoanListItemViewModel
                {
                    LoanId = l.LoanId,
                    BookCopyId = l.BookCopyId,
                    AccessionNumber = l.BookCopy.AccessionNumber,
                    BookTitle = l.BookCopy.Book.Title,
                    StudentId = l.StudentId,
                    StudentEnrollment = l.Student.EnrollmentNumber,
                    StudentName = l.Student.FullName,
                    DepartmentCode = l.Student.Department.DepartmentCode,
                    IssuedAt = l.IssuedAt,
                    DueAt = l.DueAt,
                    RenewalCount = l.RenewalCount,
                    IsOverdue = l.DueAt.Date < today,
                    OverdueDays = l.DueAt.Date < today ? (today - l.DueAt.Date).Days : 0,
                    EstimatedFine = l.DueAt.Date < today ? (today - l.DueAt.Date).Days * dailyRate : 0m,
                    IssuedBy = l.IssuedByUser != null ? (l.IssuedByUser.FullName ?? l.IssuedByUser.UserName ?? "Staff") : "Staff"
                })
                .ToListAsync();

            // Overdue Alerts
            model.OverdueAlerts = await _context.Loans
                .AsNoTracking()
                .Include(l => l.Student)
                    .ThenInclude(s => s.Department)
                .Include(l => l.BookCopy)
                    .ThenInclude(c => c.Book)
                .Include(l => l.IssuedByUser)
                .Where(l => l.Status == LoanStatus.Active && l.DueAt.Date < today)
                .OrderBy(l => l.DueAt)
                .Take(5)
                .Select(l => new ActiveLoanListItemViewModel
                {
                    LoanId = l.LoanId,
                    BookCopyId = l.BookCopyId,
                    AccessionNumber = l.BookCopy.AccessionNumber,
                    BookTitle = l.BookCopy.Book.Title,
                    StudentId = l.StudentId,
                    StudentEnrollment = l.Student.EnrollmentNumber,
                    StudentName = l.Student.FullName,
                    DepartmentCode = l.Student.Department.DepartmentCode,
                    IssuedAt = l.IssuedAt,
                    DueAt = l.DueAt,
                    RenewalCount = l.RenewalCount,
                    IsOverdue = true,
                    OverdueDays = (today - l.DueAt.Date).Days,
                    EstimatedFine = (today - l.DueAt.Date).Days * dailyRate,
                    IssuedBy = l.IssuedByUser != null ? (l.IssuedByUser.FullName ?? l.IssuedByUser.UserName ?? "Staff") : "Staff"
                })
                .ToListAsync();

            // Recent Payments
            model.RecentPayments = await _context.FinePayments
                .AsNoTracking()
                .Include(p => p.Fine)
                    .ThenInclude(f => f.Loan)
                        .ThenInclude(l => l.Student)
                            .ThenInclude(s => s.Department)
                .Include(p => p.Fine)
                    .ThenInclude(f => f.Loan)
                        .ThenInclude(l => l.BookCopy)
                            .ThenInclude(c => c.Book)
                .OrderByDescending(p => p.ReceivedAt)
                .Take(5)
                .Select(p => new FineListItemViewModel
                {
                    FineId = p.FineId,
                    LoanId = p.Fine.LoanId,
                    StudentEnrollment = p.Fine.Loan.Student.EnrollmentNumber,
                    StudentName = p.Fine.Loan.Student.FullName,
                    DepartmentCode = p.Fine.Loan.Student.Department.DepartmentCode,
                    BookTitle = p.Fine.Loan.BookCopy.Book.Title,
                    AccessionNumber = p.Fine.Loan.BookCopy.AccessionNumber,
                    FineAmount = p.Fine.FineAmount,
                    AmountPaid = p.Amount,
                    RemainingBalance = 0m,
                    Status = FineStatus.Paid,
                    CalculatedAt = p.Fine.CalculatedAt,
                    ReceiptNumber = p.ReceiptNumber,
                    PaymentDate = p.ReceivedAt
                })
                .ToListAsync();

            // Localize times
            foreach (var item in model.RecentLoans)
            {
                item.IssuedAt = _dateTimeProvider.ToLocalDateTime(item.IssuedAt);
                item.DueAt = _dateTimeProvider.ToLocalDateTime(item.DueAt);
            }
            foreach (var item in model.OverdueAlerts)
            {
                item.IssuedAt = _dateTimeProvider.ToLocalDateTime(item.IssuedAt);
                item.DueAt = _dateTimeProvider.ToLocalDateTime(item.DueAt);
            }
            foreach (var item in model.RecentPayments)
            {
                item.CalculatedAt = _dateTimeProvider.ToLocalDateTime(item.CalculatedAt);
                if (item.PaymentDate.HasValue) item.PaymentDate = _dateTimeProvider.ToLocalDateTime(item.PaymentDate.Value);
            }

            return model;
        }

        public async Task<OverdueReportFilterViewModel> GetOverdueReportAsync(OverdueReportFilterViewModel filter)
        {
            var today = _dateTimeProvider.Now.Date;
            var setting = await _context.LibrarySettings.FirstOrDefaultAsync();
            decimal dailyRate = setting?.DailyFineRate ?? 5.00m;

            var query = _context.Loans
                .AsNoTracking()
                .Include(l => l.Student)
                    .ThenInclude(s => s.Department)
                .Include(l => l.BookCopy)
                    .ThenInclude(c => c.Book)
                .Where(l => l.Status == LoanStatus.Active && l.DueAt.Date < today);

            if (filter.DepartmentId.HasValue && filter.DepartmentId.Value > 0)
            {
                query = query.Where(l => l.Student.DepartmentId == filter.DepartmentId.Value);
            }

            filter.DepartmentOptions = await _context.Departments
                .AsNoTracking()
                .OrderBy(d => d.DepartmentName)
                .Select(d => new SelectListItem
                {
                    Value = d.DepartmentId.ToString(),
                    Text = $"{d.DepartmentName} ({d.DepartmentCode})"
                })
                .ToListAsync();

            var list = await query
                .OrderBy(l => l.DueAt)
                .Select(l => new OverdueReportItemViewModel
                {
                    LoanId = l.LoanId,
                    StudentName = l.Student.FullName,
                    EnrollmentNumber = l.Student.EnrollmentNumber,
                    DepartmentCode = l.Student.Department.DepartmentCode,
                    Phone = l.Student.Phone,
                    Email = l.Student.Email,
                    BookTitle = l.BookCopy.Book.Title,
                    AccessionNumber = l.BookCopy.AccessionNumber,
                    ShelfLocation = l.BookCopy.ShelfLocation,
                    IssuedAt = l.IssuedAt,
                    DueAt = l.DueAt,
                    OverdueDays = (today - l.DueAt.Date).Days,
                    EstimatedFine = (today - l.DueAt.Date).Days * dailyRate
                })
                .ToListAsync();

            if (filter.MinDaysOverdue.HasValue && filter.MinDaysOverdue.Value > 0)
            {
                list = list.Where(item => item.OverdueDays >= filter.MinDaysOverdue.Value).ToList();
            }

            foreach (var item in list)
            {
                item.IssuedAt = _dateTimeProvider.ToLocalDateTime(item.IssuedAt);
                item.DueAt = _dateTimeProvider.ToLocalDateTime(item.DueAt);
            }

            filter.Items = list;
            filter.TotalOverdueCount = list.Count;
            filter.TotalEstimatedFine = list.Sum(i => i.EstimatedFine);

            return filter;
        }

        public async Task<InventoryReportViewModel> GetInventoryReportAsync()
        {
            var model = new InventoryReportViewModel
            {
                TotalCatalogTitles = await _context.Books.CountAsync(),
                TotalPhysicalCopies = await _context.BookCopies.CountAsync(),
                AvailableCount = await _context.BookCopies.CountAsync(c => c.Status == BookCopyStatus.Available),
                IssuedCount = await _context.BookCopies.CountAsync(c => c.Status == BookCopyStatus.Issued),
                DamagedCount = await _context.BookCopies.CountAsync(c => c.Status == BookCopyStatus.Damaged),
                LostCount = await _context.BookCopies.CountAsync(c => c.Status == BookCopyStatus.Lost),
                WithdrawnCount = await _context.BookCopies.CountAsync(c => c.Status == BookCopyStatus.Withdrawn)
            };

            model.CategoryBreakdown = await _context.Categories
                .AsNoTracking()
                .OrderBy(c => c.Name)
                .Select(c => new CategoryInventoryBreakdownViewModel
                {
                    CategoryName = c.Name,
                    BookCount = c.Books.Count,
                    CopyCount = c.Books.SelectMany(b => b.Copies).Count(),
                    AvailableCopies = c.Books.SelectMany(b => b.Copies).Count(cp => cp.Status == BookCopyStatus.Available),
                    IssuedCopies = c.Books.SelectMany(b => b.Copies).Count(cp => cp.Status == BookCopyStatus.Issued)
                })
                .ToListAsync();

            model.DepartmentBreakdown = await _context.Departments
                .AsNoTracking()
                .OrderBy(d => d.DepartmentName)
                .Select(d => new DepartmentStudentBreakdownViewModel
                {
                    DepartmentName = d.DepartmentName,
                    DepartmentCode = d.DepartmentCode,
                    StudentCount = d.Students.Count,
                    ActiveLoansCount = d.Students.SelectMany(s => s.Loans).Count(l => l.Status == LoanStatus.Active)
                })
                .ToListAsync();

            return model;
        }

        public async Task<CollectionReportFilterViewModel> GetCollectionReportAsync(CollectionReportFilterViewModel filter)
        {
            var query = _context.FinePayments
                .AsNoTracking()
                .Include(p => p.ReceivedByUser)
                .Include(p => p.Fine)
                    .ThenInclude(f => f.Loan)
                        .ThenInclude(l => l.Student)
                            .ThenInclude(s => s.Department)
                .Include(p => p.Fine)
                    .ThenInclude(f => f.Loan)
                        .ThenInclude(l => l.BookCopy)
                            .ThenInclude(c => c.Book)
                .AsQueryable();

            if (filter.StartDate.HasValue)
            {
                query = query.Where(p => p.ReceivedAt >= filter.StartDate.Value.Date);
            }

            if (filter.EndDate.HasValue)
            {
                var endExclusive = filter.EndDate.Value.Date.AddDays(1);
                query = query.Where(p => p.ReceivedAt < endExclusive);
            }

            if (filter.PaymentMethod.HasValue)
            {
                query = query.Where(p => p.PaymentMethod == filter.PaymentMethod.Value);
            }

            query = query.OrderByDescending(p => p.ReceivedAt);

            filter.Transactions = await query
                .Select(p => new CollectionTransactionItemViewModel
                {
                    ReceiptNumber = p.ReceiptNumber,
                    PaymentDate = p.ReceivedAt,
                    Amount = p.Amount,
                    PaymentMethod = p.PaymentMethod,
                    StudentName = p.Fine.Loan.Student.FullName,
                    EnrollmentNumber = p.Fine.Loan.Student.EnrollmentNumber,
                    DepartmentCode = p.Fine.Loan.Student.Department.DepartmentCode,
                    BookTitle = p.Fine.Loan.BookCopy.Book.Title,
                    AccessionNumber = p.Fine.Loan.BookCopy.AccessionNumber,
                    CashierName = p.ReceivedByUser != null ? (p.ReceivedByUser.FullName ?? p.ReceivedByUser.UserName ?? "Cashier") : "Cashier"
                })
                .ToListAsync();

            foreach (var t in filter.Transactions)
            {
                t.PaymentDate = _dateTimeProvider.ToLocalDateTime(t.PaymentDate);
            }

            filter.TransactionsCount = filter.Transactions.Count;
            filter.TotalCollected = filter.Transactions.Sum(t => t.Amount);

            return filter;
        }

        public async Task<PaginatedList<AuditLogListItemViewModel>> GetAuditLogsAsync(AuditLogReportFilterViewModel filter)
        {
            var query = _context.AuditLogs
                .AsNoTracking()
                .AsQueryable();

            if (!string.IsNullOrWhiteSpace(filter.Action))
            {
                query = query.Where(a => a.Action.ToLower() == filter.Action.Trim().ToLower());
            }

            if (!string.IsNullOrWhiteSpace(filter.EntityType))
            {
                query = query.Where(a => a.EntityType.ToLower() == filter.EntityType.Trim().ToLower());
            }

            if (!string.IsNullOrWhiteSpace(filter.SearchTerm))
            {
                var term = filter.SearchTerm.Trim().ToLower();
                query = query.Where(a =>
                    a.Action.ToLower().Contains(term) ||
                    a.EntityType.ToLower().Contains(term) ||
                    (a.Details != null && a.Details.ToLower().Contains(term)) ||
                    (a.UserEmail != null && a.UserEmail.ToLower().Contains(term)));
            }

            if (filter.StartDate.HasValue)
            {
                query = query.Where(a => a.Timestamp >= filter.StartDate.Value.Date);
            }

            if (filter.EndDate.HasValue)
            {
                var endExclusive = filter.EndDate.Value.Date.AddDays(1);
                query = query.Where(a => a.Timestamp < endExclusive);
            }

            query = query.OrderByDescending(a => a.Timestamp);

            var totalCount = await query.CountAsync();
            var items = await query
                .Skip((filter.PageNumber - 1) * filter.PageSize)
                .Take(filter.PageSize)
                .Select(a => new AuditLogListItemViewModel
                {
                    AuditLogId = a.AuditLogId,
                    Action = a.Action,
                    EntityType = a.EntityType,
                    EntityId = a.EntityId ?? string.Empty,
                    PerformedByUserId = a.UserId ?? string.Empty,
                    PerformedByUserEmail = a.UserEmail,
                    Timestamp = a.Timestamp,
                    Details = a.Details
                })
                .ToListAsync();

            foreach (var item in items)
            {
                item.Timestamp = _dateTimeProvider.ToLocalDateTime(item.Timestamp);
            }

            return new PaginatedList<AuditLogListItemViewModel>(items, totalCount, filter.PageNumber, filter.PageSize);
        }
    }
}
