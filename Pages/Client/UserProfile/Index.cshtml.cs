using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using MyRazorApp.Models;
using MyRazorApp.Services;
using MyRazorApp.Helpers;
using Microsoft.AspNetCore.Http;
using System.IO;
using System.Threading.Tasks;

namespace MyRazorApp.Pages.Client.UserProfile
{
    public class IndexModel : PageModel
    {
        private readonly MongoService _mongoService;
        private readonly IWebHostEnvironment _env;

        public IndexModel(MongoService mongoService, IWebHostEnvironment env)
        {
            _mongoService = mongoService;
            _env = env;
        }

        // ----------------------------
        // Bind properties
        // ----------------------------
        [BindProperty]
        public User CurrentUser { get; set; } = new();

        [BindProperty]
        public IFormFile? ProfilePictureFile { get; set; }

        [BindProperty]
        public string CurrentPassword { get; set; } = string.Empty;

        [BindProperty]
        public string NewPassword { get; set; } = string.Empty;

        [BindProperty]
        public string ConfirmPassword { get; set; } = string.Empty;

        public string Message { get; set; } = string.Empty;
        public string ErrorMessage { get; set; } = string.Empty;

        // ----------------------------
        // Load user
        // ----------------------------
        public async Task<IActionResult> OnGetAsync()
        {
            string? userId = HttpContext.Session.GetString("user_id");
            if (string.IsNullOrEmpty(userId))
                return RedirectToPage("/Auth/Login");

            var user = await _mongoService.GetUserByIdAsync(userId);
            if (user == null)
                return RedirectToPage("/Auth/Login");

            CurrentUser = user;

            // Ensure default profile picture if none
            if (string.IsNullOrEmpty(CurrentUser.ProfilePicture))
            {
                CurrentUser.ProfilePicture = null; // keep null, view handles default
            }

            return Page();
        }

        // ----------------------------
        // Upload or change profile picture
        // ----------------------------
        public async Task<IActionResult> OnPostUploadAsync()
        {
            if (ProfilePictureFile == null || ProfilePictureFile.Length == 0)
            {
                ErrorMessage = "No file selected.";
                return Page();
            }

            string? userId = HttpContext.Session.GetString("user_id");
            if (string.IsNullOrEmpty(userId))
                return RedirectToPage("/Auth/Login");

            var user = await _mongoService.GetUserByIdAsync(userId);
            if (user == null) return Page();

            // Delete old picture if exists
            if (!string.IsNullOrEmpty(user.ProfilePicture))
            {
                var oldPath = Path.Combine(_env.WebRootPath, "uploads", user.ProfilePicture);
                if (System.IO.File.Exists(oldPath))
                    System.IO.File.Delete(oldPath);
            }

            // Save new file
            string fileName = $"{user.UserId}_{Path.GetFileName(ProfilePictureFile.FileName)}";
            var filePath = Path.Combine(_env.WebRootPath, "uploads", fileName);

            using (var stream = new FileStream(filePath, FileMode.Create))
            {
                await ProfilePictureFile.CopyToAsync(stream);
            }

            user.ProfilePicture = fileName;
            await _mongoService.UpdateUserAsync(user, user.Username);

            Message = "Profile picture updated successfully!";
            CurrentUser = user;
            return Page();
        }

        // ----------------------------
        // Remove profile picture
        // ----------------------------
        public async Task<IActionResult> OnPostRemovePictureAsync()
        {
            string? userId = HttpContext.Session.GetString("user_id");
            if (string.IsNullOrEmpty(userId))
                return RedirectToPage("/Auth/Login");

            var user = await _mongoService.GetUserByIdAsync(userId);
            if (user == null) return Page();

            if (!string.IsNullOrEmpty(user.ProfilePicture))
            {
                var oldPath = Path.Combine(_env.WebRootPath, "uploads", user.ProfilePicture);
                if (System.IO.File.Exists(oldPath))
                    System.IO.File.Delete(oldPath);
            }

            user.ProfilePicture = null;
            await _mongoService.UpdateUserAsync(user, user.Username);

            Message = "Profile picture removed successfully!";
            CurrentUser = user;
            return Page();
        }

        // ----------------------------
        // Change username
        // ----------------------------
        public async Task<IActionResult> OnPostChangeUsernameAsync()
        {
            string? userId = HttpContext.Session.GetString("user_id");
            if (string.IsNullOrEmpty(userId))
                return RedirectToPage("/Auth/Login");

            var user = await _mongoService.GetUserByIdAsync(userId);
            if (user == null) return Page();

            if (string.IsNullOrWhiteSpace(CurrentUser.Username))
            {
                ErrorMessage = "Username cannot be empty.";
                CurrentUser = user;
                return Page();
            }

            // Optional: Check if username is already taken
            var existingUser = await _mongoService.GetUserByUsernameAsync(CurrentUser.Username);
            if (existingUser != null && existingUser.UserId != user.UserId)
            {
                ErrorMessage = "Username is already taken.";
                CurrentUser = user;
                return Page();
            }

            user.Username = CurrentUser.Username;
            await _mongoService.UpdateUserAsync(user, user.Username);

            Message = "Username updated successfully!";
            CurrentUser = user;
            return Page();
        }

        // ----------------------------
        // Change password
        // ----------------------------
        public async Task<IActionResult> OnPostChangePasswordAsync()
        {
            string? userId = HttpContext.Session.GetString("user_id");
            if (string.IsNullOrEmpty(userId))
                return RedirectToPage("/Auth/Login");

            var user = await _mongoService.GetUserByIdAsync(userId);
            if (user == null) return Page();

            if (!AuthHelper.VerifyPassword(CurrentPassword, user.PasswordHash))
            {
                ErrorMessage = "Current password is incorrect.";
                CurrentUser = user;
                return Page();
            }

            if (NewPassword != ConfirmPassword)
            {
                ErrorMessage = "New passwords do not match.";
                CurrentUser = user;
                return Page();
            }

            user.PasswordHash = AuthHelper.HashPassword(NewPassword);
            await _mongoService.UpdateUserAsync(user, user.Username);

            Message = "Password changed successfully!";
            CurrentUser = user;
            return Page();
        }
    }
}
