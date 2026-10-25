using College_Library_Management_System.Data;
using College_Library_Management_System.Models;
using College_Library_Management_System.Models.Enums;
using College_Library_Management_System.Services.Common;
using College_Library_Management_System.Services.Interfaces;
using College_Library_Management_System.ViewModels.Common;
using College_Library_Management_System.ViewModels.Inventory;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;

namespace College_Library_Management_System.Services.Interfaces
{
    public interface IBookCopyService
    {
        Task<PaginatedList<BookCopyListItemViewModel>> GetCopiesAsync(BookCopyListFilterViewModel filter);
        Task<BookCopy?> GetCopyByIdAsync(int bookCopyId);
        Task<BookCopyFormViewModel?> GetCopyForEditAsync(int bookCopyId);
        Task<BookCopy?> GetCopyByAccessionNumberAsync(string accessionNumber);
        Task<BookCopyHistoryViewModel?> GetCopyHistoryAsync(int bookCopyId);
        Task<bool> IsAccessionNumberUniqueAsync(string accessionNumber, int? excludeId = null);
        Task<List<SelectListItem>> GetBookOptionsAsync();
        Task<(bool Success, string Message, int? BookCopyId)> CreateCopyAsync(BookCopyFormViewModel model, string? userId, string? userEmail);
        Task<(bool Success, string Message, int CreatedCount)> CreateBatchCopiesAsync(BatchCopyFormViewModel model, string? userId, string? userEmail);
        Task<(bool Success, string Message)> UpdateCopyAsync(BookCopyFormViewModel model, string? userId, string? userEmail);
        Task<(bool Success, string Message)> UpdateCopyStatusAsync(int bookCopyId, BookCopyStatus newStatus, string? conditionNotes, string? userId, string? userEmail);
    }
}

namespace College_Library_Management_System.Services
{
    public class BookCopyService : IBookCopyService
    {
        private readonly ApplicationDbContext _context;
        private readonly IDateTimeProvider _dateTimeProvider;
        private readonly IAuditLogService _auditLogService;

        public BookCopyService(ApplicationDbContext context, IDateTimeProvider dateTimeProvider, IAuditLogService auditLogService)
        {
            _context = context;
            _dateTimeProvider = dateTimeProvider;
            _auditLogService = auditLogService;
        }

        public async Task<PaginatedList<BookCopyListItemViewModel>> GetCopiesAsync(BookCopyListFilterViewModel filter)
        {
            var query = _context.BookCopies
                .Include(bc => bc.Book)
                .Include(bc => bc.Loans.Where(l => l.Status == LoanStatus.Active))
                    .ThenInclude(l => l.Student)
                .AsNoTracking()
                .AsQueryable();

            if (!string.IsNullOrWhiteSpace(filter.SearchTerm))
            {
                var term = filter.SearchTerm.Trim();
                query = query.Where(bc =>
                    bc.AccessionNumber.Contains(term) ||
                    bc.Book.Title.Contains(term) ||
                    (bc.Book.ISBN != null && bc.Book.ISBN.Contains(term)) ||
                    bc.ShelfLocation.Contains(term));
            }

            if (filter.BookId.HasValue && filter.BookId.Value > 0)
            {
                query = query.Where(bc => bc.BookId == filter.BookId.Value);
            }

            if (filter.Status.HasValue)
            {
                query = query.Where(bc => bc.Status == filter.Status.Value);
            }

            if (!string.IsNullOrWhiteSpace(filter.ShelfLocation))
            {
                var shelf = filter.ShelfLocation.Trim();
                query = query.Where(bc => bc.ShelfLocation.Contains(shelf));
            }

            var totalCount = await query.CountAsync();
            var pageNumber = Math.Max(1, filter.PageNumber);
            var pageSize = filter.PageSize > 0 ? filter.PageSize : 15;

            var items = await query
                .OrderBy(bc => bc.AccessionNumber)
                .Skip((pageNumber - 1) * pageSize)
                .Take(pageSize)
                .Select(bc => new BookCopyListItemViewModel
                {
                    BookCopyId = bc.BookCopyId,
                    BookId = bc.BookId,
                    BookTitle = bc.Book.Title,
                    ISBN = bc.Book.ISBN,
                    AccessionNumber = bc.AccessionNumber,
                    ShelfLocation = bc.ShelfLocation,
                    Status = bc.Status,
                    ConditionNotes = bc.ConditionNotes,
                    HasActiveLoan = bc.Loans.Any(l => l.Status == LoanStatus.Active),
                    CurrentBorrowerName = bc.Loans.Where(l => l.Status == LoanStatus.Active).Select(l => l.Student.FullName).FirstOrDefault(),
                    DueDate = bc.Loans.Where(l => l.Status == LoanStatus.Active).Select(l => (DateTime?)l.DueAt).FirstOrDefault()
                })
                .ToListAsync();

            // Format dates
            foreach (var item in items)
            {
                if (item.DueDate.HasValue)
                {
                    item.DueDate = _dateTimeProvider.ToLocalDateTime(item.DueDate.Value);
                }
            }

            return new PaginatedList<BookCopyListItemViewModel>(items, totalCount, pageNumber, pageSize);
        }

