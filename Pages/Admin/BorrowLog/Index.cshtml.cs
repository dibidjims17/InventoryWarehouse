using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using MyRazorApp.Models;
using MyRazorApp.Services;
using System.Globalization;

namespace MyRazorApp.Pages.Admin.BorrowLog
{
    public class IndexModel : PageModel
    {
        private readonly MongoService _mongo;

        public IndexModel(MongoService mongo)
        {
            _mongo = mongo;
        }

        [BindProperty(SupportsGet = true)]
        public string? SearchQuery { get; set; }

        public List<Borrow> Logs { get; set; } = new List<Borrow>();

        public async Task<IActionResult> OnGetAsync()
        {
            // Admin protection
            var role = HttpContext.Session.GetString("role");
            if (role != "admin") return RedirectToPage("/AccessDenied");

            // Get all borrows
            var allBorrows = await _mongo.GetAllBorrowsAsync();

            // Apply search filter if needed
            if (!string.IsNullOrEmpty(SearchQuery))
            {
                var query = SearchQuery.ToLower(CultureInfo.InvariantCulture);
                Logs = allBorrows.Where(b =>
                    (!string.IsNullOrEmpty(b.Username) && b.Username.ToLower(CultureInfo.InvariantCulture).Contains(query)) ||
                    (!string.IsNullOrEmpty(b.ItemName) && b.ItemName.ToLower(CultureInfo.InvariantCulture).Contains(query)) ||
                    (!string.IsNullOrEmpty(b.ItemCode) && b.ItemCode.ToLower(CultureInfo.InvariantCulture).Contains(query)) ||
                    (!string.IsNullOrEmpty(b.ItemCategory) && b.ItemCategory.ToLower(CultureInfo.InvariantCulture).Contains(query))
                ).ToList();
            }
            else
            {
                Logs = allBorrows;
            }

            // Sort descending by requested date
            Logs = Logs.OrderByDescending(b => b.RequestedAt).ToList();

            return Page();
        }
    }
}
