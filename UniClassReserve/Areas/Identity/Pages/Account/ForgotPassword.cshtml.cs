using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using System.ComponentModel.DataAnnotations;
using System.Threading.Tasks;
using UniClassReserve.Data;

namespace UniClassReserve.Areas.Identity.Pages.Account
{
    public class ForgotPasswordModel : PageModel
    {
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly IEmailService _emailService;
        public ForgotPasswordModel(UserManager<ApplicationUser> userManager, IEmailService emailService)
        {
            _userManager = userManager;
            _emailService = emailService;
        }

        [BindProperty]
        public InputModel Input { get; set; } = new();
        public string? ResultMessage { get; set; }

        public class InputModel
        {
            [Required]
            [EmailAddress]
            public string Email { get; set; } = string.Empty;
        }

        public void OnGet() { }

        public async Task<IActionResult> OnPostAsync()
        {
            if (!ModelState.IsValid)
                return Page();
            var user = await _userManager.FindByEmailAsync(Input.Email);
            if (user == null)
            {
                ResultMessage = "If the email exists, a reset link has been sent.";
                return Page();
            }
            var token = await _userManager.GeneratePasswordResetTokenAsync(user);
            var resetUrl = Url.Page(
                "/Account/ResetPassword",
                pageHandler: null,
                values: new { area = "Identity", code = token, email = Input.Email },
                protocol: Request.Scheme);
            var body = $"<p>Click <a href='{resetUrl}'>here</a> to reset your password.</p>";
            await _emailService.SendEmailAsync(Input.Email, "Password Reset", body);
            ResultMessage = "If the email exists, a reset link has been sent.";
            return Page();
        }
    }
} 