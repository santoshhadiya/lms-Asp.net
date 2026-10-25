using College_Library_Management_System.Data;
using College_Library_Management_System.Models;
using College_Library_Management_System.Services.Common;
using Microsoft.EntityFrameworkCore;

namespace College_Library_Management_System.Services.Interfaces
{
    public interface IAuditLogService
    {
        Task LogAsync(string action, string entityType, string? entityId, string details, string? userId = null, string? userEmail = null, string? ipAddress = null);
        Task<List<AuditLog>> GetRecentLogsAsync(int count = 50);
    }
}

namespace College_Library_Management_System.Services
{
    public class AuditLogService : Interfaces.IAuditLogService
    {
        private readonly ApplicationDbContext _context;
        private readonly IDateTimeProvider _dateTimeProvider;

        public AuditLogService(ApplicationDbContext context, IDateTimeProvider dateTimeProvider)
        {
            _context = context;
            _dateTimeProvider = dateTimeProvider;
        }

        public async Task LogAsync(string action, string entityType, string? entityId, string details, string? userId = null, string? userEmail = null, string? ipAddress = null)
        {
            var auditLog = new AuditLog
            {
                Action = action,
                EntityType = entityType,
                EntityId = entityId,
                Details = details,
                UserId = userId,
                UserEmail = userEmail,
                IpAddress = ipAddress,
                Timestamp = _dateTimeProvider.UtcNow
            };

            _context.AuditLogs.Add(auditLog);
            await _context.SaveChangesAsync();
        }

        public async Task<List<AuditLog>> GetRecentLogsAsync(int count = 50)
        {
            return await _context.AuditLogs
                .OrderByDescending(a => a.Timestamp)
                .Take(count)
                .ToListAsync();
        }
    }
}
