using College_Library_Management_System.ViewModels.StudentDashboard;

namespace College_Library_Management_System.Services.Interfaces
{
    public interface IStudentDashboardService
    {
        Task<StudentDashboardViewModel?> GetStudentDashboardDataAsync(string userId, string? userEmail);
    }
}