        public async Task<BookCopy?> GetCopyByIdAsync(int bookCopyId)
        {
            return await _context.BookCopies
                .Include(bc => bc.Book)
                .Include(bc => bc.Loans.Where(l => l.Status == LoanStatus.Active))
                    .ThenInclude(l => l.Student)
                .FirstOrDefaultAsync(bc => bc.BookCopyId == bookCopyId);
        }

        public async Task<BookCopyFormViewModel?> GetCopyForEditAsync(int bookCopyId)
        {
            var copy = await _context.BookCopies
                .Include(bc => bc.Book)
                .FirstOrDefaultAsync(bc => bc.BookCopyId == bookCopyId);

            if (copy == null) return null;

            return new BookCopyFormViewModel
            {
                BookCopyId = copy.BookCopyId,
                BookId = copy.BookId,
                BookTitle = copy.Book.Title,
                AccessionNumber = copy.AccessionNumber,
                ShelfLocation = copy.ShelfLocation,
                Status = copy.Status,
                ConditionNotes = copy.ConditionNotes,
                BookOptions = await GetBookOptionsAsync()
            };
        }

        public async Task<BookCopy?> GetCopyByAccessionNumberAsync(string accessionNumber)
        {
            var normalized = accessionNumber.Trim().ToUpperInvariant();
            return await _context.BookCopies
                .Include(bc => bc.Book)
                .FirstOrDefaultAsync(bc => bc.AccessionNumber.ToUpper() == normalized);
        }

        public async Task<BookCopyHistoryViewModel?> GetCopyHistoryAsync(int bookCopyId)
        {
            var copy = await _context.BookCopies
                .Include(bc => bc.Book)
                .Include(bc => bc.Loans)
                    .ThenInclude(l => l.Student)
                        .ThenInclude(s => s.Department)
                .Include(bc => bc.Loans)
                    .ThenInclude(l => l.IssuedByUser)
                .Include(bc => bc.Loans)
                    .ThenInclude(l => l.ReceivedByUser)
                .Include(bc => bc.Loans)
                    .ThenInclude(l => l.Fine)
                .AsNoTracking()
                .FirstOrDefaultAsync(bc => bc.BookCopyId == bookCopyId);

            if (copy == null) return null;

            var history = copy.Loans
                .OrderByDescending(l => l.IssuedAt)
                .Select(l => new CopyLoanHistoryItemViewModel
                {
                    LoanId = l.LoanId,
                    StudentEnrollment = l.Student.EnrollmentNumber,
                    StudentName = l.Student.FullName,
                    DepartmentCode = l.Student.Department.DepartmentCode,
                    IssuedAt = _dateTimeProvider.ToLocalDateTime(l.IssuedAt),
                    DueAt = _dateTimeProvider.ToLocalDateTime(l.DueAt),
                    ReturnedAt = l.ReturnedAt.HasValue ? _dateTimeProvider.ToLocalDateTime(l.ReturnedAt.Value) : null,
                    LoanStatus = l.Status,
                    RenewalCount = l.RenewalCount,
                    IssuedBy = l.IssuedByUser.FullName,
                    ReceivedBy = l.ReceivedByUser?.FullName,
                    FineAmount = l.Fine?.FineAmount,
                    FineStatus = l.Fine?.Status
                })
                .ToList();

            return new BookCopyHistoryViewModel
            {
                BookCopyId = copy.BookCopyId,
                BookId = copy.BookId,
                BookTitle = copy.Book.Title,
                ISBN = copy.Book.ISBN,
                AccessionNumber = copy.AccessionNumber,
                ShelfLocation = copy.ShelfLocation,
                Status = copy.Status,
                ConditionNotes = copy.ConditionNotes,
                LoanHistory = history
            };
        }

