using College_Library_Management_System.Data;
using College_Library_Management_System.Models;
using College_Library_Management_System.Services;
using College_Library_Management_System.Services.Common;
using College_Library_Management_System.ViewModels.Books;
using College_Library_Management_System.ViewModels.Departments;
using College_Library_Management_System.ViewModels.MasterData;
using College_Library_Management_System.ViewModels.Students;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace CollegeLibrary.Tests
{
    public class Phase2MasterDataTests
    {
        private ApplicationDbContext CreateInMemoryDbContext(string dbName)
        {
            var options = new DbContextOptionsBuilder<ApplicationDbContext>()
                .UseInMemoryDatabase(databaseName: dbName)
                .Options;

            return new ApplicationDbContext(options);
        }

        [Fact]
        public async Task DepartmentService_DuplicateCode_IsRejected()
        {
            using var context = CreateInMemoryDbContext(Guid.NewGuid().ToString());
            var dateTimeProvider = new DateTimeProvider();
            var auditService = new AuditLogService(context, dateTimeProvider);
            var deptService = new DepartmentService(context, auditService);

            // Add first department
            var dept1 = new DepartmentFormViewModel { DepartmentName = "Computer Science", DepartmentCode = "CSE" };
            var (success1, _, _) = await deptService.CreateDepartmentAsync(dept1, "test-user", "test@college.edu");
            Assert.True(success1);

            // Try to add second department with identical code (case-insensitive)
            var dept2 = new DepartmentFormViewModel { DepartmentName = "Computational Software Engineering", DepartmentCode = "cse" };
            var (success2, message2, _) = await deptService.CreateDepartmentAsync(dept2, "test-user", "test@college.edu");

            Assert.False(success2);
            Assert.Contains("already registered", message2, StringComparison.OrdinalIgnoreCase);
        }

        [Fact]
        public async Task DepartmentService_DeleteReferencedDepartment_IsPrevented()
        {
            using var context = CreateInMemoryDbContext(Guid.NewGuid().ToString());
            var dateTimeProvider = new DateTimeProvider();
            var auditService = new AuditLogService(context, dateTimeProvider);
            var deptService = new DepartmentService(context, auditService);

            var dept = new Department { DepartmentName = "Mechanical Engineering", DepartmentCode = "ME" };
            context.Departments.Add(dept);
            await context.SaveChangesAsync();

            // Add student in this department
            var student = new Student
            {
                EnrollmentNumber = "EN2026ME001",
                FullName = "Rahul Varma",
                DepartmentId = dept.DepartmentId,
                Semester = 1,
                IsActive = true
            };
            context.Students.Add(student);
            await context.SaveChangesAsync();

            // Attempt to delete department
            var (success, message) = await deptService.DeleteDepartmentAsync(dept.DepartmentId, "test-user", "test@college.edu");

            Assert.False(success);
            Assert.Contains("Cannot delete department", message);
            Assert.NotNull(await context.Departments.FindAsync(dept.DepartmentId));
        }

        [Fact]
        public async Task StudentService_DuplicateEnrollmentNumber_IsRejected()
        {
            using var context = CreateInMemoryDbContext(Guid.NewGuid().ToString());
            var dateTimeProvider = new DateTimeProvider();
            var auditService = new AuditLogService(context, dateTimeProvider);
            var studentService = new StudentService(context, dateTimeProvider, auditService);

            var dept = new Department { DepartmentName = "Information Technology", DepartmentCode = "IT" };
            context.Departments.Add(dept);
            await context.SaveChangesAsync();

            var student1 = new StudentFormViewModel
            {
                EnrollmentNumber = "EN2026IT010",
                FullName = "Pooja Patel",
                DepartmentId = dept.DepartmentId,
                Semester = 2,
                IsActive = true
            };

            var (success1, _, _) = await studentService.CreateStudentAsync(student1, "test-user", "test@college.edu");
            Assert.True(success1);

            // Try to create another student with same enrollment number
            var student2 = new StudentFormViewModel
            {
                EnrollmentNumber = "en2026it010", // lowercase check
                FullName = "Pooja K. Patel",
                DepartmentId = dept.DepartmentId,
                Semester = 2,
                IsActive = true
            };

            var (success2, message2, _) = await studentService.CreateStudentAsync(student2, "test-user", "test@college.edu");
            Assert.False(success2);
            Assert.Contains("already registered", message2);
        }

        [Fact]
        public async Task StudentService_NonExistentDepartment_IsRejected()
        {
            using var context = CreateInMemoryDbContext(Guid.NewGuid().ToString());
            var dateTimeProvider = new DateTimeProvider();
            var auditService = new AuditLogService(context, dateTimeProvider);
            var studentService = new StudentService(context, dateTimeProvider, auditService);

            var student = new StudentFormViewModel
            {
                EnrollmentNumber = "EN2026EE050",
                FullName = "Aditya Roy",
                DepartmentId = 99999, // does not exist
                Semester = 1,
                IsActive = true
            };

            var (success, message, _) = await studentService.CreateStudentAsync(student, "test-user", "test@college.edu");
            Assert.False(success);
            Assert.Contains("does not exist", message);
        }

        [Fact]
        public async Task BookService_DuplicateIsbn_IsRejected()
        {
            using var context = CreateInMemoryDbContext(Guid.NewGuid().ToString());
            var dateTimeProvider = new DateTimeProvider();
            var auditService = new AuditLogService(context, dateTimeProvider);
            var bookService = new BookService(context, dateTimeProvider, auditService);

            var category = new Category { Name = "Computer Science" };
            var publisher = new Publisher { Name = "Pearson" };
            var author = new Author { Name = "Tanenbaum" };
            context.Categories.Add(category);
            context.Publishers.Add(publisher);
            context.Authors.Add(author);
            await context.SaveChangesAsync();

            var book1 = new BookFormViewModel
            {
                Title = "Computer Networks",
                ISBN = "978-0132126953",
                CategoryId = category.CategoryId,
                PublisherId = publisher.PublisherId,
                SelectedAuthorIds = new List<int> { author.AuthorId }
            };

            var (success1, _, _) = await bookService.CreateBookAsync(book1, "test-user", "test@college.edu");
            Assert.True(success1);

            // Attempt to add another book with identical ISBN
            var book2 = new BookFormViewModel
            {
                Title = "Computer Networks 6th Ed",
                ISBN = "978-0132126953",
                CategoryId = category.CategoryId,
                PublisherId = publisher.PublisherId,
                SelectedAuthorIds = new List<int> { author.AuthorId }
            };

            var (success2, message2, _) = await bookService.CreateBookAsync(book2, "test-user", "test@college.edu");
            Assert.False(success2);
            Assert.Contains("already registered", message2);
        }

        [Fact]
        public async Task BookService_RequiresAtLeastOneAuthor()
        {
            using var context = CreateInMemoryDbContext(Guid.NewGuid().ToString());
            var dateTimeProvider = new DateTimeProvider();
            var auditService = new AuditLogService(context, dateTimeProvider);
            var bookService = new BookService(context, dateTimeProvider, auditService);

            var category = new Category { Name = "Mathematics" };
            var publisher = new Publisher { Name = "Cambridge" };
            context.Categories.Add(category);
            context.Publishers.Add(publisher);
            await context.SaveChangesAsync();

            var book = new BookFormViewModel
            {
                Title = "Linear Algebra Done Right",
                CategoryId = category.CategoryId,
                PublisherId = publisher.PublisherId,
                SelectedAuthorIds = new List<int>() // empty authors
            };

            var (success, message, _) = await bookService.CreateBookAsync(book, "test-user", "test@college.edu");
            Assert.False(success);
            Assert.Contains("at least one author", message);
        }

        [Fact]
        public async Task BookService_SupportsMultipleAuthorsPerBook()
        {
            using var context = CreateInMemoryDbContext(Guid.NewGuid().ToString());
            var dateTimeProvider = new DateTimeProvider();
            var auditService = new AuditLogService(context, dateTimeProvider);
            var bookService = new BookService(context, dateTimeProvider, auditService);

            var category = new Category { Name = "Algorithms" };
            var publisher = new Publisher { Name = "MIT Press" };
            var author1 = new Author { Name = "Thomas H. Cormen" };
            var author2 = new Author { Name = "Charles E. Leiserson" };
            var author3 = new Author { Name = "Ronald L. Rivest" };
            var author4 = new Author { Name = "Clifford Stein" };

            context.Categories.Add(category);
            context.Publishers.Add(publisher);
            context.Authors.AddRange(author1, author2, author3, author4);
            await context.SaveChangesAsync();

            var book = new BookFormViewModel
            {
                Title = "Introduction to Algorithms (CLRS)",
                ISBN = "978-0262033848",
                PublicationYear = 2009,
                CategoryId = category.CategoryId,
                PublisherId = publisher.PublisherId,
                SelectedAuthorIds = new List<int> { author1.AuthorId, author2.AuthorId, author3.AuthorId, author4.AuthorId }
            };

            var (success, _, bookId) = await bookService.CreateBookAsync(book, "test-user", "test@college.edu");
            Assert.True(success);
            Assert.NotNull(bookId);

            var createdBook = await context.Books
                .Include(b => b.BookAuthors)
                .FirstOrDefaultAsync(b => b.BookId == bookId.Value);

            Assert.NotNull(createdBook);
            Assert.Equal(4, createdBook.BookAuthors.Count);
        }
    }
}
