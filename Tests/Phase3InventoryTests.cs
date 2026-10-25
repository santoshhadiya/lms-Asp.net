using College_Library_Management_System.Data;
using College_Library_Management_System.Models;
using College_Library_Management_System.Models.Enums;
using College_Library_Management_System.Services;
using College_Library_Management_System.Services.Common;
using College_Library_Management_System.ViewModels.Inventory;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace CollegeLibrary.Tests
{
    public class Phase3InventoryTests
    {
        private ApplicationDbContext CreateInMemoryDbContext(string dbName)
        {
            var options = new DbContextOptionsBuilder<ApplicationDbContext>()
                .UseInMemoryDatabase(databaseName: dbName)
                .Options;

            return new ApplicationDbContext(options);
        }

        private async Task<(Book book, Department dept, Student student)> SeedBasicDataAsync(ApplicationDbContext context)
        {
            var dept = new Department { DepartmentName = "Computer Science", DepartmentCode = "CSE" };
            var cat = new Category { Name = "Programming" };
            var pub = new Publisher { Name = "Tech Press" };
            context.Departments.Add(dept);
            context.Categories.Add(cat);
            context.Publishers.Add(pub);
            await context.SaveChangesAsync();

            var book = new Book
            {
                Title = "Clean Code Architecture",
                ISBN = "978-0132350884",
                CategoryId = cat.CategoryId,
                PublisherId = pub.PublisherId,
                PublicationYear = 2022
            };
            context.Books.Add(book);

            var student = new Student
            {
                EnrollmentNumber = "EN2026001",
                FullName = "Rahul Sharma",
                Email = "rahul@college.edu",
                DepartmentId = dept.DepartmentId,
                Semester = 5,
                IsActive = true
            };
            context.Students.Add(student);
            await context.SaveChangesAsync();

            return (book, dept, student);
        }

        [Fact]
        public async Task BookCopyService_DuplicateAccessionNumber_IsRejected()
        {
            using var context = CreateInMemoryDbContext(Guid.NewGuid().ToString());
            var dateTimeProvider = new DateTimeProvider();
            var auditService = new AuditLogService(context, dateTimeProvider);
            var copyService = new BookCopyService(context, dateTimeProvider, auditService);

            var (book, _, _) = await SeedBasicDataAsync(context);

            var copy1 = new BookCopyFormViewModel
            {
                BookId = book.BookId,
                AccessionNumber = "ACC-0001",
                ShelfLocation = "Rack A-1",
                Status = BookCopyStatus.Available
            };
            var (success1, _, _) = await copyService.CreateCopyAsync(copy1, "user1", "user1@college.edu");
            Assert.True(success1);

            // Attempt to create duplicate accession number (with whitespace/case differences)
            var copy2 = new BookCopyFormViewModel
            {
                BookId = book.BookId,
                AccessionNumber = "  acc-0001  ",
                ShelfLocation = "Rack A-2",
                Status = BookCopyStatus.Available
            };
            var (success2, message2, _) = await copyService.CreateCopyAsync(copy2, "user1", "user1@college.edu");

            Assert.False(success2);
            Assert.Contains("already assigned", message2, StringComparison.OrdinalIgnoreCase);
        }

        [Fact]
        public async Task BookCopyService_BatchCopyCreation_GeneratesSequentialCopies()
        {
            using var context = CreateInMemoryDbContext(Guid.NewGuid().ToString());
            var dateTimeProvider = new DateTimeProvider();
            var auditService = new AuditLogService(context, dateTimeProvider);
            var copyService = new BookCopyService(context, dateTimeProvider, auditService);

            var (book, _, _) = await SeedBasicDataAsync(context);

            var batchModel = new BatchCopyFormViewModel
            {
                BookId = book.BookId,
                Prefix = "LIB-",
                StartingNumber = 101,
                NumberOfCopies = 5,
                ShelfLocation = "Rack B-2",
                ConditionNotes = "Brand New"
            };

            var (success, message, createdCount) = await copyService.CreateBatchCopiesAsync(batchModel, "user1", "user1@college.edu");

            Assert.True(success);
            Assert.Equal(5, createdCount);

            var copies = await context.BookCopies
                .Where(c => c.BookId == book.BookId)
                .OrderBy(c => c.AccessionNumber)
                .ToListAsync();

            Assert.Equal(5, copies.Count);
            Assert.Equal("LIB-0101", copies[0].AccessionNumber);
            Assert.Equal("LIB-0102", copies[1].AccessionNumber);
            Assert.Equal("LIB-0103", copies[2].AccessionNumber);
            Assert.Equal("LIB-0104", copies[3].AccessionNumber);
            Assert.Equal("LIB-0105", copies[4].AccessionNumber);
            Assert.All(copies, c => Assert.Equal(BookCopyStatus.Available, c.Status));
        }

        [Fact]
        public async Task BookCopyService_BatchCopyCreation_RejectsIfAnyAccessionExists()
        {
            using var context = CreateInMemoryDbContext(Guid.NewGuid().ToString());
            var dateTimeProvider = new DateTimeProvider();
            var auditService = new AuditLogService(context, dateTimeProvider);
            var copyService = new BookCopyService(context, dateTimeProvider, auditService);

            var (book, _, _) = await SeedBasicDataAsync(context);

            // Pre-seed LIB-0103
            context.BookCopies.Add(new BookCopy
            {
                BookId = book.BookId,
                AccessionNumber = "LIB-0103",
                ShelfLocation = "Rack B-1",
                Status = BookCopyStatus.Available
            });
            await context.SaveChangesAsync();

            var batchModel = new BatchCopyFormViewModel
            {
                BookId = book.BookId,
                Prefix = "LIB-",
                StartingNumber = 101,
                NumberOfCopies = 5,
                ShelfLocation = "Rack B-2"
            };

            var (success, message, createdCount) = await copyService.CreateBatchCopiesAsync(batchModel, "user1", "user1@college.edu");

            Assert.False(success);
            Assert.Contains("LIB-0103", message);
            Assert.Equal(0, createdCount);

            // Verify transaction safety - only the pre-seeded copy exists
            var totalCopies = await context.BookCopies.CountAsync();
            Assert.Equal(1, totalCopies);
        }

        [Fact]
        public async Task BookCopyService_ChangeStatus_RejectsWhenActiveLoanExists()
        {
            using var context = CreateInMemoryDbContext(Guid.NewGuid().ToString());
            var dateTimeProvider = new DateTimeProvider();
            var auditService = new AuditLogService(context, dateTimeProvider);
            var copyService = new BookCopyService(context, dateTimeProvider, auditService);

            var (book, _, student) = await SeedBasicDataAsync(context);

            var copy = new BookCopy
            {
                BookId = book.BookId,
                AccessionNumber = "ACC-0999",
                ShelfLocation = "Rack C-1",
                Status = BookCopyStatus.Issued
            };
            context.BookCopies.Add(copy);
            await context.SaveChangesAsync();

            // Add active loan
            var loan = new Loan
            {
                BookCopyId = copy.BookCopyId,
                StudentId = student.StudentId,
                IssuedAt = DateTime.UtcNow.AddDays(-5),
                DueAt = DateTime.UtcNow.AddDays(9),
                Status = LoanStatus.Active,
                IssuedByUserId = "librarian1"
            };
            context.Loans.Add(loan);
            await context.SaveChangesAsync();

            // Attempt to update status to Available
            var (success, message) = await copyService.UpdateCopyStatusAsync(
                copy.BookCopyId,
                BookCopyStatus.Available,
                "Trying to force available",
                "user1",
                "user1@college.edu");

            Assert.False(success);
            Assert.Contains("currently issued on loan", message, StringComparison.OrdinalIgnoreCase);

            var dbCopy = await context.BookCopies.FindAsync(copy.BookCopyId);
            Assert.Equal(BookCopyStatus.Issued, dbCopy!.Status);
        }

        [Fact]
        public async Task BookCopyService_ChangeStatus_SucceedsWhenNoActiveLoan()
        {
            using var context = CreateInMemoryDbContext(Guid.NewGuid().ToString());
            var dateTimeProvider = new DateTimeProvider();
            var auditService = new AuditLogService(context, dateTimeProvider);
            var copyService = new BookCopyService(context, dateTimeProvider, auditService);

            var (book, _, _) = await SeedBasicDataAsync(context);

            var copy = new BookCopy
            {
                BookId = book.BookId,
                AccessionNumber = "ACC-1000",
                ShelfLocation = "Rack C-1",
                Status = BookCopyStatus.Available
            };
            context.BookCopies.Add(copy);
            await context.SaveChangesAsync();

            var (success, _) = await copyService.UpdateCopyStatusAsync(
                copy.BookCopyId,
                BookCopyStatus.Damaged,
                "Cover torn during maintenance",
                "user1",
                "user1@college.edu");

            Assert.True(success);

            var dbCopy = await context.BookCopies.FindAsync(copy.BookCopyId);
            Assert.Equal(BookCopyStatus.Damaged, dbCopy!.Status);
            Assert.Equal("Cover torn during maintenance", dbCopy.ConditionNotes);
        }
    }
}
