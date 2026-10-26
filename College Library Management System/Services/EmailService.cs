using System.Net;
using System.Net.Mail;
using College_Library_Management_System.Services.Interfaces;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace College_Library_Management_System.Services
{
    public class EmailService : IEmailService
    {
        private readonly ILogger<EmailService> _logger;
        private readonly IConfiguration _configuration;

        // In-memory record for dev testing/demonstration
        public static string? LastSentResetLink { get; set; }

        public EmailService(ILogger<EmailService> logger, IConfiguration configuration)
        {
            _logger = logger;
            _configuration = configuration;
        }

        public async Task SendEmailAsync(string toEmail, string subject, string htmlMessage)
        {
            _logger.LogInformation("Sending email to {ToEmail}. Subject: {Subject}", toEmail, subject);

            // Attempt SMTP if configured in appsettings
            var smtpHost = _configuration["Smtp:Host"];
            var smtpPortStr = _configuration["Smtp:Port"];
            var smtpUser = _configuration["Smtp:Username"];
            var smtpPass = _configuration["Smtp:Password"];
            var fromEmail = _configuration["Smtp:From"] ?? "library@collegelibrary.edu";

            if (!string.IsNullOrWhiteSpace(smtpHost) && int.TryParse(smtpPortStr, out int smtpPort))
            {
                try
                {
                    using var client = new SmtpClient(smtpHost, smtpPort)
                    {
                        EnableSsl = true,
                        Credentials = !string.IsNullOrWhiteSpace(smtpUser) ? new NetworkCredential(smtpUser, smtpPass) : null
                    };

                    using var mail = new MailMessage
                    {
                        From = new MailAddress(fromEmail, "College Library"),
                        Subject = subject,
                        Body = htmlMessage,
                        IsBodyHtml = true
                    };
                    mail.To.Add(toEmail);

                    await client.SendMailAsync(mail);
                    _logger.LogInformation("Email successfully dispatched via SMTP to {ToEmail}", toEmail);
                    return;
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, "Failed to send email via SMTP. Falling back to log-based delivery.");
                }
            }

            // Fallback / Development delivery: logs the email content and preserves last sent link for verification
            LastSentResetLink = htmlMessage;
            _logger.LogInformation("\n================== [OUTGOING EMAIL] ==================\nTO: {ToEmail}\nSUBJECT: {Subject}\nBODY:\n{Body}\n=======================================================", 
                toEmail, subject, htmlMessage);
        }
    }
}