        public async Task<bool> IsAccessionNumberUniqueAsync(string accessionNumber, int? excludeId = null)
        {
            var normalized = accessionNumber.Trim().ToUpperInvariant();
            var query = _context.BookCopies.Where(bc => bc.AccessionNumber.ToUpper() == normalized);
            if (excludeId.HasValue)
            {
                query = query.Where(bc => bc.BookCopyId != excludeId.Value);
            }
            return !await query.AnyAsync();
        }

        public async Task<List<SelectListItem>> GetBookOptionsAsync()
        {
            return await _context.Books
                .OrderBy(b => b.Title)
                .Select(b => new SelectListItem
                {
                    Value = b.BookId.ToString(),
                    Text = b.ISBN != null ? $"{b.Title} (ISBN: {b.ISBN})" : b.Title
                })
                .ToListAsync();
        }

        public async Task<(bool Success, string Message, int? BookCopyId)> CreateCopyAsync(BookCopyFormViewModel model, string? userId, string? userEmail)
        {
            var book = await _context.Books.FindAsync(model.BookId);
            if (book == null) return (false, "Specified book catalog entry not found.", null);

            var trimmedAcc = model.AccessionNumber.Trim().ToUpperInvariant();
            if (!await IsAccessionNumberUniqueAsync(trimmedAcc))
            {
                return (false, $"Accession Number '{trimmedAcc}' is already assigned to another copy.", null);
            }

            var copy = new BookCopy
            {
                BookId = model.BookId,
                AccessionNumber = trimmedAcc,
                ShelfLocation = model.ShelfLocation.Trim(),
                Status = model.Status,
                ConditionNotes = string.IsNullOrWhiteSpace(model.ConditionNotes) ? null : model.ConditionNotes.Trim()
            };

            _context.BookCopies.Add(copy);
            await _context.SaveChangesAsync();

            await _auditLogService.LogAsync(
                action: "CreateBookCopy",
                entityType: nameof(BookCopy),
                entityId: copy.BookCopyId.ToString(),
                details: $"Added physical copy '{copy.AccessionNumber}' for book '{book.Title}' at '{copy.ShelfLocation}'.",
                userId: userId,
                userEmail: userEmail
            );

            return (true, $"Copy with Accession Number '{copy.AccessionNumber}' registered successfully.", copy.BookCopyId);
        }

        public async Task<(bool Success, string Message, int CreatedCount)> CreateBatchCopiesAsync(BatchCopyFormViewModel model, string? userId, string? userEmail)
        {
            var book = await _context.Books.FindAsync(model.BookId);
            if (book == null) return (false, "Specified book catalog entry not found.", 0);

            if (model.NumberOfCopies < 1 || model.NumberOfCopies > 100)
            {
                return (false, "You can generate between 1 and 100 copies in one batch.", 0);
            }

            var prefix = model.Prefix.Trim().ToUpperInvariant();
            var shelfLocation = model.ShelfLocation.Trim();
            var conditionNotes = string.IsNullOrWhiteSpace(model.ConditionNotes) ? null : model.ConditionNotes.Trim();

            // Pre-calculate candidate accession numbers
            var candidates = new List<string>();
            for (int i = 0; i < model.NumberOfCopies; i++)
            {
                var num = model.StartingNumber + i;
                candidates.Add($"{prefix}{num:D4}");
            }

            // Check for collision with existing records
            var existingAccNumbers = await _context.BookCopies
                .Where(bc => candidates.Contains(bc.AccessionNumber.ToUpper()))
                .Select(bc => bc.AccessionNumber)
                .ToListAsync();

            if (existingAccNumbers.Any())
            {
                return (false, $"Batch generation aborted: Accession number(s) '{string.Join(", ", existingAccNumbers.Take(3))}' already exist in database.", 0);
            }

            var newCopies = candidates.Select(acc => new BookCopy
            {
                BookId = model.BookId,
                AccessionNumber = acc,
                ShelfLocation = shelfLocation,
                Status = BookCopyStatus.Available,
                ConditionNotes = conditionNotes
            }).ToList();

            _context.BookCopies.AddRange(newCopies);
            await _context.SaveChangesAsync();

            await _auditLogService.LogAsync(
                action: "BatchCreateBookCopies",
                entityType: nameof(BookCopy),
                entityId: book.BookId.ToString(),
                details: $"Added batch of {newCopies.Count} copies ({candidates.First()} to {candidates.Last()}) for book '{book.Title}'.",
                userId: userId,
                userEmail: userEmail
            );

            return (true, $"Successfully registered {newCopies.Count} copies ({candidates.First()} - {candidates.Last()}).", newCopies.Count);
        }

