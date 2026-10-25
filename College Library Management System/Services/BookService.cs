using College_Library_Management_System.Data;
using College_Library_Management_System.Models;
using College_Library_Management_System.Models.Enums;
using College_Library_Management_System.Services.Common;
using College_Library_Management_System.Services.Interfaces;
using College_Library_Management_System.ViewModels.Books;
using College_Library_Management_System.ViewModels.Common;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;

namespace College_Library_Management_System.Services.Interfaces
{
    public interface IBookService
    {
        Task<PaginatedList<BookListItemViewModel>> GetBooksAsync(BookListFilterViewModel filter);
        Task<BookFormViewModel?> GetBookForEditAsync(int bookId);
        Task<BookDetailsViewModel?> GetBookDetailsAsync(int bookId);
        Task<bool> IsIsbnUniqueAsync(string isbn, int? excludeBookId = null);
        Task<BookFormViewModel> PopulateFormOptionsAsync(BookFormViewModel? model = null);
        Task<(bool Success, string Message, int? BookId)> CreateBookAsync(BookFormViewModel model, string? userId, string? userEmail);
        Task<(bool Success, string Message)> UpdateBookAsync(BookFormViewModel model, string? userId, string? userEmail);
        Task<(bool Success, string Message)> DeleteBookAsync(int bookId, string? userId, string? userEmail);
    }
}

namespace College_Library_Management_System.Services
{
    public class BookService : IBookService
    {
        private readonly ApplicationDbContext _context;
        private readonly IDateTimeProvider _dateTimeProvider;
        private readonly IAuditLogService _auditLogService;

        public BookService(ApplicationDbContext context, IDateTimeProvider dateTimeProvider, IAuditLogService auditLogService)
        {
            _context = context;
            _dateTimeProvider = dateTimeProvider;
            _auditLogService = auditLogService;
        }

        public async Task<PaginatedList<BookListItemViewModel>> GetBooksAsync(BookListFilterViewModel filter)
        {
            var query = _context.Books
                .Include(b => b.Category)
                .Include(b => b.Publisher)
                .Include(b => b.BookAuthors)
                    .ThenInclude(ba => ba.Author)
                .Include(b => b.Copies)
                .AsNoTracking()
                .AsQueryable();

            if (!string.IsNullOrWhiteSpace(filter.SearchTerm))
            {
                var term = filter.SearchTerm.Trim();
                query = query.Where(b =>
                    b.Title.Contains(term) ||
                    (b.ISBN != null && b.ISBN.Contains(term)) ||
                    b.Category.Name.Contains(term) ||
                    b.Publisher.Name.Contains(term) ||
                    b.BookAuthors.Any(ba => ba.Author.Name.Contains(term)) ||
                    b.Copies.Any(c => c.AccessionNumber.Contains(term)));
            }

            if (filter.CategoryId.HasValue && filter.CategoryId > 0)
            {
                query = query.Where(b => b.CategoryId == filter.CategoryId.Value);
            }

            if (filter.PublisherId.HasValue && filter.PublisherId > 0)
            {
                query = query.Where(b => b.PublisherId == filter.PublisherId.Value);
            }

            var totalCount = await query.CountAsync();
            var pageNumber = Math.Max(1, filter.PageNumber);
            var pageSize = filter.PageSize > 0 ? filter.PageSize : 10;

            var items = await query
                .OrderBy(b => b.Title)
                .Skip((pageNumber - 1) * pageSize)
                .Take(pageSize)
                .Select(b => new BookListItemViewModel
                {
                    BookId = b.BookId,
                    Title = b.Title,
                    ISBN = b.ISBN,
                    CategoryName = b.Category.Name,
                    PublisherName = b.Publisher.Name,
                    PublicationYear = b.PublicationYear,
                    AuthorsString = string.Join(", ", b.BookAuthors.Select(ba => ba.Author.Name)),
                    TotalCopiesCount = b.Copies.Count,
                    AvailableCopiesCount = b.Copies.Count(c => c.Status == BookCopyStatus.Available),
                    IssuedCopiesCount = b.Copies.Count(c => c.Status == BookCopyStatus.Issued)
                })
                .ToListAsync();

            return new PaginatedList<BookListItemViewModel>(items, totalCount, pageNumber, pageSize);
        }

