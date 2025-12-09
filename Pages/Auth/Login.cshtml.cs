using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using MyRazorApp.Models;
using MyRazorApp.Helpers;
using MyRazorApp.Services;
using Microsoft.AspNetCore.Http;
using System;
using System.Threading.Tasks;

namespace MyRazorApp.Pages.Auth
{
    public class LoginModel : PageModel
    {
        private readonly MongoService _mongoService;

        public LoginModel(MongoService mongoService)
        {
            _mongoService = mongoService;
        }

        [BindProperty]
        public string LoginInput { get; set; } = string.Empty; // username OR email

        [BindProperty]
        public string Password { get; set; } = string.Empty;

        [BindProperty]
        public bool RememberMe { get; set; } = false;

        public string ErrorMessage { get; set; } = string.Empty;

        public async Task<IActionResult> OnPostAsync()
        {
            if (!ModelState.IsValid)
                return Page();

            User? user;

            // Detect if input is an email or username
            if (LoginInput.Contains("@"))
                user = await _mongoService.GetUserByEmailAsync(LoginInput);
            else
                user = await _mongoService.GetUserByUsernameAsync(LoginInput);

            // Check credentials
            if (user == null || !AuthHelper.VerifyPassword(Password, user.PasswordHash))
            {
                ErrorMessage = "Invalid login credentials.";
                return Page();
            }

            // Check if account is deactivated
            if (user.Status?.ToLower() == "deactivated")
            {
                ErrorMessage = "This account has been deactivated by the admin.";
                return Page();
            }

            // **Check if email is verified**
            if (!user.IsEmailVerified)
            {
                ErrorMessage = "Please verify your email before logging in.";
                return Page();
            }

            // Update user status and last login
            user.Status = "Active";
            user.LastLogin = DateTime.UtcNow;

            // Generate session token
            string token = AuthHelper.GenerateToken(user.UserId);
            user.SessionToken = token;

            await _mongoService.UpdateUserAsync(user);

            // Store session safely
            HttpContext.Session.SetString("session_token", token);
            HttpContext.Session.SetString("user_id", user.UserId ?? string.Empty);
            HttpContext.Session.SetString("username", user.Username ?? "Unknown");
            HttpContext.Session.SetString("role", user.Role ?? "client");

            // Store token in cookie if "Remember Me" is checked
            if (RememberMe)
            {
                var options = new CookieOptions
                {
                    HttpOnly = true,
                    Secure = true,
                    SameSite = SameSiteMode.Strict,
                    Expires = DateTime.UtcNow.AddDays(30)
                };
                Response.Cookies.Append("rememberme", token, options);
            }

            // Log the login action
            var loginReport = new Report
            {
                Type = "user_login",
                PerformedBy = user.Username ?? "Unknown",
                TargetName = user.Username ?? "Unknown",
                Details = $"User '{user.Username}' logged in successfully.",
                Timestamp = DateTime.UtcNow
            };
            await _mongoService.CreateReportAsync(loginReport);

            return RedirectToPage("/Index");
        }
        public async Task<IActionResult> OnPostResendVerificationAsync()
        {
            if (string.IsNullOrEmpty(LoginInput))
            {
                ErrorMessage = "Invalid input.";
                return Page();
            }

            User? user;
            if (LoginInput.Contains("@"))
                user = await _mongoService.GetUserByEmailAsync(LoginInput);
            else
                user = await _mongoService.GetUserByUsernameAsync(LoginInput);

            if (user == null)
            {
                ErrorMessage = "User not found.";
                return Page();
            }

            if (user.IsEmailVerified)
            {
                ErrorMessage = "Email is already verified. Please login.";
                return Page();
            }

            // Generate new verification code
            user.EmailVerificationCode = CodeHelper.GenerateVerificationCode();
            user.EmailVerificationExpiry = DateTime.UtcNow.AddMinutes(15);
            await _mongoService.UpdateUserAsync(user);

            // Send verification email
            EmailHelper.SendVerificationEmail(user.Email, user.EmailVerificationCode!);

            ErrorMessage = "Verification email resent. Please check your inbox.";
            return Page();
        }

    }
}
