using System.Security.Cryptography;
using College_Library_Management_System.Data;
using College_Library_Management_System.Models;
using College_Library_Management_System.Services.Common;
using College_Library_Management_System.Services.Interfaces;
using College_Library_Management_System.ViewModels.Librarians;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

namespace College_Library_Management_System.Services
{
    public class LibrarianService : ILibrarianService
    {
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly ApplicationDbContext _context;
        private readonly IDateTimeProvider _dateTimeProvider;
        private readonly IAuditLogService _auditLogService;

        public LibrarianService(
            UserManager<ApplicationUser> userManager,
            ApplicationDbContext context,
            IDateTimeProvider dateTimeProvider,
            IAuditLogService auditLogService)
        {
            _userManager = userManager;
            _context = context;
            _dateTimeProvider = dateTimeProvider;
            _auditLogService = auditLogService;
        }

        public async Task<List<LibrarianListItemViewModel>> GetLibrariansAsync(string? searchTerm = null, bool? isActive = null)
        {
            var librarians = await _userManager.GetUsersInRoleAsync(DbInitializer.LibrarianRole);
            var query = librarians.AsQueryable();

            if (!string.IsNullOrWhiteSpace(searchTerm))
            {
                var term = searchTerm.Trim().ToLowerInvariant();
                query = query.Where(u =>
                    u.FullName.ToLowerInvariant().Contains(term) ||
                    (u.Email != null && u.Email.ToLowerInvariant().Contains(term)) ||
                    (u.PhoneNumber != null && u.PhoneNumber.Contains(term)));
            }

            if (isActive.HasValue)
            {
                query = query.Where(u => u.IsActive == isActive.Value);
            }

            var list = query
                .OrderBy(u => u.FullName)
                .Select(u => new LibrarianListItemViewModel
                {
                    Id = u.Id,
                    FullName = u.FullName,
                    Email = u.Email ?? string.Empty,
                    PhoneNumber = u.PhoneNumber,
                    IsActive = u.IsActive,
                    CreatedAt = _dateTimeProvider.ToLocalDateTime(u.CreatedAt),
                    IssuedLoansCount = _context.Loans.Count(l => l.IssuedByUserId == u.Id),
                    ReceivedLoansCount = _context.Loans.Count(l => l.ReceivedByUserId == u.Id)
                })
                .ToList();

            return list;
        }

        public async Task<LibrarianFormViewModel?> GetLibrarianForEditAsync(string id)
        {
            var user = await _userManager.FindByIdAsync(id);
            if (user == null || !await _userManager.IsInRoleAsync(user, DbInitializer.LibrarianRole))
            {
                return null;
            }

            return new LibrarianFormViewModel
            {
                Id = user.Id,
                FullName = user.FullName,
                Email = user.Email ?? string.Empty,
                PhoneNumber = user.PhoneNumber,
                IsActive = user.IsActive
            };
        }

        public async Task<(bool Success, string Message, string? GeneratedPassword)> CreateLibrarianAsync(
            LibrarianFormViewModel model, string adminUserId, string adminEmail)
        {
            var email = model.Email.Trim().ToLowerInvariant();
            var existing = await _userManager.FindByEmailAsync(email);
            if (existing != null)
            {
                return (false, $"An account with email '{email}' already exists.", null);
            }

            string tempPassword = !string.IsNullOrWhiteSpace(model.TemporaryPassword)
                ? model.TemporaryPassword.Trim()
                : GenerateSecurePassword();

            var librarian = new ApplicationUser
            {
                UserName = email,
                Email = email,
                FullName = model.FullName.Trim(),
                PhoneNumber = string.IsNullOrWhiteSpace(model.PhoneNumber) ? null : model.PhoneNumber.Trim(),
                EmailConfirmed = true,
                IsActive = model.IsActive,
                CreatedAt = _dateTimeProvider.UtcNow
            };

            var createResult = await _userManager.CreateAsync(librarian, tempPassword);
            if (!createResult.Succeeded)
            {
                var errors = string.Join(", ", createResult.Errors.Select(e => e.Description));
                return (false, $"Failed to create librarian: {errors}", null);
            }

            await _userManager.AddToRoleAsync(librarian, DbInitializer.LibrarianRole);

            await _auditLogService.LogAsync(
                action: "CreateLibrarian",
                entityType: nameof(ApplicationUser),
                entityId: librarian.Id,
                details: $"Admin registered new Librarian '{librarian.FullName}' ({librarian.Email}).",
                userId: adminUserId,
                userEmail: adminEmail
            );

            return (true, $"Librarian account created successfully for '{librarian.FullName}'. Temporary Password: '{tempPassword}'", tempPassword);
        }

