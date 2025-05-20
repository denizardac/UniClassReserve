using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using System.ComponentModel.DataAnnotations;
using System.Threading.Tasks;
using UniClassReserve.Data;

namespace UniClassReserve.Areas.Identity.Pages.Account
{
    public class ResetPasswordModel : PageModel
    {
        private readonly UserManager<ApplicationUser> _userManager;
        public ResetPasswordModel(UserManager<ApplicationUser> userManager)
        {
            _userManager = userManager;
        }

        [BindProperty]
        public InputModel Input { get; set; } = new();
        public string? ResultMessage { get; set; }

        public class InputModel
        {
            [Required]
            public string Email { get; set; } = string.Empty;
            [Required]
            public string Code { get; set; } = string.Empty;
            [Required]
            [DataType(DataType.Password)]
            [StringLength(100, MinimumLength = 6)]
            public string Password { get; set; } = string.Empty;
            [Required]
            [DataType(DataType.Password)]
            [Compare("Password", ErrorMessage = "Passwords do not match.")]
            public string ConfirmPassword { get; set; } = string.Empty;
        }

        public void OnGet(string? code = null, string? email = null)
        {
            Input.Code = code ?? string.Empty;
            Input.Email = email ?? string.Empty;
        }

        public async Task<IActionResult> OnPostAsync()
        {
            if (!ModelState.IsValid)
                return Page();
            var user = await _userManager.FindByEmailAsync(Input.Email);
            if (user == null)
            {
                ResultMessage = "Invalid request. User not found.";
                return Page();
            }
            var result = await _userManager.ResetPasswordAsync(user, Input.Code, Input.Password);
            if (result.Succeeded)
            {
                if (await _userManager.IsLockedOutAsync(user))
                {
                    await _userManager.SetLockoutEndDateAsync(user, DateTimeOffset.UtcNow);
                }
                ResultMessage = "Your password has been reset successfully. <a href='/Identity/Account/Login'>Click here to log in.</a>";
                return Page();
            }
            foreach (var error in result.Errors)
                ModelState.AddModelError(string.Empty, error.Description);
            ResultMessage = "Password could not be reset. Please make sure you meet the password requirements.";
            return Page();
        }
    }
} 