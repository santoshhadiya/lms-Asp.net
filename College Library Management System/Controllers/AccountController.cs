using College_Library_Management_System.Models;
using College_Library_Management_System.Services;
using College_Library_Management_System.Services.Interfaces;
using College_Library_Management_System.ViewModels.Account;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;

namespace College_Library_Management_System.Controllers
{
    public class AccountController : Controller
    {
        private readonly SignInManager<ApplicationUser> _signInManager;
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly IAuditLogService _auditLogService;
        private readonly IEmailService _emailService;

        public AccountController(
            SignInManager<ApplicationUser> signInManager,
            UserManager<ApplicationUser> userManager,
            IAuditLogService auditLogService,
            IEmailService emailService)
        {
            _signInManager = signInManager;
            _userManager = userManager;
            _auditLogService = auditLogService;
            _emailService = emailService;
        }

        [HttpGet]
        [AllowAnonymous]
        public async Task<IActionResult> Login(string? returnUrl = null)
        {
            if (User.Identity?.IsAuthenticated == true)
            {
                var currentUser = await _userManager.GetUserAsync(User);
                if (currentUser != null && await _userManager.IsInRoleAsync(currentUser, "Student"))
                {
                    return RedirectToAction("Index", "StudentDashboard");
                }
                return RedirectToAction("Index", "Home");
            }

            ViewData["ReturnUrl"] = returnUrl;
            return View(new LoginViewModel { ReturnUrl = returnUrl });
        }

        [HttpPost]
        [AllowAnonymous]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Login(LoginViewModel model)
        {
            ViewData["ReturnUrl"] = model.ReturnUrl;

            if (!ModelState.IsValid)
            {
                return View(model);
            }

            var identifier = model.Email.Trim();
            var user = await _userManager.FindByEmailAsync(identifier) 
                       ?? await _userManager.FindByNameAsync(identifier);

            if (user == null)
            {
                ModelState.AddModelError(string.Empty, "Invalid login credentials.");
                return View(model);
            }

            if (!user.IsActive)
            {
                ModelState.AddModelError(string.Empty, "This user account has been deactivated. Please contact the administrator.");
                return View(model);
            }

            var result = await _signInManager.PasswordSignInAsync(user, model.Password, model.RememberMe, lockoutOnFailure: true);

            if (result.Succeeded)
            {
                await _auditLogService.LogAsync(
                    action: "UserLogin",
                    entityType: nameof(ApplicationUser),
                    entityId: user.Id,
                    details: $"User '{user.Email}' logged in successfully.",
                    userId: user.Id,
                    userEmail: user.Email,
                    ipAddress: HttpContext.Connection.RemoteIpAddress?.ToString()
                );

                if (!string.IsNullOrEmpty(model.ReturnUrl) && Url.IsLocalUrl(model.ReturnUrl))
                {
                    return Redirect(model.ReturnUrl);
                }

                if (await _userManager.IsInRoleAsync(user, "Student"))
                {
                    return RedirectToAction("Index", "StudentDashboard");
                }

                return RedirectToAction("Index", "Home");
            }

            if (result.IsLockedOut)
            {
                ModelState.AddModelError(string.Empty, "Account locked due to multiple failed login attempts. Please try again later.");
                return View(model);
            }

            ModelState.AddModelError(string.Empty, "Invalid login credentials.");
            return View(model);
        }

        [HttpGet]
        [AllowAnonymous]
        public IActionResult ForgotPassword()
        {
            return View();
        }

        [HttpPost]
        [AllowAnonymous]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ForgotPassword(ForgotPasswordViewModel model)
        {
            if (!ModelState.IsValid)
            {
                return View(model);
            }

            var user = await _userManager.FindByEmailAsync(model.Email.Trim());
            if (user != null && user.IsActive)
            {
                var token = await _userManager.GeneratePasswordResetTokenAsync(user);
                var callbackUrl = Url.Action("ResetPassword", "Account", new { token, email = user.Email }, protocol: Request.Scheme);

                EmailService.LastSentResetLink = callbackUrl;
                TempData["DevResetLink"] = callbackUrl;

                var emailBody = $@"
                    <div style='font-family: Arial, sans-serif; max-width: 600px; margin: 0 auto; padding: 20px; border: 1px solid #e2e8f0; border-radius: 8px;'>
                        <h2 style='color: #1e3a8a;'>Password Reset Request</h2>
                        <p>Hello <strong>{user.FullName}</strong>,</p>
                        <p>We received a request to reset the password for your College Library Management account.</p>
                        <p style='margin: 25px 0;'>
                            <a href='{callbackUrl}' style='background-color: #1e3a8a; color: #ffffff; padding: 12px 24px; text-decoration: none; border-radius: 6px; font-weight: bold; display: inline-block;'>
                                Reset Password
                            </a>
                        </p>
                        <p style='color: #64748b; font-size: 0.9rem;'>Or copy and paste this link in your browser:</p>
                        <p style='background-color: #f1f5f9; padding: 10px; border-radius: 4px; word-break: break-all; font-size: 0.85rem; color: #334155;'>{callbackUrl}</p>
                        <p style='color: #94a3b8; font-size: 0.8rem; margin-top: 30px;'>This link is valid for a limited time. If you did not request this, you can safely ignore this email.</p>
                    </div>";

                await _emailService.SendEmailAsync(user.Email!, "Reset Your Library Account Password", emailBody);

                await _auditLogService.LogAsync(
                    action: "ForgotPasswordRequested",
                    entityType: nameof(ApplicationUser),
                    entityId: user.Id,
                    details: $"Password reset token generated and sent to '{user.Email}'.",
                    userId: user.Id,
                    userEmail: user.Email
                );
            }

            return RedirectToAction(nameof(ForgotPasswordConfirmation));
        }

