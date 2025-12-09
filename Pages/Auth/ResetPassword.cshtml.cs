using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using MyRazorApp.Services;
using System;
using BCrypt.Net;

namespace MyRazorApp.Pages.Auth
{
    public class ResetPasswordModel : PageModel
    {
        private readonly MongoService _mongoService;

        public ResetPasswordModel(MongoService mongoService)
        {
            _mongoService = mongoService;
        }

        [BindProperty]
        public string Email { get; set; } = string.Empty;

        [BindProperty]
        public string OTP { get; set; } = string.Empty;

        [BindProperty]
        public string NewPassword { get; set; } = string.Empty;

        [BindProperty]
        public string ConfirmPassword { get; set; } = string.Empty;

        public string Message { get; set; } = string.Empty;

        public void OnGet() { }

        public IActionResult OnPost()
        {
            if (NewPassword != ConfirmPassword)
            {
                Message = "Passwords do not match.";
                return Page();
            }

            var user = _mongoService.GetUserByEmailAsync(Email).Result;
            if (user == null)
            {
                Message = "Email not found.";
                return Page();
            }

            if (user.OTP != OTP || user.OTPExpiry < DateTime.UtcNow)
            {
                Message = "Invalid or expired OTP.";
                return Page();
            }

            // Reset password
            user.PasswordHash = BCrypt.Net.BCrypt.HashPassword(NewPassword);

            // Clear OTP
            user.OTP = string.Empty;
            user.OTPExpiry = null;

            _mongoService.UpdateUserAsync(user).Wait();

            Message = "Password has been reset successfully!";
            return Page();
        }
    }
}
