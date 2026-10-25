using College_Library_Management_System.Data;
using College_Library_Management_System.Models;
using College_Library_Management_System.Services.Interfaces;
using College_Library_Management_System.ViewModels.MasterData;
using Microsoft.EntityFrameworkCore;

namespace College_Library_Management_System.Services.Interfaces
{
    public interface IMasterDataService
    {
        // Categories
        Task<List<CategoryViewModel>> GetAllCategoriesAsync();
        Task<CategoryViewModel?> GetCategoryForEditAsync(int id);
        Task<(bool Success, string Message, int? Id)> CreateCategoryAsync(CategoryViewModel model, string? userId, string? userEmail);
        Task<(bool Success, string Message)> UpdateCategoryAsync(CategoryViewModel model, string? userId, string? userEmail);
        Task<(bool Success, string Message)> DeleteCategoryAsync(int id, string? userId, string? userEmail);

        // Publishers
        Task<List<PublisherViewModel>> GetAllPublishersAsync();
        Task<PublisherViewModel?> GetPublisherForEditAsync(int id);
        Task<(bool Success, string Message, int? Id)> CreatePublisherAsync(PublisherViewModel model, string? userId, string? userEmail);
        Task<(bool Success, string Message)> UpdatePublisherAsync(PublisherViewModel model, string? userId, string? userEmail);
        Task<(bool Success, string Message)> DeletePublisherAsync(int id, string? userId, string? userEmail);

        // Authors
        Task<List<AuthorViewModel>> GetAllAuthorsAsync();
        Task<AuthorViewModel?> GetAuthorForEditAsync(int id);
        Task<(bool Success, string Message, int? Id)> CreateAuthorAsync(AuthorViewModel model, string? userId, string? userEmail);
        Task<(bool Success, string Message)> UpdateAuthorAsync(AuthorViewModel model, string? userId, string? userEmail);
        Task<(bool Success, string Message)> DeleteAuthorAsync(int id, string? userId, string? userEmail);
    }
}

namespace College_Library_Management_System.Services
{
    public class MasterDataService : IMasterDataService
    {
        private readonly ApplicationDbContext _context;
        private readonly IAuditLogService _auditLogService;

        public MasterDataService(ApplicationDbContext context, IAuditLogService auditLogService)
        {
            _context = context;
            _auditLogService = auditLogService;
        }

        #region Category Management
        public async Task<List<CategoryViewModel>> GetAllCategoriesAsync()
        {
            return await _context.Categories
                .Select(c => new CategoryViewModel
                {
                    CategoryId = c.CategoryId,
                    Name = c.Name,
                    Description = c.Description,
                    BookCount = c.Books.Count
                })
                .OrderBy(c => c.Name)
                .ToListAsync();
        }

        public async Task<CategoryViewModel?> GetCategoryForEditAsync(int id)
        {
            var cat = await _context.Categories.FindAsync(id);
            if (cat == null) return null;

            return new CategoryViewModel
            {
                CategoryId = cat.CategoryId,
                Name = cat.Name,
                Description = cat.Description
            };
        }

        public async Task<(bool Success, string Message, int? Id)> CreateCategoryAsync(CategoryViewModel model, string? userId, string? userEmail)
        {
            var trimmedName = model.Name.Trim();
            var exists = await _context.Categories.AnyAsync(c => c.Name.ToLower() == trimmedName.ToLower());
            if (exists)
            {
                return (false, $"Category '{trimmedName}' already exists.", null);
            }

            var category = new Category
            {
                Name = trimmedName,
                Description = string.IsNullOrWhiteSpace(model.Description) ? null : model.Description.Trim()
            };

            _context.Categories.Add(category);
            await _context.SaveChangesAsync();

            await _auditLogService.LogAsync("CreateCategory", nameof(Category), category.CategoryId.ToString(), $"Created category '{category.Name}'.", userId, userEmail);
            return (true, "Category created successfully.", category.CategoryId);
        }

