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

        public List<Borrow> Borrows { get; set; } = new();

        public string? SuccessMessage { get; set; }
        public string? ErrorMessage { get; set; }

        // =========================
        // GET: LOAD BORROWS
        // =========================
        public async Task<IActionResult> OnGetAsync()
        {
            var userId = HttpContext.Session.GetString("user_id");
            if (string.IsNullOrEmpty(userId))
                return RedirectToPage("/Auth/Login");

            Borrows = await _mongo.GetBorrowsByUserAsync(userId);
            return Page();
        }

        // =========================
        // POST: REQUEST RETURN
        // =========================
        public async Task<IActionResult> OnPostRequestReturnAsync(string borrowId)
        {
            var userId = HttpContext.Session.GetString("user_id");
            var username = HttpContext.Session.GetString("username");

            if (string.IsNullOrEmpty(userId) ||
                string.IsNullOrEmpty(username) ||
                string.IsNullOrEmpty(borrowId))
            {
                ErrorMessage = "Invalid request.";
                return RedirectToPage();
            }

            var borrows = await _mongo.GetBorrowsByUserAsync(userId);
            var borrow = borrows.FirstOrDefault(b => b.Id == borrowId);

            // Server-side protection (important)
            if (borrow == null)
            {
                ErrorMessage = "Borrow record not found.";
                return RedirectToPage();
            }

            if (borrow.Status != "Approved")
            {
                ErrorMessage = "Only approved items can be returned.";
                return RedirectToPage();
            }

            if (borrow.ReturnRequested)
            {
                ErrorMessage = "Return request already submitted.";
                return RedirectToPage();
            }

            // Update borrow
            borrow.ReturnRequested = true;
            await _mongo.UpdateBorrowAsync(borrow);

            // Create audit report
            await _mongo.CreateReportAsync(new Report
            {
                Type = "borrow_return_request",
                PerformedBy = username,
                TargetName = borrow.ItemName,
                Details = $"Return requested for {borrow.Quantity} pcs of '{borrow.ItemName}'",
                Timestamp = DateTime.UtcNow
            });

            SuccessMessage = $"Return request for '{borrow.ItemName}' submitted successfully.";
            return RedirectToPage();
        }
    }
}
