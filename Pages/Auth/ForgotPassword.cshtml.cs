using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using MyRazorApp.Helpers;
using MyRazorApp.Services;
using System;

namespace MyRazorApp.Pages.Auth
{
    public class ForgotPasswordModel : PageModel
    {
        private readonly MongoService _mongoService;

        public ForgotPasswordModel(MongoService mongoService)
        {
            _mongoService = mongoService;
        }

        [BindProperty]
        public string Email { get; set; } = string.Empty;

        public string Message { get; set; } = string.Empty;

        public void OnGet() { }

        public IActionResult OnPost()
        {
            var user = _mongoService.GetUserByEmailAsync(Email).Result;
            if (user == null)
            {
                Message = "Email not found.";
                return Page();
            }

            // Generate 6-digit OTP
            var random = new Random();
            var otp = random.Next(100000, 999999).ToString();

            user.OTP = otp;
            user.OTPExpiry = DateTime.UtcNow.AddMinutes(10); // OTP valid for 10 minutes

            _mongoService.UpdateUserAsync(user).Wait();

            // Send OTP via email
            EmailHelper.SendOTPEmail(user.Email, otp);

            // Redirect to ResetPassword page, passing email
            return RedirectToPage("/Auth/ResetPassword", new { email = Email });
        }

    }
}
