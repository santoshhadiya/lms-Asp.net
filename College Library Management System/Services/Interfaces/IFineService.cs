using College_Library_Management_System.ViewModels.Common;
using College_Library_Management_System.ViewModels.Fines;
using Microsoft.AspNetCore.Mvc.Rendering;

namespace College_Library_Management_System.Services.Interfaces
{
    public interface IFineService
    {
        Task<PaginatedList<FineListItemViewModel>> GetFinesAsync(FineListFilterViewModel filter);
        Task<CollectFinePaymentViewModel?> GetFineForPaymentAsync(int fineId);
        Task<(bool Success, string Message, string? ReceiptNumber)> CollectFullPaymentAsync(CollectFinePaymentViewModel model, string userId, string userEmail);
        Task<WaiveFineViewModel?> GetFineForWaiverAsync(int fineId);
        Task<(bool Success, string Message)> WaiveFineAsync(WaiveFineViewModel model, string userId, string userEmail);
        Task<FineReceiptViewModel?> GetReceiptByNumberAsync(string receiptNumber);
        Task<FineReceiptViewModel?> GetReceiptByFineIdAsync(int fineId);
        Task<List<SelectListItem>> GetDepartmentOptionsAsync();
    }
}
