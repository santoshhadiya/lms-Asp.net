using College_Library_Management_System.Data;
using College_Library_Management_System.Models;
using College_Library_Management_System.Models.Enums;
using College_Library_Management_System.Services.Common;
using College_Library_Management_System.Services.Interfaces;
using College_Library_Management_System.ViewModels.Common;
using College_Library_Management_System.ViewModels.Fines;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;

namespace College_Library_Management_System.Services
{
    public class FineService : IFineService
    {
        private readonly ApplicationDbContext _context;
        private readonly IAuditLogService _auditService;
        private readonly IDateTimeProvider _dateTimeProvider;

        public FineService(
            ApplicationDbContext context,
            IAuditLogService auditService,
            IDateTimeProvider dateTimeProvider)
        {
            _context = context;
            _auditService = auditService;
            _dateTimeProvider = dateTimeProvider;
        }

        public async Task<PaginatedList<FineListItemViewModel>> GetFinesAsync(FineListFilterViewModel filter)
        {
            var query = _context.Fines
                .AsNoTracking()
                .Include(f => f.Loan)
                    .ThenInclude(l => l.Student)
                        .ThenInclude(s => s.Department)
                .Include(f => f.Loan)
                    .ThenInclude(l => l.BookCopy)
                        .ThenInclude(bc => bc.Book)
                .Include(f => f.FinePayment)
                .AsQueryable();

            if (!string.IsNullOrWhiteSpace(filter.SearchTerm))
            {
                var term = filter.SearchTerm.Trim().ToLower();
                query = query.Where(f =>
                    f.Loan.Student.EnrollmentNumber.ToLower().Contains(term) ||
                    f.Loan.Student.FullName.ToLower().Contains(term) ||
                    f.Loan.BookCopy.AccessionNumber.ToLower().Contains(term) ||
                    f.Loan.BookCopy.Book.Title.ToLower().Contains(term) ||
                    (f.FinePayment != null && f.FinePayment.ReceiptNumber.ToLower().Contains(term)));
            }

            if (filter.Status.HasValue)
            {
                query = query.Where(f => f.Status == filter.Status.Value);
            }

            if (filter.DepartmentId.HasValue && filter.DepartmentId.Value > 0)
            {
                query = query.Where(f => f.Loan.Student.DepartmentId == filter.DepartmentId.Value);
            }

            query = query.OrderByDescending(f => f.CalculatedAt);

            var totalCount = await query.CountAsync();
            var items = await query
                .Skip((filter.PageNumber - 1) * filter.PageSize)
                .Take(filter.PageSize)
                .Select(f => new FineListItemViewModel
                {
                    FineId = f.FineId,
                    LoanId = f.LoanId,
                    StudentId = f.Loan.StudentId,
                    StudentEnrollment = f.Loan.Student.EnrollmentNumber,
                    StudentName = f.Loan.Student.FullName,
                    DepartmentCode = f.Loan.Student.Department.DepartmentCode,
                    BookTitle = f.Loan.BookCopy.Book.Title,
                    AccessionNumber = f.Loan.BookCopy.AccessionNumber,
                    FineAmount = f.FineAmount,
                    DailyFineRate = f.DailyFineRateApplied,
                    AmountPaid = f.AmountPaid,
                    WaivedAmount = f.WaivedAmount,
                    RemainingBalance = f.FineAmount - f.AmountPaid - f.WaivedAmount,
                    Status = f.Status,
                    CalculatedAt = f.CalculatedAt,
                    ReceiptNumber = f.FinePayment != null ? f.FinePayment.ReceiptNumber : null,
                    PaymentDate = f.FinePayment != null ? f.FinePayment.ReceivedAt : null
                })
                .ToListAsync();

            foreach (var item in items)
            {
                item.CalculatedAt = _dateTimeProvider.ToLocalDateTime(item.CalculatedAt);
                if (item.PaymentDate.HasValue)
                {
                    item.PaymentDate = _dateTimeProvider.ToLocalDateTime(item.PaymentDate.Value);
                }
            }

            return new PaginatedList<FineListItemViewModel>(items, totalCount, filter.PageNumber, filter.PageSize);
        }