        public async Task<(bool Success, string Message)> UpdateCopyAsync(BookCopyFormViewModel model, string? userId, string? userEmail)
        {
            var copy = await _context.BookCopies
                .Include(bc => bc.Loans.Where(l => l.Status == LoanStatus.Active))
                .FirstOrDefaultAsync(bc => bc.BookCopyId == model.BookCopyId);

            if (copy == null) return (false, "Book copy not found.");

            var trimmedAcc = model.AccessionNumber.Trim().ToUpperInvariant();
            if (!await IsAccessionNumberUniqueAsync(trimmedAcc, model.BookCopyId))
            {
                return (false, $"Accession Number '{trimmedAcc}' is already assigned to another copy.");
            }

            // If the copy is currently loaned, its status cannot be changed to Available or Damaged directly without returning
            var hasActiveLoan = copy.Loans.Any(l => l.Status == LoanStatus.Active);
            if (hasActiveLoan && model.Status != BookCopyStatus.Issued && model.Status != BookCopyStatus.Lost)
            {
                return (false, "Cannot change status of this copy because it is currently on an active loan. Please process the book return first.");
            }

            copy.AccessionNumber = trimmedAcc;
            copy.ShelfLocation = model.ShelfLocation.Trim();
            copy.Status = model.Status;
            copy.ConditionNotes = string.IsNullOrWhiteSpace(model.ConditionNotes) ? null : model.ConditionNotes.Trim();

            await _context.SaveChangesAsync();

            await _auditLogService.LogAsync(
                action: "UpdateBookCopy",
                entityType: nameof(BookCopy),
                entityId: copy.BookCopyId.ToString(),
                details: $"Updated copy '{copy.AccessionNumber}' (Status: {copy.Status}, Shelf: '{copy.ShelfLocation}').",
                userId: userId,
                userEmail: userEmail
            );

            return (true, "Book copy updated successfully.");
        }

        public async Task<(bool Success, string Message)> UpdateCopyStatusAsync(int bookCopyId, BookCopyStatus newStatus, string? conditionNotes, string? userId, string? userEmail)
        {
            var copy = await _context.BookCopies
                .Include(bc => bc.Loans.Where(l => l.Status == LoanStatus.Active))
                .FirstOrDefaultAsync(bc => bc.BookCopyId == bookCopyId);

            if (copy == null) return (false, "Book copy not found.");

            var hasActiveLoan = copy.Loans.Any(l => l.Status == LoanStatus.Active);
            if (hasActiveLoan && newStatus != BookCopyStatus.Lost && newStatus != BookCopyStatus.Issued)
            {
                return (false, "Cannot change status because this copy is currently issued on loan. Use Return Management instead.");
            }

            var oldStatus = copy.Status;
            copy.Status = newStatus;
            if (!string.IsNullOrWhiteSpace(conditionNotes))
            {
                copy.ConditionNotes = conditionNotes.Trim();
            }

            await _context.SaveChangesAsync();

            await _auditLogService.LogAsync(
                action: "ChangeCopyStatus",
                entityType: nameof(BookCopy),
                entityId: copy.BookCopyId.ToString(),
                details: $"Changed status of copy '{copy.AccessionNumber}' from {oldStatus} to {newStatus}.",
                userId: userId,
                userEmail: userEmail
            );

            return (true, $"Status for copy '{copy.AccessionNumber}' changed to {newStatus}.");
        }
    }
}