        [HttpGet]
        [AllowAnonymous]
        public IActionResult ForgotPasswordConfirmation()
        {
            return View();
        }

        [HttpGet]
        [AllowAnonymous]
        public IActionResult ResetPassword(string? token = null, string? email = null)
        {
            if (string.IsNullOrEmpty(token))
            {
                TempData["Error"] = "A valid password reset token is required.";
                return RedirectToAction(nameof(Login));
            }

            var model = new ResetPasswordViewModel
            {
                Token = token,
                Email = email ?? string.Empty
            };

            return View(model);
        }

        [HttpPost]
        [AllowAnonymous]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ResetPassword(ResetPasswordViewModel model)
        {
            if (!ModelState.IsValid)
            {
                return View(model);
            }

            var user = await _userManager.FindByEmailAsync(model.Email.Trim());
            if (user == null)
            {
                // Don't reveal that the user does not exist
                return RedirectToAction(nameof(ResetPasswordConfirmation));
            }

            var result = await _userManager.ResetPasswordAsync(user, model.Token, model.NewPassword);
            if (result.Succeeded)
            {
                await _userManager.UpdateSecurityStampAsync(user);

                await _auditLogService.LogAsync(
                    action: "PasswordResetSuccess",
                    entityType: nameof(ApplicationUser),
                    entityId: user.Id,
                    details: $"Password successfully reset for '{user.Email}'.",
                    userId: user.Id,
                    userEmail: user.Email
                );

                return RedirectToAction(nameof(ResetPasswordConfirmation));
            }

            foreach (var error in result.Errors)
            {
                ModelState.AddModelError(string.Empty, error.Description);
            }

            return View(model);
        }

        [HttpGet]
        [AllowAnonymous]
        public IActionResult ResetPasswordConfirmation()
        {
            return View();
        }

        [HttpGet]
        [Authorize]
        public IActionResult ChangePassword()
        {
            return View(new ChangePasswordViewModel());
        }

        [HttpPost]
        [Authorize]
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
                return RedirectToAction(nameof(Login));
            }

            var result = await _userManager.ChangePasswordAsync(user, model.CurrentPassword, model.NewPassword);
            if (result.Succeeded)
            {
                await _signInManager.RefreshSignInAsync(user);

                await _auditLogService.LogAsync(
                    action: "PasswordChanged",
                    entityType: nameof(ApplicationUser),
                    entityId: user.Id,
                    details: $"User '{user.Email}' changed their password successfully.",
                    userId: user.Id,
                    userEmail: user.Email
                );

                TempData["Success"] = "Your password has been changed successfully.";

                if (await _userManager.IsInRoleAsync(user, "Student"))
                {
                    return RedirectToAction("Index", "StudentDashboard");
                }

                return RedirectToAction("Index", "Home");
            }

            foreach (var error in result.Errors)
            {
                ModelState.AddModelError(string.Empty, error.Description);
            }

            return View(model);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize]
        public async Task<IActionResult> Logout()
        {
            var user = await _userManager.GetUserAsync(User);
            if (user != null)
            {
                await _auditLogService.LogAsync(
                    action: "UserLogout",
                    entityType: nameof(ApplicationUser),
                    entityId: user.Id,
                    details: $"User '{user.Email}' logged out.",
                    userId: user.Id,
                    userEmail: user.Email,
                    ipAddress: HttpContext.Connection.RemoteIpAddress?.ToString()
                );
            }

            await _signInManager.SignOutAsync();
            return RedirectToAction("Login", "Account");
        }

        [HttpGet]
        [AllowAnonymous]
        public IActionResult AccessDenied()
        {
            return View();
        }
    }
}
