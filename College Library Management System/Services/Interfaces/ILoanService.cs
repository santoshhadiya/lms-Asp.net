using College_Library_Management_System.Models;
using College_Library_Management_System.ViewModels.Circulation;
using College_Library_Management_System.ViewModels.Common;
using Microsoft.AspNetCore.Mvc.Rendering;

namespace College_Library_Management_System.Services.Interfaces
{
    public interface ILoanService
    {
        Task<CirculationEligibilityResult> CheckIssueEligibilityAsync(string enrollmentNumber, string accessionNumber);
        Task<(bool Success, string Message, int? LoanId)> IssueBookAsync(IssueBookViewModel model, string userId, string userEmail);
        Task<ReturnBookViewModel?> GetLoanForReturnAsync(int loanId);
        Task<ReturnBookViewModel?> GetActiveLoanByAccessionAsync(string accessionNumber);
        Task<(bool Success, string Message, decimal FineAmount)> ReturnBookAsync(ReturnBookViewModel model, string userId, string userEmail);
        Task<RenewLoanViewModel?> GetLoanForRenewalAsync(int loanId);
        Task<(bool Success, string Message, DateTime? NewDueAt)> RenewLoanAsync(int loanId, string userId, string userEmail);
        Task<PaginatedList<ActiveLoanListItemViewModel>> GetActiveLoansAsync(ActiveLoanListFilterViewModel filter);
        Task<List<SelectListItem>> GetDepartmentOptionsAsync();
        Task<LibrarySetting> GetLibrarySettingAsync();
    }
}