        public async Task<(bool Success, string Message)> UpdateLibrarianAsync(
            LibrarianFormViewModel model, string adminUserId, string adminEmail)
        {
            if (string.IsNullOrEmpty(model.Id))
            {
                return (false, "Librarian ID is required.");
            }

            var user = await _userManager.FindByIdAsync(model.Id);
            if (user == null || !await _userManager.IsInRoleAsync(user, DbInitializer.LibrarianRole))
            {
                return (false, "Librarian account not found.");
            }

            user.FullName = model.FullName.Trim();
            user.PhoneNumber = string.IsNullOrWhiteSpace(model.PhoneNumber) ? null : model.PhoneNumber.Trim();
            user.IsActive = model.IsActive;

            var result = await _userManager.UpdateAsync(user);
            if (!result.Succeeded)
            {
                var errors = string.Join(", ", result.Errors.Select(e => e.Description));
                return (false, $"Update failed: {errors}");
            }

            await _auditLogService.LogAsync(
                action: "UpdateLibrarian",
                entityType: nameof(ApplicationUser),
                entityId: user.Id,
                details: $"Admin updated details for Librarian '{user.FullName}' ({user.Email}).",
                userId: adminUserId,
                userEmail: adminEmail
            );

            return (true, "Librarian details updated successfully.");
        }

        public async Task<(bool Success, string Message)> ToggleLibrarianStatusAsync(
            string id, string currentAdminId, string adminEmail)
        {
            var user = await _userManager.FindByIdAsync(id);
            if (user == null || !await _userManager.IsInRoleAsync(user, DbInitializer.LibrarianRole))
            {
                return (false, "Librarian account not found.");
            }

            if (user.Id == currentAdminId)
            {
                return (false, "You cannot deactivate your own account.");
            }

            user.IsActive = !user.IsActive;
            await _userManager.UpdateAsync(user);
            await _userManager.UpdateSecurityStampAsync(user);

            var statusStr = user.IsActive ? "Activated" : "Deactivated";
            await _auditLogService.LogAsync(
                action: "ToggleLibrarianStatus",
                entityType: nameof(ApplicationUser),
                entityId: user.Id,
                details: $"Admin {statusStr} Librarian account '{user.FullName}' ({user.Email}).",
                userId: currentAdminId,
                userEmail: adminEmail
            );

            return (true, $"Librarian account '{user.FullName}' has been {statusStr.ToLowerInvariant()}.");
        }

        public async Task<(bool Success, string Message, string? NewPassword)> ResetLibrarianPasswordAsync(
            string id, string? newPassword, string currentAdminId, string adminEmail)
        {
            var user = await _userManager.FindByIdAsync(id);
            if (user == null || !await _userManager.IsInRoleAsync(user, DbInitializer.LibrarianRole))
            {
                return (false, "Librarian account not found.", null);
            }

            string passwordToSet = !string.IsNullOrWhiteSpace(newPassword)
                ? newPassword.Trim()
                : GenerateSecurePassword();

            var token = await _userManager.GeneratePasswordResetTokenAsync(user);
            var result = await _userManager.ResetPasswordAsync(user, token, passwordToSet);

            if (!result.Succeeded)
            {
                var errors = string.Join(", ", result.Errors.Select(e => e.Description));
                return (false, $"Password reset failed: {errors}", null);
            }

            await _userManager.UpdateSecurityStampAsync(user);

            await _auditLogService.LogAsync(
                action: "ResetLibrarianPassword",
                entityType: nameof(ApplicationUser),
                entityId: user.Id,
                details: $"Admin reset password for Librarian '{user.FullName}' ({user.Email}).",
                userId: currentAdminId,
                userEmail: adminEmail
            );

            return (true, $"Password reset successfully for '{user.FullName}'. New Temporary Password: '{passwordToSet}'", passwordToSet);
        }

        private static string GenerateSecurePassword()
        {
            const string upper = "ABCDEFGHJKLMNPQRSTUVWXYZ";
            const string lower = "abcdefghijkmnpqrstuvwxyz";
            const string digits = "23456789";
            const string special = "!@#$%*";

            var random = RandomNumberGenerator.Create();
            var bytes = new byte[8];
            random.GetBytes(bytes);

            return $"Lib@{upper[bytes[0] % upper.Length]}{lower[bytes[1] % lower.Length]}{digits[bytes[2] % digits.Length]}{special[bytes[3] % special.Length]}{digits[bytes[4] % digits.Length]}9";
        }
    }
}