        public async Task<BookFormViewModel?> GetBookForEditAsync(int bookId)
        {
            var book = await _context.Books
                .Include(b => b.BookAuthors)
                .FirstOrDefaultAsync(b => b.BookId == bookId);

            if (book == null) return null;

            var model = new BookFormViewModel
            {
                BookId = book.BookId,
                Title = book.Title,
                ISBN = book.ISBN,
                PublicationYear = book.PublicationYear,
                Description = book.Description,
                CategoryId = book.CategoryId,
                PublisherId = book.PublisherId,
                SelectedAuthorIds = book.BookAuthors.Select(ba => ba.AuthorId).ToList()
            };

            return await PopulateFormOptionsAsync(model);
        }

        public async Task<BookDetailsViewModel?> GetBookDetailsAsync(int bookId)
        {
            var book = await _context.Books
                .Include(b => b.Category)
                .Include(b => b.Publisher)
                .Include(b => b.BookAuthors)
                    .ThenInclude(ba => ba.Author)
                .Include(b => b.Copies)
                    .ThenInclude(c => c.Loans.Where(l => l.Status == LoanStatus.Active))
                        .ThenInclude(l => l.Student)
                .AsNoTracking()
                .FirstOrDefaultAsync(b => b.BookId == bookId);

            if (book == null) return null;

            var copies = book.Copies
                .OrderBy(c => c.AccessionNumber)
                .Select(c =>
                {
                    var activeLoan = c.Loans.FirstOrDefault();
                    return new BookCopyItemViewModel
                    {
                        BookCopyId = c.BookCopyId,
                        AccessionNumber = c.AccessionNumber,
                        ShelfLocation = c.ShelfLocation,
                        Status = c.Status,
                        ConditionNotes = c.ConditionNotes,
                        CurrentBorrowerName = activeLoan?.Student.FullName,
                        DueDate = activeLoan != null ? _dateTimeProvider.ToLocalDateTime(activeLoan.DueAt) : null
                    };
                })
                .ToList();

            return new BookDetailsViewModel
            {
                BookId = book.BookId,
                Title = book.Title,
                ISBN = book.ISBN,
                PublicationYear = book.PublicationYear,
                Description = book.Description,
                CategoryName = book.Category.Name,
                PublisherName = book.Publisher.Name,
                CreatedAt = _dateTimeProvider.ToLocalDateTime(book.CreatedAt),
                Authors = book.BookAuthors.Select(ba => ba.Author.Name).ToList(),
                Copies = copies
            };
        }

        public async Task<bool> IsIsbnUniqueAsync(string isbn, int? excludeBookId = null)
        {
            if (string.IsNullOrWhiteSpace(isbn)) return true;

            var normalized = isbn.Trim();
            var query = _context.Books.Where(b => b.ISBN != null && b.ISBN == normalized);
            if (excludeBookId.HasValue)
            {
                query = query.Where(b => b.BookId != excludeBookId.Value);
            }
            return !await query.AnyAsync();
        }

        public async Task<BookFormViewModel> PopulateFormOptionsAsync(BookFormViewModel? model = null)
        {
            model ??= new BookFormViewModel();

            model.CategoryOptions = await _context.Categories
                .OrderBy(c => c.Name)
                .Select(c => new SelectListItem { Value = c.CategoryId.ToString(), Text = c.Name })
                .ToListAsync();

            model.PublisherOptions = await _context.Publishers
                .OrderBy(p => p.Name)
                .Select(p => new SelectListItem { Value = p.PublisherId.ToString(), Text = p.Name })
                .ToListAsync();

            model.AuthorOptions = await _context.Authors
                .OrderBy(a => a.Name)
                .Select(a => new SelectListItem { Value = a.AuthorId.ToString(), Text = a.Name })
                .ToListAsync();

            return model;
        }

