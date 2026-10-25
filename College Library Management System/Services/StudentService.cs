using College_Library_Management_System.Data;
using College_Library_Management_System.Models;
using College_Library_Management_System.Models.Enums;
using College_Library_Management_System.Services.Common;
using College_Library_Management_System.Services.Interfaces;
using College_Library_Management_System.ViewModels.Common;
using College_Library_Management_System.ViewModels.Students;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;

namespace College_Library_Management_System.Services.Interfaces
{
    public interface IStudentService
    {
        Task<PaginatedList<StudentListItemViewModel>> GetStudentsAsync(StudentListFilterViewModel filter);
        Task<StudentFormViewModel?> GetStudentForEditAsync(int studentId);
        Task<StudentDetailsViewModel?> GetStudentDetailsAsync(int studentId);
        Task<bool> IsEnrollmentNumberUniqueAsync(string enrollmentNumber, int? excludeId = null);
        Task<List<SelectListItem>> GetDepartmentOptionsAsync();
        Task<(bool Success, string Message, int? StudentId)> CreateStudentAsync(StudentFormViewModel model, string? userId, string? userEmail);
        Task<(bool Success, string Message)> UpdateStudentAsync(StudentFormViewModel model, string? userId, string? userEmail);
        Task<(bool Success, string Message)> ToggleStudentStatusAsync(int studentId, string? userId, string? userEmail);
    }
}

namespace College_Library_Management_System.Services
{
    public class StudentService : IStudentService
    {
        private readonly ApplicationDbContext _context;
        private readonly IDateTimeProvider _dateTimeProvider;
        private readonly IAuditLogService _auditLogService;

        public StudentService(ApplicationDbContext context, IDateTimeProvider dateTimeProvider, IAuditLogService auditLogService)
        {
            _context = context;
            _dateTimeProvider = dateTimeProvider;
            _auditLogService = auditLogService;
        }

        public async Task<PaginatedList<StudentListItemViewModel>> GetStudentsAsync(StudentListFilterViewModel filter)
        {
            var query = _context.Students
                .Include(s => s.Department)
                .Include(s => s.Loans)
                    .ThenInclude(l => l.Fine)
                .AsNoTracking()
                .AsQueryable();

            if (!string.IsNullOrWhiteSpace(filter.SearchTerm))
            {
                var term = filter.SearchTerm.Trim();
                query = query.Where(s =>
                    s.EnrollmentNumber.Contains(term) ||
                    s.FullName.Contains(term) ||
                    (s.Email != null && s.Email.Contains(term)) ||
                    (s.Phone != null && s.Phone.Contains(term)));
            }

            if (filter.DepartmentId.HasValue && filter.DepartmentId > 0)
            {
                query = query.Where(s => s.DepartmentId == filter.DepartmentId.Value);
            }

            if (filter.IsActive.HasValue)
            {
                query = query.Where(s => s.IsActive == filter.IsActive.Value);
            }

            var totalCount = await query.CountAsync();

            var pageNumber = Math.Max(1, filter.PageNumber);
            var pageSize = filter.PageSize > 0 ? filter.PageSize : 10;

            var items = await query
                .OrderBy(s => s.FullName)
                .Skip((pageNumber - 1) * pageSize)
                .Take(pageSize)
                .Select(s => new StudentListItemViewModel
                {
                    StudentId = s.StudentId,
                    EnrollmentNumber = s.EnrollmentNumber,
                    FullName = s.FullName,
                    Email = s.Email,
                    Phone = s.Phone,
                    DepartmentName = s.Department.DepartmentName,
                    DepartmentCode = s.Department.DepartmentCode,
                    Semester = s.Semester,
                    IsActive = s.IsActive,
                    ActiveLoansCount = s.Loans.Count(l => l.Status == LoanStatus.Active),
                    OutstandingFinesTotal = s.Loans
                        .Where(l => l.Fine != null)
                        .Select(l => l.Fine!)
                        .Where(f => f.Status != FineStatus.Paid && f.Status != FineStatus.Waived)
                        .Select(f => f.FineAmount - f.AmountPaid - f.WaivedAmount)
                        .DefaultIfEmpty(0m)
                        .Sum()
                })
                .ToListAsync();

            return new PaginatedList<StudentListItemViewModel>(items, totalCount, pageNumber, pageSize);
        }

