using College_Library_Management_System.Data;
using College_Library_Management_System.Models;
using College_Library_Management_System.Models.Enums;
using College_Library_Management_System.Services;
using College_Library_Management_System.Services.Common;
using College_Library_Management_System.ViewModels.Circulation;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace CollegeLibrary.Tests
{
    public class Phase4CirculationTests
    {
        private ApplicationDbContext CreateInMemoryDbContext(string dbName)
        {
            var options = new DbContextOptionsBuilder<ApplicationDbContext>()
                .UseInMemoryDatabase(databaseName: dbName)
                .Options;

            return new ApplicationDbContext(options);
        }

        private async Task<(Student student, List<BookCopy> copies, LibrarySetting setting)> SeedCirculationEnvironmentAsync(ApplicationDbContext context)
        {
            var dept = new Department { DepartmentName = "Information Technology", DepartmentCode = "IT" };
            var cat = new Category { Name = "Computer Science" };
            var pub = new Publisher { Name = "Academic Press" };
            context.Departments.Add(dept);
            context.Categories.Add(cat);
            context.Publishers.Add(pub);

            var setting = new LibrarySetting
            {
                LoanPeriodDays = 14,
                DailyFineRate = 5.00m,
                MaxBooksPerStudent = 3,
                BlockIssueWhenOverdue = true,
                AllowRenewal = true,
                MaxRenewalsPerLoan = 1,
                UpdatedAt = DateTime.UtcNow
            };
            context.LibrarySettings.Add(setting);
            await context.SaveChangesAsync();

            var student = new Student
            {
                EnrollmentNumber = "EN2026101",
                FullName = "Amit Patel",
                Email = "amit@college.edu",
                DepartmentId = dept.DepartmentId,
                Semester = 6,
                IsActive = true
            };
            context.Students.Add(student);

            var book = new Book
            {
                Title = "Algorithms in C#",
                CategoryId = cat.CategoryId,
                PublisherId = pub.PublisherId,
                PublicationYear = 2023
            };
            context.Books.Add(book);
            await context.SaveChangesAsync();

            var copies = new List<BookCopy>();
            for (int i = 1; i <= 5; i++)
            {
                var copy = new BookCopy
                {
                    BookId = book.BookId,
                    AccessionNumber = $"ACC-{i:D4}",
                    ShelfLocation = "Rack A-1",
                    Status = BookCopyStatus.Available
                };
                copies.Add(copy);
                context.BookCopies.Add(copy);
            }
            await context.SaveChangesAsync();

            return (student, copies, setting);
        }

        [Fact]
        public async Task IssueBook_MaxThreeBooks_Enforced()
        {
            using var context = CreateInMemoryDbContext(Guid.NewGuid().ToString());
            var dateTimeProvider = new DateTimeProvider();
            var auditService = new AuditLogService(context, dateTimeProvider);
            var loanService = new LoanService(context, auditService, dateTimeProvider);

            var (student, copies, _) = await SeedCirculationEnvironmentAsync(context);

            // Issue 3 books (allowed)
            for (int i = 0; i < 3; i++)
            {
                var model = new IssueBookViewModel
                {
                    StudentEnrollment = student.EnrollmentNumber,
                    AccessionNumber = copies[i].AccessionNumber,
                    LoanPeriodDays = 14
                };
                var (success, _, _) = await loanService.IssueBookAsync(model, "lib1", "lib@college.edu");
                Assert.True(success);
            }

            // Attempt to issue 4th book (must be rejected)
            var fourthModel = new IssueBookViewModel
            {
                StudentEnrollment = student.EnrollmentNumber,
                AccessionNumber = copies[3].AccessionNumber,
                LoanPeriodDays = 14
            };
            var (fourthSuccess, fourthMessage, _) = await loanService.IssueBookAsync(fourthModel, "lib1", "lib@college.edu");

            Assert.False(fourthSuccess);
            Assert.Contains("maximum borrowing limit", fourthMessage, StringComparison.OrdinalIgnoreCase);

            // Verify copy status was not altered
            var fourthCopy = await context.BookCopies.FindAsync(copies[3].BookCopyId);
            Assert.Equal(BookCopyStatus.Available, fourthCopy!.Status);
        }

        [Fact]
        public async Task IssueBook_BlockedWhenStudentHasOverdueLoan()
        {
            using var context = CreateInMemoryDbContext(Guid.NewGuid().ToString());
            var dateTimeProvider = new DateTimeProvider();
            var auditService = new AuditLogService(context, dateTimeProvider);
            var loanService = new LoanService(context, auditService, dateTimeProvider);

            var (student, copies, _) = await SeedCirculationEnvironmentAsync(context);

            // Seed an active overdue loan (due 3 days ago)
            var overdueLoan = new Loan
            {
                StudentId = student.StudentId,
                BookCopyId = copies[0].BookCopyId,
                IssuedAt = dateTimeProvider.Now.AddDays(-17),
                DueAt = dateTimeProvider.Now.AddDays(-3),
                Status = LoanStatus.Active,
                IssuedByUserId = "lib1"
            };
            copies[0].Status = BookCopyStatus.Issued;
            context.Loans.Add(overdueLoan);
            await context.SaveChangesAsync();

            // Attempt to borrow another book
            var model = new IssueBookViewModel
            {
                StudentEnrollment = student.EnrollmentNumber,
                AccessionNumber = copies[1].AccessionNumber,
                LoanPeriodDays = 14
            };

            var (success, message, _) = await loanService.IssueBookAsync(model, "lib1", "lib@college.edu");

            Assert.False(success);
            Assert.Contains("overdue", message, StringComparison.OrdinalIgnoreCase);

            // Verify copy 1 remains available
            var copy1 = await context.BookCopies.FindAsync(copies[1].BookCopyId);
            Assert.Equal(BookCopyStatus.Available, copy1!.Status);
        }

        [Fact]
        public async Task ReturnBook_OnSchedule_IncursZeroFine()
        {
            using var context = CreateInMemoryDbContext(Guid.NewGuid().ToString());
            var dateTimeProvider = new DateTimeProvider();
            var auditService = new AuditLogService(context, dateTimeProvider);
            var loanService = new LoanService(context, auditService, dateTimeProvider);

            var (student, copies, _) = await SeedCirculationEnvironmentAsync(context);

            // Loan issued 5 days ago, due in 9 days
            var loan = new Loan
            {
                StudentId = student.StudentId,
                BookCopyId = copies[0].BookCopyId,
                IssuedAt = dateTimeProvider.Now.AddDays(-5),
                DueAt = dateTimeProvider.Now.AddDays(9),
                Status = LoanStatus.Active,
                IssuedByUserId = "lib1"
            };
            copies[0].Status = BookCopyStatus.Issued;
            context.Loans.Add(loan);
            await context.SaveChangesAsync();

            var returnModel = new ReturnBookViewModel
            {
                LoanId = loan.LoanId,
                ReturnCondition = BookCopyStatus.Available,
                ConditionNotes = "Returned in pristine condition"
            };

            var (success, message, fineAmount) = await loanService.ReturnBookAsync(returnModel, "lib1", "lib@college.edu");

            Assert.True(success);
            Assert.Equal(0m, fineAmount);

            var updatedLoan = await context.Loans.FindAsync(loan.LoanId);
            Assert.Equal(LoanStatus.Returned, updatedLoan!.Status);
            Assert.NotNull(updatedLoan.ReturnedAt);

            var updatedCopy = await context.BookCopies.FindAsync(copies[0].BookCopyId);
            Assert.Equal(BookCopyStatus.Available, updatedCopy!.Status);

            // No Fine record created
            var finesCount = await context.Fines.CountAsync(f => f.LoanId == loan.LoanId);
            Assert.Equal(0, finesCount);
        }

        [Fact]
        public async Task ReturnBook_Overdue_CalculatesFiveRupeesPerDay()
        {
            using var context = CreateInMemoryDbContext(Guid.NewGuid().ToString());
            var dateTimeProvider = new DateTimeProvider();
            var auditService = new AuditLogService(context, dateTimeProvider);
            var loanService = new LoanService(context, auditService, dateTimeProvider);

            var (student, copies, _) = await SeedCirculationEnvironmentAsync(context);

            // Loan was due 4 days ago
            var loan = new Loan
            {
                StudentId = student.StudentId,
                BookCopyId = copies[0].BookCopyId,
                IssuedAt = dateTimeProvider.Now.AddDays(-18),
                DueAt = dateTimeProvider.Now.AddDays(-4),
                Status = LoanStatus.Active,
                IssuedByUserId = "lib1"
            };
            copies[0].Status = BookCopyStatus.Issued;
            context.Loans.Add(loan);
            await context.SaveChangesAsync();

            var returnModel = new ReturnBookViewModel
            {
                LoanId = loan.LoanId,
                ReturnCondition = BookCopyStatus.Available
            };

            var (success, message, fineAmount) = await loanService.ReturnBookAsync(returnModel, "lib1", "lib@college.edu");

            Assert.True(success);
            // 4 overdue days * ₹5.00 = ₹20.00
            Assert.Equal(20.00m, fineAmount);

            var fineRecord = await context.Fines.FirstOrDefaultAsync(f => f.LoanId == loan.LoanId);
            Assert.NotNull(fineRecord);
            Assert.Equal(20.00m, fineRecord.FineAmount);
            Assert.Equal(5.00m, fineRecord.DailyFineRateApplied);
            Assert.Equal(FineStatus.Pending, fineRecord.Status);
            Assert.Equal(0m, fineRecord.AmountPaid);
        }

        [Fact]
        public async Task RenewLoan_EligibleActiveLoan_ExtendsDueDateByFourteenDays()
        {
            using var context = CreateInMemoryDbContext(Guid.NewGuid().ToString());
            var dateTimeProvider = new DateTimeProvider();
            var auditService = new AuditLogService(context, dateTimeProvider);
            var loanService = new LoanService(context, auditService, dateTimeProvider);

            var (student, copies, _) = await SeedCirculationEnvironmentAsync(context);

            var initialDueAt = dateTimeProvider.Now.AddDays(5);
            var loan = new Loan
            {
                StudentId = student.StudentId,
                BookCopyId = copies[0].BookCopyId,
                IssuedAt = dateTimeProvider.Now.AddDays(-9),
                DueAt = initialDueAt,
                Status = LoanStatus.Active,
                RenewalCount = 0,
                IssuedByUserId = "lib1"
            };
            copies[0].Status = BookCopyStatus.Issued;
            context.Loans.Add(loan);
            await context.SaveChangesAsync();

            var (success, _, newDueAt) = await loanService.RenewLoanAsync(loan.LoanId, "lib1", "lib@college.edu");

            Assert.True(success);
            Assert.NotNull(newDueAt);

            var updatedLoan = await context.Loans.FindAsync(loan.LoanId);
            Assert.Equal(1, updatedLoan!.RenewalCount);
            // Extended by 14 days
            Assert.Equal(initialDueAt.AddDays(14).Date, updatedLoan.DueAt.Date);
        }

        [Fact]
        public async Task RenewLoan_AlreadyRenewedOnce_IsRejected()
        {
            using var context = CreateInMemoryDbContext(Guid.NewGuid().ToString());
            var dateTimeProvider = new DateTimeProvider();
            var auditService = new AuditLogService(context, dateTimeProvider);
            var loanService = new LoanService(context, auditService, dateTimeProvider);

            var (student, copies, _) = await SeedCirculationEnvironmentAsync(context);

            var loan = new Loan
            {
                StudentId = student.StudentId,
                BookCopyId = copies[0].BookCopyId,
                IssuedAt = dateTimeProvider.Now.AddDays(-10),
                DueAt = dateTimeProvider.Now.AddDays(4),
                Status = LoanStatus.Active,
                RenewalCount = 1, // Already renewed once
                IssuedByUserId = "lib1"
            };
            copies[0].Status = BookCopyStatus.Issued;
            context.Loans.Add(loan);
            await context.SaveChangesAsync();

            var (success, message, _) = await loanService.RenewLoanAsync(loan.LoanId, "lib1", "lib@college.edu");

            Assert.False(success);
            Assert.Contains("Maximum renewal limit", message, StringComparison.OrdinalIgnoreCase);
        }

        [Fact]
        public async Task RenewLoan_OverdueLoan_IsRejected()
        {
            using var context = CreateInMemoryDbContext(Guid.NewGuid().ToString());
            var dateTimeProvider = new DateTimeProvider();
            var auditService = new AuditLogService(context, dateTimeProvider);
            var loanService = new LoanService(context, auditService, dateTimeProvider);

            var (student, copies, _) = await SeedCirculationEnvironmentAsync(context);

            var loan = new Loan
            {
                StudentId = student.StudentId,
                BookCopyId = copies[0].BookCopyId,
                IssuedAt = dateTimeProvider.Now.AddDays(-16),
                DueAt = dateTimeProvider.Now.AddDays(-2), // Overdue by 2 days
                Status = LoanStatus.Active,
                RenewalCount = 0,
                IssuedByUserId = "lib1"
            };
            copies[0].Status = BookCopyStatus.Issued;
            context.Loans.Add(loan);
            await context.SaveChangesAsync();

            var (success, message, _) = await loanService.RenewLoanAsync(loan.LoanId, "lib1", "lib@college.edu");

            Assert.False(success);
            Assert.Contains("Overdue loans cannot be renewed", message, StringComparison.OrdinalIgnoreCase);
        }
    }
}
