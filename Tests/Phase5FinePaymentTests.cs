using College_Library_Management_System.Data;
using College_Library_Management_System.Models;
using College_Library_Management_System.Models.Enums;
using College_Library_Management_System.Services;
using College_Library_Management_System.Services.Common;
using College_Library_Management_System.ViewModels.Fines;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace CollegeLibrary.Tests
{
    public class Phase5FinePaymentTests
    {
        private ApplicationDbContext CreateInMemoryDbContext(string dbName)
        {
            var options = new DbContextOptionsBuilder<ApplicationDbContext>()
                .UseInMemoryDatabase(databaseName: dbName)
                .Options;

            return new ApplicationDbContext(options);
        }

        private async Task<(Fine fine, Student student)> SeedFineEnvironmentAsync(ApplicationDbContext context, decimal fineAmount = 30.00m)
        {
            var dept = new Department { DepartmentName = "Electrical Engineering", DepartmentCode = "EE" };
            var cat = new Category { Name = "Engineering" };
            var pub = new Publisher { Name = "Science Books" };
            context.Departments.Add(dept);
            context.Categories.Add(cat);
            context.Publishers.Add(pub);

            var student = new Student
            {
                EnrollmentNumber = "EN2026201",
                FullName = "Pooja Verma",
                Email = "pooja@college.edu",
                DepartmentId = dept.DepartmentId,
                Semester = 4,
                IsActive = true
            };
            context.Students.Add(student);

            var book = new Book
            {
                Title = "Circuit Theory & Analysis",
                CategoryId = cat.CategoryId,
                PublisherId = pub.PublisherId,
                PublicationYear = 2021
            };
            context.Books.Add(book);

            var copy = new BookCopy
            {
                BookId = book.BookId,
                AccessionNumber = "ACC-EE-001",
                ShelfLocation = "Rack E-2",
                Status = BookCopyStatus.Available
            };
            context.BookCopies.Add(copy);
            await context.SaveChangesAsync();

            var loan = new Loan
            {
                StudentId = student.StudentId,
                BookCopyId = copy.BookCopyId,
                IssuedAt = DateTime.UtcNow.AddDays(-20),
                DueAt = DateTime.UtcNow.AddDays(-6),
                ReturnedAt = DateTime.UtcNow,
                Status = LoanStatus.Returned,
                IssuedByUserId = "lib1",
                ReceivedByUserId = "lib1"
            };
            context.Loans.Add(loan);
            await context.SaveChangesAsync();

            var fine = new Fine
            {
                LoanId = loan.LoanId,
                FineAmount = fineAmount,
                DailyFineRateApplied = 5.00m,
                AmountPaid = 0m,
                WaivedAmount = 0m,
                Status = FineStatus.Pending,
                CalculatedAt = DateTime.UtcNow
            };
            context.Fines.Add(fine);
            await context.SaveChangesAsync();

            return (fine, student);
        }

        [Fact]
        public async Task CollectFullPayment_ExactBalance_GeneratesReceiptAndMarksPaid()
        {
            using var context = CreateInMemoryDbContext(Guid.NewGuid().ToString());
            var dateTimeProvider = new DateTimeProvider();
            var auditService = new AuditLogService(context, dateTimeProvider);
            var fineService = new FineService(context, auditService, dateTimeProvider);

            var (fine, student) = await SeedFineEnvironmentAsync(context, 25.00m);

            var model = new CollectFinePaymentViewModel
            {
                FineId = fine.FineId,
                PaymentAmount = 25.00m,
                PaymentMethod = PaymentMethod.Cash,
                Remarks = "Paid in cash at circulation counter"
            };

            var (success, message, receiptNumber) = await fineService.CollectFullPaymentAsync(model, "cashier1", "cashier@college.edu");

            Assert.True(success);
            Assert.NotNull(receiptNumber);
            Assert.StartsWith("REC-", receiptNumber);

            var updatedFine = await context.Fines.Include(f => f.FinePayment).FirstOrDefaultAsync(f => f.FineId == fine.FineId);
            Assert.Equal(FineStatus.Paid, updatedFine!.Status);
            Assert.Equal(25.00m, updatedFine.AmountPaid);
            Assert.Equal(0m, updatedFine.RemainingBalance);
            Assert.NotNull(updatedFine.FinePayment);
            Assert.Equal(receiptNumber, updatedFine.FinePayment.ReceiptNumber);
            Assert.Equal(PaymentMethod.Cash, updatedFine.FinePayment.PaymentMethod);
        }

        [Fact]
        public async Task CollectFullPayment_PartialPayment_IsStrictlyRejected()
        {
            using var context = CreateInMemoryDbContext(Guid.NewGuid().ToString());
            var dateTimeProvider = new DateTimeProvider();
            var auditService = new AuditLogService(context, dateTimeProvider);
            var fineService = new FineService(context, auditService, dateTimeProvider);

            var (fine, _) = await SeedFineEnvironmentAsync(context, 50.00m);

            // Attempt partial payment of 20 out of 50
            var model = new CollectFinePaymentViewModel
            {
                FineId = fine.FineId,
                PaymentAmount = 20.00m,
                PaymentMethod = PaymentMethod.Cash
            };

            var (success, message, receiptNumber) = await fineService.CollectFullPaymentAsync(model, "cashier1", "cashier@college.edu");

            Assert.False(success);
            Assert.Null(receiptNumber);
            Assert.Contains("Partial payments are prohibited", message, StringComparison.OrdinalIgnoreCase);

            var dbFine = await context.Fines.FindAsync(fine.FineId);
            Assert.Equal(FineStatus.Pending, dbFine!.Status);
            Assert.Equal(0m, dbFine.AmountPaid);
        }

        [Fact]
        public async Task CollectFullPayment_AlreadyPaidFine_IsRejected()
        {
            using var context = CreateInMemoryDbContext(Guid.NewGuid().ToString());
            var dateTimeProvider = new DateTimeProvider();
            var auditService = new AuditLogService(context, dateTimeProvider);
            var fineService = new FineService(context, auditService, dateTimeProvider);

            var (fine, _) = await SeedFineEnvironmentAsync(context, 15.00m);

            // Pay once
            var model = new CollectFinePaymentViewModel
            {
                FineId = fine.FineId,
                PaymentAmount = 15.00m,
                PaymentMethod = PaymentMethod.Cash
            };
            var (success1, _, _) = await fineService.CollectFullPaymentAsync(model, "cashier1", "cashier@college.edu");
            Assert.True(success1);

            // Pay again
            var (success2, message2, _) = await fineService.CollectFullPaymentAsync(model, "cashier1", "cashier@college.edu");
            Assert.False(success2);
            Assert.Contains("already been fully paid", message2, StringComparison.OrdinalIgnoreCase);
        }

        [Fact]
        public async Task WaiveFine_ValidReason_MarksWaivedAndPreservesAudit()
        {
            using var context = CreateInMemoryDbContext(Guid.NewGuid().ToString());
            var dateTimeProvider = new DateTimeProvider();
            var auditService = new AuditLogService(context, dateTimeProvider);
            var fineService = new FineService(context, auditService, dateTimeProvider);

            var (fine, _) = await SeedFineEnvironmentAsync(context, 40.00m);

            var model = new WaiveFineViewModel
            {
                FineId = fine.FineId,
                WaiverReason = "Approved by Principal for inter-college athletics camp absence."
            };

            var (success, message) = await fineService.WaiveFineAsync(model, "admin1", "admin@college.edu");

            Assert.True(success);

            var updatedFine = await context.Fines.FindAsync(fine.FineId);
            Assert.Equal(FineStatus.Waived, updatedFine!.Status);
            Assert.Equal(40.00m, updatedFine.WaivedAmount);
            Assert.Equal(0m, updatedFine.RemainingBalance);
            Assert.Equal("admin1", updatedFine.WaivedByUserId);
            Assert.Equal(model.WaiverReason, updatedFine.WaiverReason);
        }

        [Fact]
        public async Task WaiveFine_EmptyOrShortReason_IsRejected()
        {
            using var context = CreateInMemoryDbContext(Guid.NewGuid().ToString());
            var dateTimeProvider = new DateTimeProvider();
            var auditService = new AuditLogService(context, dateTimeProvider);
            var fineService = new FineService(context, auditService, dateTimeProvider);

            var (fine, _) = await SeedFineEnvironmentAsync(context, 30.00m);

            // Reason too short (< 10 chars)
            var model = new WaiveFineViewModel
            {
                FineId = fine.FineId,
                WaiverReason = "medical"
            };

            var (success, message) = await fineService.WaiveFineAsync(model, "admin1", "admin@college.edu");

            Assert.False(success);
            Assert.Contains("mandatory", message, StringComparison.OrdinalIgnoreCase);

            var dbFine = await context.Fines.FindAsync(fine.FineId);
            Assert.Equal(FineStatus.Pending, dbFine!.Status);
            Assert.Equal(0m, dbFine.WaivedAmount);
        }
    }
}
