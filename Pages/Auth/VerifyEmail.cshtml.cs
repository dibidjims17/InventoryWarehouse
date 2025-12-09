using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using MyRazorApp.Helpers;
using MyRazorApp.Services;
using System;

namespace MyRazorApp.Pages.Auth
{
    public class VerifyEmailModel : PageModel
    {
        private readonly MongoService _mongoService;

        public VerifyEmailModel(MongoService mongoService)
        {
            _mongoService = mongoService;
        }

        [BindProperty]
        public string Email { get; set; } = string.Empty;

        [BindProperty]
        public string Code { get; set; } = string.Empty;

        public string Message { get; set; } = string.Empty;

        // ✅ Accept code from query string to auto-fill input
        public void OnGet(string email, string? code = null)
        {
            Email = email;
            if (!string.IsNullOrEmpty(code))
            {
                Code = code;
            }
        }

        // ✅ Verify entered code
        public IActionResult OnPost()
        {
            var user = _mongoService.GetUserByEmailAsync(Email).Result;
            if (user == null)
            {
                Message = "User not found.";
                return Page();
            }

            if (user.EmailVerificationCode == Code &&
                user.EmailVerificationExpiry.HasValue &&
                user.EmailVerificationExpiry.Value > DateTime.UtcNow)
            {
                user.IsEmailVerified = true;
                user.EmailVerificationCode = null;
                user.EmailVerificationExpiry = null;
                user.Status = "active";

                _mongoService.UpdateUserAsync(user).Wait();

                return RedirectToPage("/Auth/Login");
            }
            else
            {
                Message = "Invalid or expired verification code.";
                return Page();
            }
        }

        // ✅ Resend verification code with "Verify Account" button
        public IActionResult OnPostResend()
        {
            var user = _mongoService.GetUserByEmailAsync(Email).Result;
            if (user == null)
            {
                Message = "User not found.";
                return Page();
            }

            var now = DateTime.UtcNow;

            if (user.LastVerificationEmailSent.HasValue && (now - user.LastVerificationEmailSent.Value).TotalSeconds < 60)
            {
                var wait = 60 - (now - user.LastVerificationEmailSent.Value).TotalSeconds;
                Message = $"Please wait {Math.Ceiling(wait)} seconds before requesting a new code.";
                return Page();
            }

            // Generate new 6-digit code
            user.EmailVerificationCode = CodeHelper.GenerateVerificationCode();
            user.EmailVerificationExpiry = DateTime.UtcNow.AddMinutes(15);
            user.LastVerificationEmailSent = now;

            _mongoService.UpdateUserAsync(user).Wait();

            // Send new email with clickable button
            EmailHelper.SendVerificationEmail(user.Email, user.EmailVerificationCode!);

            Message = "A new verification code has been sent to your email.";
            return Page();
        }
    }
}
