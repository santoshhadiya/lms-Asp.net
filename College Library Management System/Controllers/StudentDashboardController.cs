using System.Security.Claims;
using College_Library_Management_System.Models;
using College_Library_Management_System.Services.Interfaces;
using College_Library_Management_System.ViewModels.Account;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;

namespace College_Library_Management_System.Controllers
{
    [Authorize(Roles = "Student")]
    public class StudentDashboardController : Controller
    {
        private readonly IStudentDashboardService _studentDashboardService;
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly SignInManager<ApplicationUser> _signInManager;
        private readonly IAuditLogService _auditLogService;

        public StudentDashboardController(
            IStudentDashboardService studentDashboardService,
            UserManager<ApplicationUser> userManager,
            SignInManager<ApplicationUser> signInManager,
            IAuditLogService auditLogService)
        {
            _studentDashboardService = studentDashboardService;
            _userManager = userManager;
            _signInManager = signInManager;
            _auditLogService = auditLogService;
        }

        [HttpGet]
        public async Task<IActionResult> Index()
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            var userEmail = User.FindFirstValue(ClaimTypes.Email);

            if (string.IsNullOrEmpty(userId))
            {
                return RedirectToAction("Login", "Account");
            }

            var dashboard = await _studentDashboardService.GetStudentDashboardDataAsync(userId, userEmail);
            if (dashboard == null)
            {
                TempData["Error"] = "Student profile not found. Please contact the library administrator.";
                return View("NoProfile");
            }

            return View(dashboard);
        }

        [HttpGet]
        public async Task<IActionResult> Loans()
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            var userEmail = User.FindFirstValue(ClaimTypes.Email);

            if (string.IsNullOrEmpty(userId))
            {
                return RedirectToAction("Login", "Account");
            }

            var dashboard = await _studentDashboardService.GetStudentDashboardDataAsync(userId, userEmail);
            if (dashboard == null)
            {
                return View("NoProfile");
            }

            return View(dashboard);
        }

        [HttpGet]
        public async Task<IActionResult> Fines()
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            var userEmail = User.FindFirstValue(ClaimTypes.Email);

            if (string.IsNullOrEmpty(userId))
            {
                return RedirectToAction("Login", "Account");
            }

            var dashboard = await _studentDashboardService.GetStudentDashboardDataAsync(userId, userEmail);
            if (dashboard == null)
            {
                return View("NoProfile");
            }

            return View(dashboard);
        }

        [HttpGet]
        public IActionResult ChangePassword()
        {
            return View(new ChangePasswordViewModel());
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ChangePassword(ChangePasswordViewModel model)
        {
            if (!ModelState.IsValid)
            {
                return View(model);
            }

            var user = await _userManager.GetUserAsync(User);
            if (user == null)
            {
                return RedirectToAction("Login", "Account");
            }

            var result = await _userManager.ChangePasswordAsync(user, model.CurrentPassword, model.NewPassword);
            if (result.Succeeded)
            {
                await _signInManager.RefreshSignInAsync(user);

                await _auditLogService.LogAsync(
                    action: "StudentChangedPassword",
                    entityType: nameof(ApplicationUser),
                    entityId: user.Id,
                    details: $"Student '{user.Email}' changed their password successfully.",
                    userId: user.Id,
                    userEmail: user.Email
                );

                TempData["Success"] = "Your password has been changed successfully.";
                return RedirectToAction(nameof(Index));
            }

            foreach (var error in result.Errors)
            {
                ModelState.AddModelError(string.Empty, error.Description);
            }

            return View(model);
        }
    }
}
