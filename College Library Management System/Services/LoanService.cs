using College_Library_Management_System.Data;
using College_Library_Management_System.Models;
using College_Library_Management_System.Models.Enums;
using College_Library_Management_System.Services.Common;
using College_Library_Management_System.Services.Interfaces;
using College_Library_Management_System.ViewModels.Circulation;
using College_Library_Management_System.ViewModels.Common;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;

namespace College_Library_Management_System.Services
{
    public class LoanService : ILoanService
    {
        private readonly ApplicationDbContext _context;
        private readonly IAuditLogService _auditService;
        private readonly IDateTimeProvider _dateTimeProvider;

        public LoanService(
            ApplicationDbContext context,
            IAuditLogService auditService,
            IDateTimeProvider dateTimeProvider)
        {
            _context = context;
            _auditService = auditService;
            _dateTimeProvider = dateTimeProvider;
        }

        public async Task<LibrarySetting> GetLibrarySettingAsync()
        {
            var setting = await _context.LibrarySettings.FirstOrDefaultAsync();
            if (setting == null)
            {
                setting = new LibrarySetting
                {
                    LoanPeriodDays = 14,
                    DailyFineRate = 5.00m,
                    MaxBooksPerStudent = 3,
                    BlockIssueWhenOverdue = true,
                    AllowRenewal = true,
                    MaxRenewalsPerLoan = 1,
                    UpdatedAt = _dateTimeProvider.Now
                };
                _context.LibrarySettings.Add(setting);
                await _context.SaveChangesAsync();
            }
            return setting;
        }

        public async Task<CirculationEligibilityResult> CheckIssueEligibilityAsync(string enrollmentNumber, string accessionNumber)
        {
            var result = new CirculationEligibilityResult();
            var setting = await GetLibrarySettingAsync();
            result.MaxAllowedLoans = setting.MaxBooksPerStudent;

            var cleanEnrollment = (enrollmentNumber ?? string.Empty).Trim().ToUpper();
            var cleanAccession = (accessionNumber ?? string.Empty).Trim().ToUpper();

            // 1. Validate Student
            if (string.IsNullOrWhiteSpace(cleanEnrollment))
            {
                result.IsEligible = false;
                result.Message = "Please enter student enrollment number.";
                return result;
            }

            var student = await _context.Students
                .Include(s => s.Department)
                .Include(s => s.Loans)
                    .ThenInclude(l => l.BookCopy)
                        .ThenInclude(bc => bc.Book)
                .FirstOrDefaultAsync(s => s.EnrollmentNumber.ToUpper() == cleanEnrollment);

            if (student == null)
            {
                result.IsEligible = false;
                result.Message = $"Student with enrollment '{cleanEnrollment}' does not exist.";
                return result;
            }

            result.StudentId = student.StudentId;
            result.StudentName = student.FullName;
            result.EnrollmentNumber = student.EnrollmentNumber;
            result.DepartmentName = student.Department.DepartmentName;
            result.IsStudentActive = student.IsActive;

            if (!student.IsActive)
            {
                result.IsEligible = false;
                result.Message = $"Student account for {student.FullName} is inactive or suspended. Cannot issue books.";
                return result;
            }

            var today = _dateTimeProvider.Now.Date;
            var activeLoans = student.Loans.Where(l => l.Status == LoanStatus.Active).ToList();
            result.CurrentActiveLoans = activeLoans.Count;

            // Check active loan limit (3 books max)
            if (activeLoans.Count >= setting.MaxBooksPerStudent)
            {
                result.IsEligible = false;
                result.Message = $"{student.FullName} has reached the maximum borrowing limit of {setting.MaxBooksPerStudent} books ({activeLoans.Count} currently issued).";
                return result;
            }

            // Check if student has any overdue unreturned books
            var overdueLoans = activeLoans.Where(l => l.DueAt.Date < today).ToList();
            if (overdueLoans.Any())
            {
                result.HasOverdueLoans = true;
                result.OverdueBookTitles = overdueLoans.Select(l => $"{l.BookCopy.Book.Title} (Due: {l.DueAt:dd MMM yyyy})").ToList();

                if (setting.BlockIssueWhenOverdue)
                {
                    result.IsEligible = false;
                    result.Message = $"{student.FullName} has {overdueLoans.Count} overdue unreturned book(s). Overdue books must be returned before new books can be issued.";
                    return result;
                }
            }

            // 2. Validate Book Copy
            if (string.IsNullOrWhiteSpace(cleanAccession))
            {
                result.IsEligible = false;
                result.Message = "Please enter book copy accession number.";
                return result;
            }

            var copy = await _context.BookCopies
                .Include(c => c.Book)
                .FirstOrDefaultAsync(c => c.AccessionNumber.ToUpper() == cleanAccession);

            if (copy == null)
            {
                result.IsEligible = false;
                result.Message = $"Book copy with accession number '{cleanAccession}' not found in inventory.";
                return result;
            }

            result.BookCopyId = copy.BookCopyId;
            result.AccessionNumber = copy.AccessionNumber;
            result.BookTitle = copy.Book.Title;
            result.ShelfLocation = copy.ShelfLocation;
            result.CopyStatus = copy.Status;

            if (copy.Status != BookCopyStatus.Available)
            {
                result.IsEligible = false;
                result.Message = $"Copy '{copy.AccessionNumber}' ({copy.Book.Title}) is currently marked as {copy.Status} and cannot be issued.";
                return result;
            }

            result.IsEligible = true;
            result.Message = $"Eligible for loan. Book: \"{copy.Book.Title}\" -> Student: {student.FullName} ({student.EnrollmentNumber}).";
            return result;
        }

