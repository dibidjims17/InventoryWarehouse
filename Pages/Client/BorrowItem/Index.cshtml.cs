using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using MyRazorApp.Models;
using MyRazorApp.Services;

namespace MyRazorApp.Pages.Client.BorrowItem
{
    public class IndexModel : PageModel
    {
        private readonly MongoService _mongo;

        public IndexModel(MongoService mongo)
        {
            _mongo = mongo;
        }

        public List<Borrow> Borrows { get; set; } = new List<Borrow>();

        public string? SuccessMessage { get; set; }
        public string? ErrorMessage { get; set; }

        public async Task<IActionResult> OnGetAsync()
        {
            var userId = HttpContext.Session.GetString("user_id");
            if (string.IsNullOrEmpty(userId)) return RedirectToPage("/Auth/Login");

            Borrows = await _mongo.GetBorrowsByUserAsync(userId);

            return Page();
        }

        public async Task<IActionResult> OnPostRequestReturnAsync(string borrowId)
        {
            var userId = HttpContext.Session.GetString("user_id");
            var username = HttpContext.Session.GetString("username");

            if (string.IsNullOrEmpty(borrowId) || string.IsNullOrEmpty(userId) || string.IsNullOrEmpty(username))
            {
                ErrorMessage = "Invalid request.";
                return RedirectToPage();
            }

            var borrows = await _mongo.GetBorrowsByUserAsync(userId);
            var borrow = borrows.FirstOrDefault(b => b.Id == borrowId);

            if (borrow == null || borrow.Status != "Approved")
            {
                ErrorMessage = "Cannot request return for this borrow.";
                return RedirectToPage();
            }

            borrow.ReturnRequested = true;
            await _mongo.UpdateBorrowAsync(borrow);

            // Create report
            var report = new Report
            {
                Type = "borrow_return_request",
                PerformedBy = username,
                TargetName = borrow.ItemName,
                Details = $"Return requested for {borrow.Quantity} pcs of '{borrow.ItemName}'",
                Timestamp = DateTime.UtcNow
            };
            await _mongo.CreateReportAsync(report);

            SuccessMessage = $"Return request for '{borrow.ItemName}' submitted successfully.";

            return RedirectToPage();
        }
    }
}