        public async Task<CollectFinePaymentViewModel?> GetFineForPaymentAsync(int fineId)
        {
            var fine = await _context.Fines
                .Include(f => f.Loan)
                    .ThenInclude(l => l.Student)
                        .ThenInclude(s => s.Department)
                .Include(f => f.Loan)
                    .ThenInclude(l => l.BookCopy)
                        .ThenInclude(bc => bc.Book)
                .FirstOrDefaultAsync(f => f.FineId == fineId);

            if (fine == null) return null;

            var balance = fine.RemainingBalance;

            return new CollectFinePaymentViewModel
            {
                FineId = fine.FineId,
                StudentEnrollment = fine.Loan.Student.EnrollmentNumber,
                StudentName = fine.Loan.Student.FullName,
                DepartmentName = fine.Loan.Student.Department.DepartmentName,
                BookTitle = fine.Loan.BookCopy.Book.Title,
                AccessionNumber = fine.Loan.BookCopy.AccessionNumber,
                LoanIssuedAt = _dateTimeProvider.ToLocalDateTime(fine.Loan.IssuedAt),
                LoanDueAt = _dateTimeProvider.ToLocalDateTime(fine.Loan.DueAt),
                LoanReturnedAt = fine.Loan.ReturnedAt.HasValue ? _dateTimeProvider.ToLocalDateTime(fine.Loan.ReturnedAt.Value) : null,
                FineAmount = fine.FineAmount,
                AmountPaid = fine.AmountPaid,
                WaivedAmount = fine.WaivedAmount,
                RemainingBalance = balance,
                PaymentAmount = balance, // Pre-filled with full outstanding balance
                PaymentMethod = PaymentMethod.Cash
            };
        }

        public async Task<(bool Success, string Message, string? ReceiptNumber)> CollectFullPaymentAsync(
            CollectFinePaymentViewModel model,
            string userId,
            string userEmail)
        {
            var fine = await _context.Fines
                .Include(f => f.Loan)
                    .ThenInclude(l => l.Student)
                .Include(f => f.Loan)
                    .ThenInclude(l => l.BookCopy)
                .FirstOrDefaultAsync(f => f.FineId == model.FineId);

            if (fine == null)
            {
                return (false, "Fine record not found.", null);
            }

            if (fine.Status == FineStatus.Paid)
            {
                return (false, "This fine has already been fully paid.", null);
            }

            if (fine.Status == FineStatus.Waived)
            {
                return (false, "This fine was previously waived and cannot accept payment.", null);
            }

            var balance = fine.RemainingBalance;

            // Business rule: Partial payments are strictly prohibited
            if (model.PaymentAmount <= 0)
            {
                return (false, "Payment amount must be greater than zero.", null);
            }

            if (model.PaymentAmount != balance)
            {
                return (false, $"Partial payments are prohibited. The full outstanding balance of ₹{balance:N2} must be paid in a single offline transaction.", null);
            }

            var now = _dateTimeProvider.Now;
            // Generate unique professional receipt number: REC-yyyyMMdd-XXXXXX
            var receiptNumber = $"REC-{now:yyyyMMdd}-{Guid.NewGuid().ToString()[..6].ToUpper()}";

            var payment = new FinePayment
            {
                FineId = fine.FineId,
                Amount = model.PaymentAmount,
                PaymentMethod = model.PaymentMethod,
                ReceivedAt = now,
                ReceiptNumber = receiptNumber,
                ReceivedByUserId = userId,
                Remarks = model.Remarks?.Trim()
            };

            fine.AmountPaid += model.PaymentAmount;
            fine.Status = FineStatus.Paid;

            _context.FinePayments.Add(payment);

            await _context.SaveChangesAsync();

            await _auditService.LogAsync(
                "FinePayment",
                nameof(FinePayment),
                payment.FinePaymentId.ToString(),
                userId,
                userEmail,
                $"Collected full offline fine payment of ₹{model.PaymentAmount:N2} via {model.PaymentMethod} for student {fine.Loan.Student.EnrollmentNumber}. Receipt: {receiptNumber}.");

            return (true, $"Payment of ₹{model.PaymentAmount:N2} successfully recorded. Receipt #{receiptNumber} generated.", receiptNumber);
        }

        public async Task<WaiveFineViewModel?> GetFineForWaiverAsync(int fineId)
        {
            var fine = await _context.Fines
                .Include(f => f.Loan)
                    .ThenInclude(l => l.Student)
                .Include(f => f.Loan)
                    .ThenInclude(l => l.BookCopy)
                        .ThenInclude(bc => bc.Book)
                .FirstOrDefaultAsync(f => f.FineId == fineId);

            if (fine == null) return null;

            return new WaiveFineViewModel
            {
                FineId = fine.FineId,
                StudentEnrollment = fine.Loan.Student.EnrollmentNumber,
                StudentName = fine.Loan.Student.FullName,
                BookTitle = fine.Loan.BookCopy.Book.Title,
                FineAmount = fine.FineAmount,
                RemainingBalance = fine.RemainingBalance
            };
        }

