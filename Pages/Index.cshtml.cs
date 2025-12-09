using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using MyRazorApp.Services;
using MyRazorApp.Models;

namespace MyRazorApp.Pages
{
    // Prevent browser caching of dashboard page
    [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
    public class IndexModel : PageModel
    {
        private readonly MongoService _mongoService;

        public IndexModel(MongoService mongoService)
        {
            _mongoService = mongoService;
        }

        // Current user
        public User? CurrentUser { get; set; }

        // Admin metrics
        public int TotalItems { get; set; }
        public int TotalUsers { get; set; }
        public int TotalLogs { get; set; }
        public int ItemsOverdueCount { get; set; }
        public int PendingUserCount { get; set; }

        // Client metrics
        public int MyBorrowedCount { get; set; }
        public int AvailableItems { get; set; }
        public int MyPendingRequestCount { get; set; }

        // Recent Activity
        public List<BorrowRecord> RecentBorrows { get; set; } = new();

        // Borrowing Trends (last 30 days)
        public List<BorrowTrend> BorrowingTrends { get; set; } = new();

        public async Task<IActionResult> OnGetAsync()
        {
            // Extra safety: prevent cached pages
            Response.Headers["Cache-Control"] = "no-cache, no-store, must-revalidate";
            Response.Headers["Pragma"] = "no-cache";
            Response.Headers["Expires"] = "0";

            // Check session
            var userId = HttpContext.Session.GetString("user_id");
            if (string.IsNullOrEmpty(userId))
                return RedirectToPage("/Auth/Login");

            // Load current user
            CurrentUser = await _mongoService.GetUserByIdAsync(userId);
            if (CurrentUser == null)
                return RedirectToPage("/Auth/Login");

            // Load shared metrics
            AvailableItems = (int)await _mongoService.ItemsCountAsync();

            if (CurrentUser.Role == "admin")
            {
                TotalItems = AvailableItems;
                TotalUsers = (int)await _mongoService.UsersCountAsync();

                var allLogs = await _mongoService.GetAllBorrowsAsync();
                TotalLogs = allLogs.Count;

                ItemsOverdueCount = (int)await _mongoService.GetOverdueBorrowsAsync();
                PendingUserCount = (int)await _mongoService.GetPendingUsersAsync();

                RecentBorrows = await _mongoService.GetRecentBorrowsAsync(5);
                BorrowingTrends = await _mongoService.GetBorrowsLastNDaysAsync(30);
            }
            else
            {
                var myBorrows = await _mongoService.GetBorrowsByUserAsync(userId);
                MyBorrowedCount = myBorrows.Count;

                MyPendingRequestCount = await _mongoService.GetPendingBorrowsByUserAsync(userId);

                RecentBorrows = await _mongoService.GetRecentBorrowsByUserAsync(userId, 5);
                BorrowingTrends = await _mongoService.GetBorrowsLastNDaysAsync(30, userId);
            }

            return Page();
        }
    }
}
