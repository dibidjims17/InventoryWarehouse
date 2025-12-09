using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using MyRazorApp.Models;
using MyRazorApp.Services;

namespace MyRazorApp.Pages.Admin.Users
{
    public class DeactivateModel : PageModel
    {
        private readonly MongoService _mongo;

        public DeactivateModel(MongoService mongo)
        {
            _mongo = mongo;
        }

        public async Task<IActionResult> OnGetAsync(string id)
        {
            if (string.IsNullOrEmpty(id))
                return RedirectToPage("/Admin/Users/Index");

            var user = await _mongo.GetUserByIdAsync(id);
            if (user == null)
                return RedirectToPage("/Admin/Users/Index");

            user.Status = "Deactivated";
            await _mongo.UpdateUserAsync(user);

            // Log report
            var username = HttpContext.Session.GetString("username") ?? "Unknown";

            var report = new Report
            {
                Type = "user_deactivated",
                PerformedBy = username,
                TargetName = user.Username,
                Details = $"User '{user.Username}' was deactivated.",
                Timestamp = DateTime.UtcNow
            };
            await _mongo.CreateReportAsync(report);

            TempData["SuccessMessage"] = $"User '{user.Username}' deactivated successfully!";
            return RedirectToPage("/Admin/Users/Index");
        }
    }
}
