using College_Library_Management_System.Models;
using College_Library_Management_System.Models.Enums;
using College_Library_Management_System.Services.Common;
using Xunit;

namespace CollegeLibrary.Tests
{
    public class Phase1FoundationTests
    {
        [Fact]
        public void LibrarySetting_DefaultValues_MatchConfirmedBusinessRules()
        {
            var setting = new LibrarySetting();

            Assert.Equal(14, setting.LoanPeriodDays);
            Assert.Equal(5.00m, setting.DailyFineRate);
            Assert.Equal(3, setting.MaxBooksPerStudent);
            Assert.True(setting.BlockIssueWhenOverdue);
            Assert.True(setting.AllowRenewal);
            Assert.Equal(1, setting.MaxRenewalsPerLoan);
        }

        [Fact]
        public void Fine_RemainingBalance_CalculatesAccurately()
        {
            var fine = new Fine
            {
                FineAmount = 50.00m,
                AmountPaid = 20.00m,
                WaivedAmount = 10.00m
            };

            Assert.Equal(20.00m, fine.RemainingBalance);
        }

        [Fact]
        public void Fine_RemainingBalance_DoesNotGoBelowZero()
        {
            var fine = new Fine
            {
                FineAmount = 50.00m,
                AmountPaid = 50.00m,
                WaivedAmount = 10.00m
            };

            Assert.Equal(0m, fine.RemainingBalance);
        }

        [Fact]
        public void DateTimeProvider_UsesIndiaStandardTimeOrLocalOffset()
        {
            var provider = new DateTimeProvider();
            var now = provider.Now;
            var utcNow = provider.UtcNow;

            Assert.NotNull(provider.LibraryTimeZone);
            Assert.True(now >= utcNow.AddHours(5) || provider.LibraryTimeZone.BaseUtcOffset == TimeZoneInfo.Local.BaseUtcOffset);
            Assert.Equal(now.Date, provider.Today);
        }

        [Theory]
        [InlineData(BookCopyStatus.Available, 0)]
        [InlineData(BookCopyStatus.Issued, 1)]
        [InlineData(BookCopyStatus.Damaged, 2)]
        [InlineData(BookCopyStatus.Lost, 3)]
        [InlineData(BookCopyStatus.Withdrawn, 4)]
        public void BookCopyStatus_HasExpectedIntegerValues(BookCopyStatus status, int expectedValue)
        {
            Assert.Equal(expectedValue, (int)status);
        }

        [Theory]
        [InlineData(LoanStatus.Active, 0)]
        [InlineData(LoanStatus.Returned, 1)]
        [InlineData(LoanStatus.Lost, 2)]
        public void LoanStatus_HasExpectedIntegerValues(LoanStatus status, int expectedValue)
        {
            Assert.Equal(expectedValue, (int)status);
        }
    }
}