        public async Task<StudentFormViewModel?> GetStudentForEditAsync(int studentId)
        {
            var student = await _context.Students.FindAsync(studentId);
            if (student == null) return null;

            return new StudentFormViewModel
            {
                StudentId = student.StudentId,
                EnrollmentNumber = student.EnrollmentNumber,
                FullName = student.FullName,
                Email = student.Email,
                Phone = student.Phone,
                DepartmentId = student.DepartmentId,
                Semester = student.Semester,
                IsActive = student.IsActive,
                DepartmentOptions = await GetDepartmentOptionsAsync()
            };
        }

        public async Task<StudentDetailsViewModel?> GetStudentDetailsAsync(int studentId)
        {
            var student = await _context.Students
                .Include(s => s.Department)
                .Include(s => s.Loans)
                    .ThenInclude(l => l.BookCopy)
                        .ThenInclude(c => c.Book)
                .Include(s => s.Loans)
                    .ThenInclude(l => l.Fine)
                .FirstOrDefaultAsync(s => s.StudentId == studentId);

            if (student == null) return null;

            var localNow = _dateTimeProvider.Now;
            var activeLoans = student.Loans.Where(l => l.Status == LoanStatus.Active).ToList();
            var hasOverdue = activeLoans.Any(l => l.DueAt.Date < localNow.Date);

            var outstandingFines = student.Loans
                .Where(l => l.Fine != null)
                .Select(l => l.Fine!)
                .Where(f => f.Status != FineStatus.Paid && f.Status != FineStatus.Waived)
                .Sum(f => Math.Max(0m, f.FineAmount - f.AmountPaid - f.WaivedAmount));

            var loanHistory = student.Loans
                .OrderByDescending(l => l.IssuedAt)
                .Select(l => new StudentLoanHistoryItemViewModel
                {
                    LoanId = l.LoanId,
                    BookTitle = l.BookCopy.Book.Title,
                    AccessionNumber = l.BookCopy.AccessionNumber,
                    IssuedAt = _dateTimeProvider.ToLocalDateTime(l.IssuedAt),
                    DueAt = _dateTimeProvider.ToLocalDateTime(l.DueAt),
                    ReturnedAt = l.ReturnedAt.HasValue ? _dateTimeProvider.ToLocalDateTime(l.ReturnedAt.Value) : null,
                    Status = l.Status.ToString(),
                    RenewalCount = l.RenewalCount,
                    FineAmount = l.Fine?.FineAmount,
                    FineStatus = l.Fine?.Status.ToString()
                })
                .ToList();

            return new StudentDetailsViewModel
            {
                StudentId = student.StudentId,
                EnrollmentNumber = student.EnrollmentNumber,
                FullName = student.FullName,
                Email = student.Email,
                Phone = student.Phone,
                DepartmentName = student.Department.DepartmentName,
                Semester = student.Semester,
                IsActive = student.IsActive,
                CreatedAt = _dateTimeProvider.ToLocalDateTime(student.CreatedAt),
                ActiveLoansCount = activeLoans.Count,
                OutstandingFinesTotal = outstandingFines,
                HasOverdueLoans = hasOverdue,
                LoanHistory = loanHistory
            };
        }

        public async Task<bool> IsEnrollmentNumberUniqueAsync(string enrollmentNumber, int? excludeId = null)
        {
            var normalized = enrollmentNumber.Trim().ToUpperInvariant();
            var query = _context.Students.Where(s => s.EnrollmentNumber.ToUpper() == normalized);
            if (excludeId.HasValue)
            {
                query = query.Where(s => s.StudentId != excludeId.Value);
            }
            return !await query.AnyAsync();
        }

        public async Task<List<SelectListItem>> GetDepartmentOptionsAsync()
        {
            return await _context.Departments
                .OrderBy(d => d.DepartmentName)
                .Select(d => new SelectListItem
                {
                    Value = d.DepartmentId.ToString(),
                    Text = $"{d.DepartmentName} ({d.DepartmentCode})"
                })
                .ToListAsync();
        }

