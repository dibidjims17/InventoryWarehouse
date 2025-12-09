using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using MyRazorApp.Models;
using MyRazorApp.Helpers;
using MyRazorApp.Services;
using System.Threading.Tasks;

namespace MyRazorApp.Pages.Auth
{
    public class RegisterModel : PageModel
    {
        private readonly MongoService _mongoService;

        public RegisterModel(MongoService mongoService)
        {
            _mongoService = mongoService;
        }

        [BindProperty]
        public string Email { get; set; } = string.Empty;

        [BindProperty]
        public string Username { get; set; } = string.Empty;

        [BindProperty]
        public string Password { get; set; } = string.Empty;

        [BindProperty]
        public string ConfirmPassword { get; set; } = string.Empty;

        public string ErrorMessage { get; set; } = string.Empty;

        public async Task<IActionResult> OnPostAsync()
        {
            if (!ModelState.IsValid)
                return Page();

            // -----------------------
            // Password validation
            // -----------------------
            if (Password.Length < 8)
            {
                ErrorMessage = "Password must be at least 8 characters long.";
                return Page();
            }

            if (Password != ConfirmPassword)
            {
                ErrorMessage = "Passwords do not match.";
                return Page();
            }

            // Check if username or email already exists
            var existingUser = await _mongoService.GetUserByUsernameOrEmailAsync(Username) 
                            ?? await _mongoService.GetUserByUsernameOrEmailAsync(Email);

            if (existingUser != null)
            {
                ErrorMessage = "Username or email already exists.";
                return Page();
            }

            // -----------------------
            // Create new user
            // -----------------------
            var user = new User
            {
                Email = Email,
                Username = Username,
                PasswordHash = AuthHelper.HashPassword(Password),
                Role = "client",
                Status = "inactive",
                IsEmailVerified = false,
                EmailVerificationCode = CodeHelper.GenerateVerificationCode(),
                EmailVerificationExpiry = DateTime.UtcNow.AddMinutes(15)
            };

            await _mongoService.CreateUserAsync(user);

            // -----------------------
            // Send verification email
            // -----------------------
            EmailHelper.SendVerificationEmail(user.Email, user.EmailVerificationCode!);

            // -----------------------
            // Redirect to verification page
            // -----------------------
            return RedirectToPage("/Auth/VerifyEmail", new { email = user.Email });
        }
    }
}