        public async Task<(bool Success, string Message)> UpdateCategoryAsync(CategoryViewModel model, string? userId, string? userEmail)
        {
            var category = await _context.Categories.FindAsync(model.CategoryId);
            if (category == null) return (false, "Category not found.");

            var trimmedName = model.Name.Trim();
            var exists = await _context.Categories.AnyAsync(c => c.CategoryId != model.CategoryId && c.Name.ToLower() == trimmedName.ToLower());
            if (exists)
            {
                return (false, $"Another category named '{trimmedName}' already exists.");
            }

            category.Name = trimmedName;
            category.Description = string.IsNullOrWhiteSpace(model.Description) ? null : model.Description.Trim();

            await _context.SaveChangesAsync();
            await _auditLogService.LogAsync("UpdateCategory", nameof(Category), category.CategoryId.ToString(), $"Updated category '{category.Name}'.", userId, userEmail);

            return (true, "Category updated successfully.");
        }

        public async Task<(bool Success, string Message)> DeleteCategoryAsync(int id, string? userId, string? userEmail)
        {
            var category = await _context.Categories.Include(c => c.Books).FirstOrDefaultAsync(c => c.CategoryId == id);
            if (category == null) return (false, "Category not found.");

            if (category.Books.Any())
            {
                return (false, $"Cannot delete category '{category.Name}' because {category.Books.Count} book(s) are assigned to it.");
            }

            _context.Categories.Remove(category);
            await _context.SaveChangesAsync();
            await _auditLogService.LogAsync("DeleteCategory", nameof(Category), id.ToString(), $"Deleted category '{category.Name}'.", userId, userEmail);

            return (true, "Category deleted successfully.");
        }
        #endregion

        #region Publisher Management
        public async Task<List<PublisherViewModel>> GetAllPublishersAsync()
        {
            return await _context.Publishers
                .Select(p => new PublisherViewModel
                {
                    PublisherId = p.PublisherId,
                    Name = p.Name,
                    Address = p.Address,
                    ContactEmail = p.ContactEmail,
                    BookCount = p.Books.Count
                })
                .OrderBy(p => p.Name)
                .ToListAsync();
        }

        public async Task<PublisherViewModel?> GetPublisherForEditAsync(int id)
        {
            var pub = await _context.Publishers.FindAsync(id);
            if (pub == null) return null;

            return new PublisherViewModel
            {
                PublisherId = pub.PublisherId,
                Name = pub.Name,
                Address = pub.Address,
                ContactEmail = pub.ContactEmail
            };
        }

        public async Task<(bool Success, string Message, int? Id)> CreatePublisherAsync(PublisherViewModel model, string? userId, string? userEmail)
        {
            var trimmedName = model.Name.Trim();
            var exists = await _context.Publishers.AnyAsync(p => p.Name.ToLower() == trimmedName.ToLower());
            if (exists)
            {
                return (false, $"Publisher '{trimmedName}' already exists.", null);
            }

            var publisher = new Publisher
            {
                Name = trimmedName,
                Address = string.IsNullOrWhiteSpace(model.Address) ? null : model.Address.Trim(),
                ContactEmail = string.IsNullOrWhiteSpace(model.ContactEmail) ? null : model.ContactEmail.Trim().ToLowerInvariant()
            };

            _context.Publishers.Add(publisher);
            await _context.SaveChangesAsync();

            await _auditLogService.LogAsync("CreatePublisher", nameof(Publisher), publisher.PublisherId.ToString(), $"Created publisher '{publisher.Name}'.", userId, userEmail);
            return (true, "Publisher created successfully.", publisher.PublisherId);
        }

        public async Task<(bool Success, string Message)> UpdatePublisherAsync(PublisherViewModel model, string? userId, string? userEmail)
        {
            var publisher = await _context.Publishers.FindAsync(model.PublisherId);
            if (publisher == null) return (false, "Publisher not found.");

            var trimmedName = model.Name.Trim();
            var exists = await _context.Publishers.AnyAsync(p => p.PublisherId != model.PublisherId && p.Name.ToLower() == trimmedName.ToLower());
            if (exists)
            {
                return (false, $"Another publisher named '{trimmedName}' already exists.");
            }

            publisher.Name = trimmedName;
            publisher.Address = string.IsNullOrWhiteSpace(model.Address) ? null : model.Address.Trim();
            publisher.ContactEmail = string.IsNullOrWhiteSpace(model.ContactEmail) ? null : model.ContactEmail.Trim().ToLowerInvariant();

            await _context.SaveChangesAsync();
            await _auditLogService.LogAsync("UpdatePublisher", nameof(Publisher), publisher.PublisherId.ToString(), $"Updated publisher '{publisher.Name}'.", userId, userEmail);

            return (true, "Publisher updated successfully.");
        }