        public async Task<(bool Success, string Message)> WaiveFineAsync(
            WaiveFineViewModel model,
            string userId,
            string userEmail)
        {
            var fine = await _context.Fines
                .Include(f => f.Loan)
                    .ThenInclude(l => l.Student)
                .FirstOrDefaultAsync(f => f.FineId == model.FineId);

            if (fine == null)
            {
                return (false, "Fine record not found.");
            }

            if (fine.Status != FineStatus.Pending)
            {
                return (false, $"Only pending fines can be waived (Current status: {fine.Status}).");
            }

            if (string.IsNullOrWhiteSpace(model.WaiverReason) || model.WaiverReason.Trim().Length < 10)
            {
                return (false, "A detailed administrative justification (minimum 10 characters) is mandatory to waive a fine.");
            }

            var waivedBalance = fine.RemainingBalance;
            fine.WaivedAmount = waivedBalance;
            fine.Status = FineStatus.Waived;
            fine.WaivedByUserId = userId;
            fine.WaiverReason = model.WaiverReason.Trim();

            await _context.SaveChangesAsync();

            await _auditService.LogAsync(
                "FineWaive",
                nameof(Fine),
                fine.FineId.ToString(),
                userId,
                userEmail,
                $"Waived fine #{fine.FineId} of ₹{waivedBalance:N2} for student {fine.Loan.Student.EnrollmentNumber}. Justification: {fine.WaiverReason}");

            return (true, $"Fine of ₹{waivedBalance:N2} has been successfully waived.");
        }

        public async Task<FineReceiptViewModel?> GetReceiptByNumberAsync(string receiptNumber)
        {
            var cleanReceipt = (receiptNumber ?? string.Empty).Trim().ToUpper();
            var payment = await _context.FinePayments
                .Include(p => p.ReceivedByUser)
                .Include(p => p.Fine)
                    .ThenInclude(f => f.Loan)
                        .ThenInclude(l => l.Student)
                            .ThenInclude(s => s.Department)
                .Include(p => p.Fine)
                    .ThenInclude(f => f.Loan)
                        .ThenInclude(l => l.BookCopy)
                            .ThenInclude(bc => bc.Book)
                .FirstOrDefaultAsync(p => p.ReceiptNumber.ToUpper() == cleanReceipt);

            if (payment == null) return null;

            return BuildReceiptViewModel(payment);
        }

        public async Task<FineReceiptViewModel?> GetReceiptByFineIdAsync(int fineId)
        {
            var payment = await _context.FinePayments
                .Include(p => p.ReceivedByUser)
                .Include(p => p.Fine)
                    .ThenInclude(f => f.Loan)
                        .ThenInclude(l => l.Student)
                            .ThenInclude(s => s.Department)
                .Include(p => p.Fine)
                    .ThenInclude(f => f.Loan)
                        .ThenInclude(l => l.BookCopy)
                            .ThenInclude(bc => bc.Book)
                .FirstOrDefaultAsync(p => p.FineId == fineId);

            if (payment == null) return null;

            return BuildReceiptViewModel(payment);
        }

        private FineReceiptViewModel BuildReceiptViewModel(FinePayment payment)
        {
            var loan = payment.Fine.Loan;
            var returnDate = loan.ReturnedAt ?? payment.ReceivedAt;
            int overdueDays = Math.Max(0, (returnDate.Date - loan.DueAt.Date).Days);

            return new FineReceiptViewModel
            {
                ReceiptNumber = payment.ReceiptNumber,
                FineId = payment.FineId,
                PaymentId = payment.FinePaymentId,
                PaymentDate = _dateTimeProvider.ToLocalDateTime(payment.ReceivedAt),
                PaymentMethod = payment.PaymentMethod,
                AmountPaid = payment.Amount,
                StudentEnrollment = loan.Student.EnrollmentNumber,
                StudentName = loan.Student.FullName,
                DepartmentName = loan.Student.Department.DepartmentName,
                BookTitle = loan.BookCopy.Book.Title,
                AccessionNumber = loan.BookCopy.AccessionNumber,
                OverdueDays = overdueDays,
                DailyFineRate = payment.Fine.DailyFineRateApplied,
                ReceivedByStaffName = payment.ReceivedByUser?.FullName ?? payment.ReceivedByUser?.UserName ?? "Library Cashier",
                Remarks = payment.Remarks
            };
        }

        public async Task<List<SelectListItem>> GetDepartmentOptionsAsync()
        {
            return await _context.Departments
                .AsNoTracking()
                .OrderBy(d => d.DepartmentName)
                .Select(d => new SelectListItem
                {
                    Value = d.DepartmentId.ToString(),
                    Text = $"{d.DepartmentName} ({d.DepartmentCode})"
                })
                .ToListAsync();
        }
    }
}
