using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using System.Security.Claims;
using UniClassReserve.Data;
using Microsoft.Extensions.Configuration;
using Microsoft.AspNetCore.Identity;

namespace UniClassReserve.Pages
{
    [Authorize(Roles = "Instructor")]
    public class ContactUsModel : PageModel
    {
        private readonly IEmailService _emailService;
        private readonly ILogService _logService;
        private readonly IConfiguration _config;
        private readonly UserManager<UniClassReserve.Data.ApplicationUser> _userManager;
        public ContactUsModel(IEmailService emailService, ILogService logService, IConfiguration config, UserManager<UniClassReserve.Data.ApplicationUser> userManager)
        {
            _emailService = emailService;
            _logService = logService;
            _config = config;
            _userManager = userManager;
        }
        [BindProperty]
        public string Subject { get; set; } = string.Empty;
        [BindProperty]
        public string Message { get; set; } = string.Empty;
        public string? ResultMessage { get; set; }
        public void OnGet() { }
        public async Task<IActionResult> OnPostAsync()
        {
            if (string.IsNullOrWhiteSpace(Subject) || string.IsNullOrWhiteSpace(Message))
            {
                ResultMessage = "Please fill in all fields.";
                return Page();
            }
            var userEmail = User.FindFirstValue(ClaimTypes.Email) ?? User.Identity?.Name ?? "Unknown";
            var body = $"<b>From:</b> {userEmail}<br/><b>Subject:</b> {Subject}<br/><b>Message:</b><br/>{Message}";
            var admins = await _userManager.GetUsersInRoleAsync("Admin");
            var adminUser = admins.FirstOrDefault(u => !u.IsDeleted && !string.IsNullOrEmpty(u.Email));
            var adminEmail = adminUser?.Email ?? _config["AdminEmail"] ?? "admin@admin.com";
            await _emailService.SendEmailAsync(adminEmail, $"Contact Us: {Subject}", body);
            await _logService.LogAsync(userEmail, "Contact Us message sent", false, $"Subject={Subject}");
            ResultMessage = "Your message has been sent to the admin.";
            return Page();
        }
    }
} 