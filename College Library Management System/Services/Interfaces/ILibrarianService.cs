using College_Library_Management_System.ViewModels.Librarians;

namespace College_Library_Management_System.Services.Interfaces
{
    public interface ILibrarianService
    {
        Task<List<LibrarianListItemViewModel>> GetLibrariansAsync(string? searchTerm = null, bool? isActive = null);
        Task<LibrarianFormViewModel?> GetLibrarianForEditAsync(string id);
        Task<(bool Success, string Message, string? GeneratedPassword)> CreateLibrarianAsync(LibrarianFormViewModel model, string adminUserId, string adminEmail);
        Task<(bool Success, string Message)> UpdateLibrarianAsync(LibrarianFormViewModel model, string adminUserId, string adminEmail);
        Task<(bool Success, string Message)> ToggleLibrarianStatusAsync(string id, string currentAdminId, string adminEmail);
        Task<(bool Success, string Message, string? NewPassword)> ResetLibrarianPasswordAsync(string id, string? newPassword, string currentAdminId, string adminEmail);
    }
}