        public async Task<(bool Success, string Message)> DeletePublisherAsync(int id, string? userId, string? userEmail)
        {
            var publisher = await _context.Publishers.Include(p => p.Books).FirstOrDefaultAsync(p => p.PublisherId == id);
            if (publisher == null) return (false, "Publisher not found.");

            if (publisher.Books.Any())
            {
                return (false, $"Cannot delete publisher '{publisher.Name}' because {publisher.Books.Count} book(s) are associated with it.");
            }

            _context.Publishers.Remove(publisher);
            await _context.SaveChangesAsync();
            await _auditLogService.LogAsync("DeletePublisher", nameof(Publisher), id.ToString(), $"Deleted publisher '{publisher.Name}'.", userId, userEmail);

            return (true, "Publisher deleted successfully.");
        }
        #endregion

        #region Author Management
        public async Task<List<AuthorViewModel>> GetAllAuthorsAsync()
        {
            return await _context.Authors
                .Select(a => new AuthorViewModel
                {
                    AuthorId = a.AuthorId,
                    Name = a.Name,
                    Biography = a.Biography,
                    BookCount = a.BookAuthors.Count
                })
                .OrderBy(a => a.Name)
                .ToListAsync();
        }

        public async Task<AuthorViewModel?> GetAuthorForEditAsync(int id)
        {
            var author = await _context.Authors.FindAsync(id);
            if (author == null) return null;

            return new AuthorViewModel
            {
                AuthorId = author.AuthorId,
                Name = author.Name,
                Biography = author.Biography
            };
        }

        public async Task<(bool Success, string Message, int? Id)> CreateAuthorAsync(AuthorViewModel model, string? userId, string? userEmail)
        {
            var trimmedName = model.Name.Trim();

            var author = new Author
            {
                Name = trimmedName,
                Biography = string.IsNullOrWhiteSpace(model.Biography) ? null : model.Biography.Trim()
            };

            _context.Authors.Add(author);
            await _context.SaveChangesAsync();

            await _auditLogService.LogAsync("CreateAuthor", nameof(Author), author.AuthorId.ToString(), $"Created author '{author.Name}'.", userId, userEmail);
            return (true, "Author created successfully.", author.AuthorId);
        }

        public async Task<(bool Success, string Message)> UpdateAuthorAsync(AuthorViewModel model, string? userId, string? userEmail)
        {
            var author = await _context.Authors.FindAsync(model.AuthorId);
            if (author == null) return (false, "Author not found.");

            author.Name = model.Name.Trim();
            author.Biography = string.IsNullOrWhiteSpace(model.Biography) ? null : model.Biography.Trim();

            await _context.SaveChangesAsync();
            await _auditLogService.LogAsync("UpdateAuthor", nameof(Author), author.AuthorId.ToString(), $"Updated author '{author.Name}'.", userId, userEmail);

            return (true, "Author updated successfully.");
        }

        public async Task<(bool Success, string Message)> DeleteAuthorAsync(int id, string? userId, string? userEmail)
        {
            var author = await _context.Authors.Include(a => a.BookAuthors).FirstOrDefaultAsync(a => a.AuthorId == id);
            if (author == null) return (false, "Author not found.");

            if (author.BookAuthors.Any())
            {
                return (false, $"Cannot delete author '{author.Name}' because they are linked to {author.BookAuthors.Count} book(s) in the catalog.");
            }

            _context.Authors.Remove(author);
            await _context.SaveChangesAsync();
            await _auditLogService.LogAsync("DeleteAuthor", nameof(Author), id.ToString(), $"Deleted author '{author.Name}'.", userId, userEmail);

            return (true, "Author deleted successfully.");
        }
        #endregion
    }
}
