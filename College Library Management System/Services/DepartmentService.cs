using College_Library_Management_System.Data;
using College_Library_Management_System.Models;
using College_Library_Management_System.Services.Interfaces;
using College_Library_Management_System.ViewModels.Departments;
using Microsoft.EntityFrameworkCore;

namespace College_Library_Management_System.Services.Interfaces
{
    public interface IDepartmentService
    {
        Task<List<DepartmentItemViewModel>> GetAllDepartmentsAsync();
        Task<DepartmentFormViewModel?> GetDepartmentForEditAsync(int departmentId);
        Task<Department?> GetDepartmentByIdAsync(int departmentId);
        Task<bool> IsCodeUniqueAsync(string code, int? excludeId = null);
        Task<(bool Success, string Message, int? DepartmentId)> CreateDepartmentAsync(DepartmentFormViewModel model, string? userId, string? userEmail);
        Task<(bool Success, string Message)> UpdateDepartmentAsync(DepartmentFormViewModel model, string? userId, string? userEmail);
        Task<(bool Success, string Message)> DeleteDepartmentAsync(int departmentId, string? userId, string? userEmail);
    }
}

namespace College_Library_Management_System.Services
{
    public class DepartmentService : IDepartmentService
    {
        private readonly ApplicationDbContext _context;
        private readonly IAuditLogService _auditLogService;

        public DepartmentService(ApplicationDbContext context, IAuditLogService auditLogService)
        {
            _context = context;
            _auditLogService = auditLogService;
        }

        public async Task<List<DepartmentItemViewModel>> GetAllDepartmentsAsync()
        {
            return await _context.Departments
                .Select(d => new DepartmentItemViewModel
                {
                    DepartmentId = d.DepartmentId,
                    DepartmentName = d.DepartmentName,
                    DepartmentCode = d.DepartmentCode,
                    StudentCount = d.Students.Count
                })
                .OrderBy(d => d.DepartmentName)
                .ToListAsync();
        }

        public async Task<DepartmentFormViewModel?> GetDepartmentForEditAsync(int departmentId)
        {
            var dept = await _context.Departments.FindAsync(departmentId);
            if (dept == null) return null;

            return new DepartmentFormViewModel
            {
                DepartmentId = dept.DepartmentId,
                DepartmentName = dept.DepartmentName,
                DepartmentCode = dept.DepartmentCode
            };
        }

        public async Task<Department?> GetDepartmentByIdAsync(int departmentId)
        {
            return await _context.Departments
                .Include(d => d.Students)
                .FirstOrDefaultAsync(d => d.DepartmentId == departmentId);
        }

        public async Task<bool> IsCodeUniqueAsync(string code, int? excludeId = null)
        {
            var normalizedCode = code.Trim().ToUpperInvariant();
            var query = _context.Departments.Where(d => d.DepartmentCode.ToUpper() == normalizedCode);
            if (excludeId.HasValue)
            {
                query = query.Where(d => d.DepartmentId != excludeId.Value);
            }
            return !await query.AnyAsync();
        }

        public async Task<(bool Success, string Message, int? DepartmentId)> CreateDepartmentAsync(DepartmentFormViewModel model, string? userId, string? userEmail)
        {
            var trimmedCode = model.DepartmentCode.Trim().ToUpperInvariant();
            var trimmedName = model.DepartmentName.Trim();

            if (!await IsCodeUniqueAsync(trimmedCode))
            {
                return (false, $"Department code '{trimmedCode}' is already registered.", null);
            }

            var department = new Department
            {
                DepartmentName = trimmedName,
                DepartmentCode = trimmedCode
            };

            _context.Departments.Add(department);
            await _context.SaveChangesAsync();

            await _auditLogService.LogAsync(
                action: "CreateDepartment",
                entityType: nameof(Department),
                entityId: department.DepartmentId.ToString(),
                details: $"Created department '{department.DepartmentName}' with code '{department.DepartmentCode}'.",
                userId: userId,
                userEmail: userEmail
            );

            return (true, "Department created successfully.", department.DepartmentId);
        }

        public async Task<(bool Success, string Message)> UpdateDepartmentAsync(DepartmentFormViewModel model, string? userId, string? userEmail)
        {
            var department = await _context.Departments.FindAsync(model.DepartmentId);
            if (department == null)
            {
                return (false, "Department not found.");
            }

            var trimmedCode = model.DepartmentCode.Trim().ToUpperInvariant();
            var trimmedName = model.DepartmentName.Trim();

            if (!await IsCodeUniqueAsync(trimmedCode, model.DepartmentId))
            {
                return (false, $"Department code '{trimmedCode}' is already in use by another department.");
            }

            department.DepartmentName = trimmedName;
            department.DepartmentCode = trimmedCode;

            await _context.SaveChangesAsync();

            await _auditLogService.LogAsync(
                action: "UpdateDepartment",
                entityType: nameof(Department),
                entityId: department.DepartmentId.ToString(),
                details: $"Updated department '{department.DepartmentName}' (Code: '{department.DepartmentCode}').",
                userId: userId,
                userEmail: userEmail
            );

            return (true, "Department updated successfully.");
        }

        public async Task<(bool Success, string Message)> DeleteDepartmentAsync(int departmentId, string? userId, string? userEmail)
        {
            var department = await _context.Departments
                .Include(d => d.Students)
                .FirstOrDefaultAsync(d => d.DepartmentId == departmentId);

            if (department == null)
            {
                return (false, "Department not found.");
            }

            if (department.Students.Any())
            {
                return (false, $"Cannot delete department '{department.DepartmentName}' because {department.Students.Count} registered student(s) belong to it. Please reassign or deactivate the students first.");
            }

            _context.Departments.Remove(department);
            await _context.SaveChangesAsync();

            await _auditLogService.LogAsync(
                action: "DeleteDepartment",
                entityType: nameof(Department),
                entityId: departmentId.ToString(),
                details: $"Deleted department '{department.DepartmentName}' ({department.DepartmentCode}).",
                userId: userId,
                userEmail: userEmail
            );

            return (true, "Department deleted successfully.");
        }
    }
}