        public async Task<(bool Success, string Message, int? BookId)> CreateBookAsync(BookFormViewModel model, string? userId, string? userEmail)
        {
            if (model.SelectedAuthorIds == null || !model.SelectedAuthorIds.Any())
            {
                return (false, "Please select at least one author.", null);
            }

            var trimmedTitle = model.Title.Trim();
            var trimmedIsbn = string.IsNullOrWhiteSpace(model.ISBN) ? null : model.ISBN.Trim();

            if (trimmedIsbn != null && !await IsIsbnUniqueAsync(trimmedIsbn))
            {
                return (false, $"ISBN '{trimmedIsbn}' is already registered to another book.", null);
            }

            var categoryExists = await _context.Categories.AnyAsync(c => c.CategoryId == model.CategoryId);
            if (!categoryExists) return (false, "Selected category does not exist.", null);

            var publisherExists = await _context.Publishers.AnyAsync(p => p.PublisherId == model.PublisherId);
            if (!publisherExists) return (false, "Selected publisher does not exist.", null);

            var book = new Book
            {
                Title = trimmedTitle,
                ISBN = trimmedIsbn,
                PublicationYear = model.PublicationYear,
                Description = string.IsNullOrWhiteSpace(model.Description) ? null : model.Description.Trim(),
                CategoryId = model.CategoryId,
                PublisherId = model.PublisherId,
                CreatedAt = _dateTimeProvider.UtcNow
            };

            foreach (var authorId in model.SelectedAuthorIds.Distinct())
            {
                book.BookAuthors.Add(new BookAuthor { AuthorId = authorId });
            }

            _context.Books.Add(book);
            await _context.SaveChangesAsync();

            await _auditLogService.LogAsync(
                action: "CreateBook",
                entityType: nameof(Book),
                entityId: book.BookId.ToString(),
                details: $"Created book catalog entry '{book.Title}' (ISBN: {book.ISBN ?? "N/A"}).",
                userId: userId,
                userEmail: userEmail
            );

            return (true, "Book added to catalog successfully.", book.BookId);
        }

        public async Task<(bool Success, string Message)> UpdateBookAsync(BookFormViewModel model, string? userId, string? userEmail)
        {
            var book = await _context.Books
                .Include(b => b.BookAuthors)
                .FirstOrDefaultAsync(b => b.BookId == model.BookId);

            if (book == null) return (false, "Book not found.");

            if (model.SelectedAuthorIds == null || !model.SelectedAuthorIds.Any())
            {
                return (false, "Please select at least one author.");
            }

            var trimmedIsbn = string.IsNullOrWhiteSpace(model.ISBN) ? null : model.ISBN.Trim();
            if (trimmedIsbn != null && !await IsIsbnUniqueAsync(trimmedIsbn, model.BookId))
            {
                return (false, $"ISBN '{trimmedIsbn}' is already registered to another book.");
            }

            book.Title = model.Title.Trim();
            book.ISBN = trimmedIsbn;
            book.PublicationYear = model.PublicationYear;
            book.Description = string.IsNullOrWhiteSpace(model.Description) ? null : model.Description.Trim();
            book.CategoryId = model.CategoryId;
            book.PublisherId = model.PublisherId;

            // Synchronize authors
            var currentAuthorIds = book.BookAuthors.Select(ba => ba.AuthorId).ToHashSet();
            var newAuthorIds = model.SelectedAuthorIds.Distinct().ToHashSet();

            // Remove unselected authors
            var authorsToRemove = book.BookAuthors.Where(ba => !newAuthorIds.Contains(ba.AuthorId)).ToList();
            foreach (var removeBa in authorsToRemove)
            {
                book.BookAuthors.Remove(removeBa);
            }

            // Add newly selected authors
            foreach (var newAuthorId in newAuthorIds.Where(id => !currentAuthorIds.Contains(id)))
            {
                book.BookAuthors.Add(new BookAuthor { BookId = book.BookId, AuthorId = newAuthorId });
            }

            await _context.SaveChangesAsync();

            await _auditLogService.LogAsync(
                action: "UpdateBook",
                entityType: nameof(Book),
                entityId: book.BookId.ToString(),
                details: $"Updated book catalog entry '{book.Title}'.",
                userId: userId,
                userEmail: userEmail
            );

            return (true, "Book updated successfully.");
        }

        public async Task<(bool Success, string Message)> DeleteBookAsync(int bookId, string? userId, string? userEmail)
        {
            var book = await _context.Books
                .Include(b => b.Copies)
                .FirstOrDefaultAsync(b => b.BookId == bookId);

            if (book == null) return (false, "Book not found.");

            if (book.Copies.Any())
            {
                return (false, $"Cannot delete book '{book.Title}' because {book.Copies.Count} physical copy/copies exist in inventory.");
            }

            _context.Books.Remove(book);
            await _context.SaveChangesAsync();

            await _auditLogService.LogAsync(
                action: "DeleteBook",
                entityType: nameof(Book),
                entityId: bookId.ToString(),
                details: $"Deleted book '{book.Title}'.",
                userId: userId,
                userEmail: userEmail
            );

            return (true, "Book deleted successfully.");
        }
    }

    internal static class LoanExtensions
    {
        public static bool HasValue(this Loan? loan) => loan != null;
    }
}
