using System.Net;
using System.Net.Mail;
using Microsoft.Extensions.Configuration;
using System.Threading.Tasks;
using System;

namespace UniClassReserve.Data
{
    public class EmailService : IEmailService
    {
        private readonly IConfiguration _config;
        private readonly ILogService _logService;
        public EmailService(IConfiguration config, ILogService logService)
        {
            _config = config;
            _logService = logService;
        }

        public async Task SendEmailAsync(string to, string subject, string body)
        {
            try
            {
                var smtpHost = _config["Smtp:Host"];
                var smtpPort = int.Parse(_config["Smtp:Port"] ?? "587");
                var smtpUser = _config["Smtp:Username"];
                var smtpPass = _config["Smtp:Password"];
                var from = _config["Smtp:From"];
                if (string.IsNullOrEmpty(from))
                    throw new InvalidOperationException("SMTP 'From' address is not configured.");

                using var client = new SmtpClient(smtpHost, smtpPort)
                {
                    Credentials = new NetworkCredential(smtpUser, smtpPass),
                    EnableSsl = true
                };
                var mail = new MailMessage(from, to, subject, body)
                {
                    IsBodyHtml = true
                };
                await client.SendMailAsync(mail);
            }
            catch (Exception ex)
            {
                // Hata loglama
                await _logService.LogAsync("SYSTEM", $"Email send failed: {ex.Message}", true, ex.ToString());
            }
        }
    }
} 