using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using MyRazorApp.Models;
using MyRazorApp.Services;

namespace MyRazorApp.Pages.Auth
{
    public class LogoutModel : PageModel
    {
        private readonly MongoService _mongoService;

        public LogoutModel(MongoService mongoService)
        {
            _mongoService = mongoService;
        }

        public async Task<IActionResult> OnGetAsync()
        {
            string? token = HttpContext.Session.GetString("session_token");
            string? username = HttpContext.Session.GetString("username") ?? "Unknown";

            if (!string.IsNullOrEmpty(username)) // fallback if token is missing
            {
                // Log logout report first
                var report = new Report
                {
                    Type = "logout",
                    PerformedBy = username,
                    TargetName = username,
                    Details = $"User '{username}' logged out.",
                    Timestamp = DateTime.UtcNow
                };
                await _mongoService.CreateReportAsync(report);
            }

            if (!string.IsNullOrEmpty(token))
            {
                var user = await _mongoService.GetUserByTokenAsync(token);
                if (user != null)
                {
                    user.Status = "Inactive";
                    user.SessionToken = null;
                    await _mongoService.UpdateUserAsync(user);
                }
            }

            HttpContext.Session.Clear();
            if (Request.Cookies.ContainsKey("rememberme"))
                Response.Cookies.Delete("rememberme");

            return RedirectToPage("/Auth/Login");
        }
    }
}
