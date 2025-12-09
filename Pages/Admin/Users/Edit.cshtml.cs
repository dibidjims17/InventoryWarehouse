using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using MyRazorApp.Models;
using MyRazorApp.Services;

namespace MyRazorApp.Pages.Admin.Users
{
    public class EditModel : PageModel
    {
        private readonly MongoService _mongo;

        public EditModel(MongoService mongo)
        {
            _mongo = mongo;
        }

        [BindProperty]
        public User EditUser { get; set; } = new(); // Only editable fields bound

        public List<string> Roles { get; set; } = new() { "client", "admin" };

        [TempData]
        public string SuccessMessage { get; set; } = string.Empty;

        public async Task<IActionResult> OnGetAsync(string id)
        {
            if (string.IsNullOrEmpty(id))
                return RedirectToPage("/Admin/Users/Index");

            var user = await _mongo.GetUserByIdAsync(id);
            if (user == null)
                return RedirectToPage("/Admin/Users/Index");

            EditUser = user;
            return Page();
        }

        public async Task<IActionResult> OnPostAsync()
        {
            if (!ModelState.IsValid)
                return Page();

            // Load the full existing user
            var existingUser = await _mongo.GetUserByIdAsync(EditUser.UserId);
            if (existingUser == null)
                return NotFound();

            // Update only editable fields
            existingUser.Username = EditUser.Username;
            existingUser.Email = EditUser.Email;
            existingUser.Role = EditUser.Role;

            await _mongo.UpdateUserAsync(existingUser);

            // Optional: log the edit
            var report = new Report
            {
                Type = "user_edit",
                PerformedBy = HttpContext.Session.GetString("username") ?? "Unknown",
                TargetName = existingUser.Username,
                OldValue = $"Role: {existingUser.Role}, Status: {existingUser.Status}",
                NewValue = $"Role: {existingUser.Role}, Status: {existingUser.Status}",
                Details = $"User account updated."
            };
            await _mongo.CreateReportAsync(report);

            // Set TempData for success message + countdown
            SuccessMessage = $"User '{existingUser.Username}' updated successfully!";
            return Page(); // Keep on the same page to show the message + countdown
        }
    }
}