        public async Task<(bool Success, string Message, int? LoanId)> IssueBookAsync(
            IssueBookViewModel model,
            string userId,
            string userEmail)
        {
            var setting = await GetLibrarySettingAsync();
            var cleanEnrollment = (model.StudentEnrollment ?? string.Empty).Trim().ToUpper();
            var cleanAccession = (model.AccessionNumber ?? string.Empty).Trim().ToUpper();

            var student = await _context.Students
                .Include(s => s.Loans)
                    .ThenInclude(l => l.BookCopy)
                        .ThenInclude(bc => bc.Book)
                .FirstOrDefaultAsync(s => s.EnrollmentNumber.ToUpper() == cleanEnrollment);

            if (student == null)
            {
                return (false, $"Student with enrollment '{cleanEnrollment}' not found.", null);
            }

            if (!student.IsActive)
            {
                return (false, "Student account is inactive. Books cannot be issued.", null);
            }

            var today = _dateTimeProvider.Now.Date;
            var activeLoans = student.Loans.Where(l => l.Status == LoanStatus.Active).ToList();

            if (activeLoans.Count >= setting.MaxBooksPerStudent)
            {
                return (false, $"Student has reached the maximum borrowing limit ({setting.MaxBooksPerStudent} books).", null);
            }

            var hasOverdue = activeLoans.Any(l => l.DueAt.Date < today);
            if (hasOverdue && setting.BlockIssueWhenOverdue)
            {
                return (false, "Student has an overdue book that must be returned before borrowing new books.", null);
            }

            var copy = await _context.BookCopies
                .Include(c => c.Book)
                .FirstOrDefaultAsync(c => c.AccessionNumber.ToUpper() == cleanAccession);

            if (copy == null)
            {
                return (false, $"Book copy with accession number '{cleanAccession}' not found.", null);
            }

            if (copy.Status != BookCopyStatus.Available)
            {
                return (false, $"Book copy '{copy.AccessionNumber}' is not available (Current status: {copy.Status}).", null);
            }

            int loanDays = model.LoanPeriodDays > 0 ? model.LoanPeriodDays : setting.LoanPeriodDays;
            var now = _dateTimeProvider.Now;
            var dueAt = now.AddDays(loanDays);

            copy.Status = BookCopyStatus.Issued;

            var loan = new Loan
            {
                StudentId = student.StudentId,
                BookCopyId = copy.BookCopyId,
                IssuedAt = now,
                DueAt = dueAt,
                Status = LoanStatus.Active,
                RenewalCount = 0,
                IssuedByUserId = userId
            };

            _context.Loans.Add(loan);

            try
            {
                await _context.SaveChangesAsync();

                await _auditService.LogAsync(
                    "Issue",
                    nameof(Loan),
                    loan.LoanId.ToString(),
                    userId,
                    userEmail,
                    $"Issued copy {copy.AccessionNumber} ({copy.Book.Title}) to student {student.EnrollmentNumber} ({student.FullName}). Due: {dueAt:dd MMM yyyy}.");

                return (true, $"Book \"{copy.Book.Title}\" successfully issued to {student.FullName}. Due date: {dueAt:dd MMM yyyy}.", loan.LoanId);
            }
            catch (DbUpdateException)
            {
                return (false, "Concurrency Conflict: This book copy has just been issued or modified by another transaction. Please refresh and try again.", null);
            }
        }

