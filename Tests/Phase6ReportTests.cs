using College_Library_Management_System.Data;
using College_Library_Management_System.Models;
using College_Library_Management_System.Models.Enums;
using College_Library_Management_System.Services;
using College_Library_Management_System.Services.Common;
using College_Library_Management_System.ViewModels.Reports;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace CollegeLibrary.Tests
{
    public class Phase6ReportTests
    {
        private ApplicationDbContext CreateInMemoryDbContext(string dbName)
        {
            var options = new DbContextOptionsBuilder<ApplicationDbContext>()
                .UseInMemoryDatabase(databaseName: dbName)
                .Options;

            return new ApplicationDbContext(options);
        }

        private async Task SeedReportEnvironmentAsync(ApplicationDbContext context)
        {
            var dept1 = new Department { DepartmentName = "Computer Science", DepartmentCode = "CSE" };
            var dept2 = new Department { DepartmentName = "Mechanical Engineering", DepartmentCode = "ME" };
            var cat = new Category { Name = "Data Science" };
            var pub = new Publisher { Name = "Global Press" };
            var staff = new ApplicationUser
            {
                Id = "lib1",
                UserName = "lib@college.edu",
                Email = "lib@college.edu",
                FullName = "Chief Librarian",
                IsActive = true
            };
            context.Departments.AddRange(dept1, dept2);
            context.Categories.Add(cat);
            context.Publishers.Add(pub);
            context.Users.Add(staff);

            var s1 = new Student
            {
                EnrollmentNumber = "EN2026301",
                FullName = "Vikram Singh",
                Email = "vikram@college.edu",
                DepartmentId = dept1.DepartmentId,
                Semester = 5,
                IsActive = true
            };
            var s2 = new Student
            {
                EnrollmentNumber = "EN2026302",
                FullName = "Ananya Roy",
                Email = "ananya@college.edu",
                DepartmentId = dept2.DepartmentId,
                Semester = 3,
                IsActive = true
            };
            context.Students.AddRange(s1, s2);

            var book1 = new Book { Title = "Python for Data Analysis", CategoryId = cat.CategoryId, PublisherId = pub.PublisherId, PublicationYear = 2022 };
            var book2 = new Book { Title = "Machine Learning in Action", CategoryId = cat.CategoryId, PublisherId = pub.PublisherId, PublicationYear = 2023 };
            context.Books.AddRange(book1, book2);
            await context.SaveChangesAsync();

            var c1 = new BookCopy { BookId = book1.BookId, AccessionNumber = "ACC-DS-001", ShelfLocation = "A-1", Status = BookCopyStatus.Issued };
            var c2 = new BookCopy { BookId = book1.BookId, AccessionNumber = "ACC-DS-002", ShelfLocation = "A-1", Status = BookCopyStatus.Available };
            var c3 = new BookCopy { BookId = book2.BookId, AccessionNumber = "ACC-DS-003", ShelfLocation = "A-2", Status = BookCopyStatus.Damaged };
            context.BookCopies.AddRange(c1, c2, c3);
            await context.SaveChangesAsync();

            // Active loan - overdue
            var l1 = new Loan
            {
                StudentId = s1.StudentId,
                BookCopyId = c1.BookCopyId,
                IssuedAt = DateTime.UtcNow.AddDays(-20),
                DueAt = DateTime.UtcNow.AddDays(-6),
                Status = LoanStatus.Active,
                IssuedByUserId = "lib1"
            };
            context.Loans.Add(l1);

            // Fine payment
            var fine = new Fine
            {
                LoanId = l1.LoanId,
                FineAmount = 30.00m,
                DailyFineRateApplied = 5.00m,
                AmountPaid = 30.00m,
                Status = FineStatus.Paid,
                CalculatedAt = DateTime.UtcNow.AddDays(-1)
            };
            context.Fines.Add(fine);

            var payment = new FinePayment
            {
                FineId = fine.FineId,
                Amount = 30.00m,
                PaymentMethod = PaymentMethod.Cash,
                ReceivedAt = DateTime.UtcNow.AddDays(-1),
                ReceiptNumber = "REC-20261025-ABC123",
                ReceivedByUserId = "lib1"
            };
            context.FinePayments.Add(payment);

            var auditLog = new AuditLog
            {
                Action = "Issue",
                EntityType = "Loan",
                EntityId = l1.LoanId.ToString(),
                UserId = "lib1",
                UserEmail = "lib@college.edu",
                Timestamp = DateTime.UtcNow.AddDays(-20),
                Details = "Initial issue of ACC-DS-001"
            };
            context.AuditLogs.Add(auditLog);

            await context.SaveChangesAsync();
        }

        [Fact]
        public async Task ReportService_GetDashboardMetrics_ReturnsAccurateTotals()
        {
            using var context = CreateInMemoryDbContext(Guid.NewGuid().ToString());
            var dateTimeProvider = new DateTimeProvider();
            var reportService = new ReportService(context, dateTimeProvider);

            await SeedReportEnvironmentAsync(context);

            var metrics = await reportService.GetDashboardMetricsAsync();

            Assert.Equal(2, metrics.TotalBooks);
            Assert.Equal(3, metrics.TotalCopies);
            Assert.Equal(1, metrics.AvailableCopies);
            Assert.Equal(1, metrics.IssuedCopies);
            Assert.Equal(1, metrics.DamagedCopies);
            Assert.Equal(2, metrics.TotalStudents);
            Assert.Equal(1, metrics.ActiveLoans);
            Assert.Equal(1, metrics.OverdueLoans);
            Assert.Equal(30.00m, metrics.TotalFinesCollected);
        }

        [Fact]
        public async Task ReportService_GetOverdueReport_FiltersAccurately()
        {
            using var context = CreateInMemoryDbContext(Guid.NewGuid().ToString());
            var dateTimeProvider = new DateTimeProvider();
            var reportService = new ReportService(context, dateTimeProvider);

            await SeedReportEnvironmentAsync(context);

            var filter = new OverdueReportFilterViewModel { MinDaysOverdue = 5 };
            var result = await reportService.GetOverdueReportAsync(filter);

            Assert.Equal(1, result.TotalOverdueCount);
            Assert.True(result.TotalEstimatedFine >= 30.00m);
            Assert.Equal("EN2026301", result.Items[0].EnrollmentNumber);
        }

        [Fact]
        public async Task ReportService_GetInventoryReport_SummarizesCategoriesAndDepts()
        {
            using var context = CreateInMemoryDbContext(Guid.NewGuid().ToString());
            var dateTimeProvider = new DateTimeProvider();
            var reportService = new ReportService(context, dateTimeProvider);

            await SeedReportEnvironmentAsync(context);

            var report = await reportService.GetInventoryReportAsync();

            Assert.Equal(2, report.TotalCatalogTitles);
            Assert.Equal(3, report.TotalPhysicalCopies);
            Assert.Single(report.CategoryBreakdown);
            Assert.Equal("Data Science", report.CategoryBreakdown[0].CategoryName);
            Assert.Equal(3, report.CategoryBreakdown[0].CopyCount);
            Assert.Equal(2, report.DepartmentBreakdown.Count);
        }

        [Fact]
        public async Task ReportService_GetCollectionReport_CalculatesTotalRevenue()
        {
            using var context = CreateInMemoryDbContext(Guid.NewGuid().ToString());
            var dateTimeProvider = new DateTimeProvider();
            var reportService = new ReportService(context, dateTimeProvider);

            await SeedReportEnvironmentAsync(context);

            var filter = new CollectionReportFilterViewModel { PaymentMethod = PaymentMethod.Cash };
            var result = await reportService.GetCollectionReportAsync(filter);

            Assert.Equal(1, result.TransactionsCount);
            Assert.Equal(30.00m, result.TotalCollected);
            Assert.Equal("REC-20261025-ABC123", result.Transactions[0].ReceiptNumber);
        }
    }
}