        public async Task<(bool Success, string Message, int? StudentId)> CreateStudentAsync(StudentFormViewModel model, string? userId, string? userEmail)
        {
            var trimmedEnrollment = model.EnrollmentNumber.Trim().ToUpperInvariant();

            if (!await IsEnrollmentNumberUniqueAsync(trimmedEnrollment))
            {
                return (false, $"Enrollment Number '{trimmedEnrollment}' is already registered to another student.", null);
            }

            var departmentExists = await _context.Departments.AnyAsync(d => d.DepartmentId == model.DepartmentId);
            if (!departmentExists)
            {
                return (false, "The selected department does not exist.", null);
            }

            var student = new Student
            {
                EnrollmentNumber = trimmedEnrollment,
                FullName = model.FullName.Trim(),
                Email = string.IsNullOrWhiteSpace(model.Email) ? null : model.Email.Trim().ToLowerInvariant(),
                Phone = string.IsNullOrWhiteSpace(model.Phone) ? null : model.Phone.Trim(),
                DepartmentId = model.DepartmentId,
                Semester = model.Semester,
                IsActive = model.IsActive,
                CreatedAt = _dateTimeProvider.UtcNow
            };

            _context.Students.Add(student);
            await _context.SaveChangesAsync();

            await _auditLogService.LogAsync(
                action: "CreateStudent",
                entityType: nameof(Student),
                entityId: student.StudentId.ToString(),
                details: $"Registered student '{student.FullName}' (Enrollment: {student.EnrollmentNumber}).",
                userId: userId,
                userEmail: userEmail
            );

            return (true, "Student registered successfully.", student.StudentId);
        }

        public async Task<(bool Success, string Message)> UpdateStudentAsync(StudentFormViewModel model, string? userId, string? userEmail)
        {
            var student = await _context.Students.FindAsync(model.StudentId);
            if (student == null)
            {
                return (false, "Student record not found.");
            }

            var trimmedEnrollment = model.EnrollmentNumber.Trim().ToUpperInvariant();

            if (!await IsEnrollmentNumberUniqueAsync(trimmedEnrollment, model.StudentId))
            {
                return (false, $"Enrollment Number '{trimmedEnrollment}' is already registered to another student.");
            }

            var departmentExists = await _context.Departments.AnyAsync(d => d.DepartmentId == model.DepartmentId);
            if (!departmentExists)
            {
                return (false, "The selected department does not exist.");
            }

            student.EnrollmentNumber = trimmedEnrollment;
            student.FullName = model.FullName.Trim();
            student.Email = string.IsNullOrWhiteSpace(model.Email) ? null : model.Email.Trim().ToLowerInvariant();
            student.Phone = string.IsNullOrWhiteSpace(model.Phone) ? null : model.Phone.Trim();
            student.DepartmentId = model.DepartmentId;
            student.Semester = model.Semester;
            student.IsActive = model.IsActive;

            await _context.SaveChangesAsync();

            await _auditLogService.LogAsync(
                action: "UpdateStudent",
                entityType: nameof(Student),
                entityId: student.StudentId.ToString(),
                details: $"Updated student '{student.FullName}' (Enrollment: {student.EnrollmentNumber}).",
                userId: userId,
                userEmail: userEmail
            );

            return (true, "Student updated successfully.");
        }

        public async Task<(bool Success, string Message)> ToggleStudentStatusAsync(int studentId, string? userId, string? userEmail)
        {
            var student = await _context.Students.FindAsync(studentId);
            if (student == null)
            {
                return (false, "Student record not found.");
            }

            student.IsActive = !student.IsActive;
            await _context.SaveChangesAsync();

            var statusStr = student.IsActive ? "Activated" : "Deactivated";
            await _auditLogService.LogAsync(
                action: "ToggleStudentStatus",
                entityType: nameof(Student),
                entityId: student.StudentId.ToString(),
                details: $"{statusStr} student '{student.FullName}' (Enrollment: {student.EnrollmentNumber}).",
                userId: userId,
                userEmail: userEmail
            );

            return (true, $"Student status changed to {(student.IsActive ? "Active" : "Inactive")}.");
        }
    }
}