        public async Task<ReturnBookViewModel?> GetLoanForReturnAsync(int loanId)
        {
            var loan = await _context.Loans
                .Include(l => l.Student)
                    .ThenInclude(s => s.Department)
                .Include(l => l.BookCopy)
                    .ThenInclude(bc => bc.Book)
                .FirstOrDefaultAsync(l => l.LoanId == loanId);

            if (loan == null) return null;

            return BuildReturnViewModel(loan, await GetLibrarySettingAsync());
        }

        public async Task<ReturnBookViewModel?> GetActiveLoanByAccessionAsync(string accessionNumber)
        {
            var cleanAccession = (accessionNumber ?? string.Empty).Trim().ToUpper();
            var loan = await _context.Loans
                .Include(l => l.Student)
                    .ThenInclude(s => s.Department)
                .Include(l => l.BookCopy)
                    .ThenInclude(bc => bc.Book)
                .FirstOrDefaultAsync(l => l.Status == LoanStatus.Active && l.BookCopy.AccessionNumber.ToUpper() == cleanAccession);

            if (loan == null) return null;

            return BuildReturnViewModel(loan, await GetLibrarySettingAsync());
        }

        private ReturnBookViewModel BuildReturnViewModel(Loan loan, LibrarySetting setting)
        {
            var now = _dateTimeProvider.Now;
            var today = now.Date;
            var dueDate = loan.DueAt.Date;
            int overdueDays = Math.Max(0, (today - dueDate).Days);
            decimal fine = overdueDays * setting.DailyFineRate;

            return new ReturnBookViewModel
            {
                LoanId = loan.LoanId,
                BookCopyId = loan.BookCopyId,
                AccessionNumber = loan.BookCopy.AccessionNumber,
                BookTitle = loan.BookCopy.Book.Title,
                StudentEnrollment = loan.Student.EnrollmentNumber,
                StudentName = loan.Student.FullName,
                DepartmentCode = loan.Student.Department.DepartmentCode,
                IssuedAt = _dateTimeProvider.ToLocalDateTime(loan.IssuedAt),
                DueAt = _dateTimeProvider.ToLocalDateTime(loan.DueAt),
                ReturnDate = now,
                IsOverdue = overdueDays > 0,
                OverdueDays = overdueDays,
                DailyFineRate = setting.DailyFineRate,
                CalculatedFine = fine,
                ReturnCondition = BookCopyStatus.Available,
                ConditionNotes = loan.BookCopy.ConditionNotes
            };
        }

        public async Task<(bool Success, string Message, decimal FineAmount)> ReturnBookAsync(
            ReturnBookViewModel model,
            string userId,
            string userEmail)
        {
            var loan = await _context.Loans
                .Include(l => l.Student)
                .Include(l => l.BookCopy)
                    .ThenInclude(bc => bc.Book)
                .FirstOrDefaultAsync(l => l.LoanId == model.LoanId);

            if (loan == null)
            {
                return (false, "Loan record not found.", 0m);
            }

            if (loan.Status != LoanStatus.Active)
            {
                return (false, "This loan is already returned or closed.", 0m);
            }

            var setting = await GetLibrarySettingAsync();
            var returnTime = _dateTimeProvider.Now;
            var returnDate = returnTime.Date;
            var dueDate = loan.DueAt.Date;

            // Business rule: Overdue is max(0, ReturnDate.Date - DueDate.Date) * DailyFineRate
            // If returned on due date, overdueDays = 0, fine = 0.
            int overdueDays = Math.Max(0, (returnDate - dueDate).Days);
            decimal fineAmount = overdueDays * setting.DailyFineRate;

            loan.Status = model.ReturnCondition == BookCopyStatus.Lost ? LoanStatus.Lost : LoanStatus.Returned;
            loan.ReturnedAt = returnTime;
            loan.ReceivedByUserId = userId;

            // Update physical copy status & condition
            var copy = loan.BookCopy;
            copy.Status = model.ReturnCondition;
            if (!string.IsNullOrWhiteSpace(model.ConditionNotes))
            {
                copy.ConditionNotes = model.ConditionNotes.Trim();
            }

            if (fineAmount > 0)
            {
                var fine = new Fine
                {
                    LoanId = loan.LoanId,
                    FineAmount = fineAmount,
                    DailyFineRateApplied = setting.DailyFineRate,
                    AmountPaid = 0m,
                    WaivedAmount = 0m,
                    Status = FineStatus.Pending,
                    CalculatedAt = returnTime
                };
                _context.Fines.Add(fine);
            }

            await _context.SaveChangesAsync();

            string fineNotice = fineAmount > 0
                ? $" Overdue fine of ₹{fineAmount:N2} ({overdueDays} days @ ₹{setting.DailyFineRate}/day) assessed."
                : " Returned on time with ₹0 fine.";

            await _auditService.LogAsync(
                "Return",
                nameof(Loan),
                loan.LoanId.ToString(),
                userId,
                userEmail,
                $"Returned copy {copy.AccessionNumber} for student {loan.Student.EnrollmentNumber}. Condition: {model.ReturnCondition}.{fineNotice}");

            return (true, $"Book \"{copy.Book.Title}\" successfully returned.{fineNotice}", fineAmount);
        }

        public async Task<RenewLoanViewModel?> GetLoanForRenewalAsync(int loanId)
        {
            var loan = await _context.Loans
                .Include(l => l.Student)
                .Include(l => l.BookCopy)
                    .ThenInclude(bc => bc.Book)
                .FirstOrDefaultAsync(l => l.LoanId == loanId);

            if (loan == null) return null;

            var setting = await GetLibrarySettingAsync();
            var today = _dateTimeProvider.Now.Date;
            var isOverdue = loan.DueAt.Date < today;

            var model = new RenewLoanViewModel
            {
                LoanId = loan.LoanId,
                AccessionNumber = loan.BookCopy.AccessionNumber,
                BookTitle = loan.BookCopy.Book.Title,
                StudentEnrollment = loan.Student.EnrollmentNumber,
                StudentName = loan.Student.FullName,
                CurrentDueAt = _dateTimeProvider.ToLocalDateTime(loan.DueAt),
                NewDueAt = _dateTimeProvider.ToLocalDateTime(loan.DueAt.AddDays(setting.LoanPeriodDays)),
                RenewalCount = loan.RenewalCount,
                MaxRenewals = setting.MaxRenewalsPerLoan
            };

            if (loan.Status != LoanStatus.Active)
            {
                model.IsEligible = false;
                model.IneligibilityReason = "Loan is no longer active.";
            }
            else if (!setting.AllowRenewal)
            {
                model.IsEligible = false;
                model.IneligibilityReason = "Book renewals are currently disabled in library policy settings.";
            }
            else if (isOverdue)
            {
                // Business rule: Maximum 1 renewal per loan, only eligible if active and not already overdue.
                model.IsEligible = false;
                model.IneligibilityReason = $"This loan is overdue (Due date was {loan.DueAt:dd MMM yyyy}). Overdue books cannot be renewed; they must be returned and late fee cleared.";
            }
            else if (loan.RenewalCount >= setting.MaxRenewalsPerLoan)
            {
                // Business rule: Maximum 1 renewal per loan
                model.IsEligible = false;
                model.IneligibilityReason = $"Maximum renewal limit reached ({setting.MaxRenewalsPerLoan} renewal allowed).";
            }
            else
            {
                model.IsEligible = true;
            }

            return model;
        }

        public async Task<(bool Success, string Message, DateTime? NewDueAt)> RenewLoanAsync(
            int loanId,
            string userId,
            string userEmail)
        {
            var loan = await _context.Loans
                .Include(l => l.Student)
                .Include(l => l.BookCopy)
                    .ThenInclude(bc => bc.Book)
                .FirstOrDefaultAsync(l => l.LoanId == loanId);

            if (loan == null)
            {
                return (false, "Loan record not found.", null);
            }

            if (loan.Status != LoanStatus.Active)
            {
                return (false, "Only active loans can be renewed.", null);
            }

            var setting = await GetLibrarySettingAsync();
            var today = _dateTimeProvider.Now.Date;

            if (!setting.AllowRenewal)
            {
                return (false, "Renewals are disabled in library policy.", null);
            }

            if (loan.DueAt.Date < today)
            {
                return (false, "Overdue loans cannot be renewed. The book must be returned.", null);
            }

            if (loan.RenewalCount >= setting.MaxRenewalsPerLoan)
            {
                return (false, $"Maximum renewal limit of {setting.MaxRenewalsPerLoan} reached.", null);
            }

            loan.DueAt = loan.DueAt.AddDays(setting.LoanPeriodDays);
            loan.RenewalCount += 1;

            await _context.SaveChangesAsync();

            var localDueAt = _dateTimeProvider.ToLocalDateTime(loan.DueAt);

            await _auditService.LogAsync(
                "Renew",
                nameof(Loan),
                loan.LoanId.ToString(),
                userId,
                userEmail,
                $"Renewed loan #{loan.LoanId} for copy {loan.BookCopy.AccessionNumber}. Renewal count: {loan.RenewalCount}. New due date: {localDueAt:dd MMM yyyy}.");

            return (true, $"Loan successfully renewed! New return due date is {localDueAt:dd MMM yyyy}.", localDueAt);
        }

        public async Task<PaginatedList<ActiveLoanListItemViewModel>> GetActiveLoansAsync(ActiveLoanListFilterViewModel filter)
        {
            var query = _context.Loans
                .AsNoTracking()
                .Include(l => l.Student)
                    .ThenInclude(s => s.Department)
                .Include(l => l.BookCopy)
                    .ThenInclude(bc => bc.Book)
                .Include(l => l.IssuedByUser)
                .Where(l => l.Status == LoanStatus.Active);

            if (!string.IsNullOrWhiteSpace(filter.SearchTerm))
            {
                var term = filter.SearchTerm.Trim().ToLower();
                query = query.Where(l =>
                    l.Student.EnrollmentNumber.ToLower().Contains(term) ||
                    l.Student.FullName.ToLower().Contains(term) ||
                    l.BookCopy.AccessionNumber.ToLower().Contains(term) ||
                    l.BookCopy.Book.Title.ToLower().Contains(term));
            }

            if (filter.DepartmentId.HasValue && filter.DepartmentId.Value > 0)
            {
                query = query.Where(l => l.Student.DepartmentId == filter.DepartmentId.Value);
            }

            var setting = await GetLibrarySettingAsync();
            var today = _dateTimeProvider.Now.Date;

            if (filter.OverdueOnly)
            {
                query = query.Where(l => l.DueAt.Date < today);
            }

            query = query.OrderBy(l => l.DueAt);

            var totalCount = await query.CountAsync();
            var items = await query
                .Skip((filter.PageNumber - 1) * filter.PageSize)
                .Take(filter.PageSize)
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
                    EstimatedFine = l.DueAt.Date < today ? (today - l.DueAt.Date).Days * setting.DailyFineRate : 0m,
                    IssuedBy = l.IssuedByUser.FullName ?? l.IssuedByUser.UserName ?? "Staff"
                })
                .ToListAsync();

            // Convert to local time
            foreach (var item in items)
            {
                item.IssuedAt = _dateTimeProvider.ToLocalDateTime(item.IssuedAt);
                item.DueAt = _dateTimeProvider.ToLocalDateTime(item.DueAt);
            }

            return new PaginatedList<ActiveLoanListItemViewModel>(items, totalCount, filter.PageNumber, filter.PageSize);
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
