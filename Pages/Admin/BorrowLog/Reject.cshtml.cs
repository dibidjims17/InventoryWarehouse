using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using MyRazorApp.Models;
using MyRazorApp.Services;

namespace MyRazorApp.Pages.Admin.BorrowLog
{
    public class RejectModel : PageModel
    {
        private readonly MongoService _mongo;

        public RejectModel(MongoService mongo)
        {
            _mongo = mongo;
        }

        [BindProperty(SupportsGet = true)]
        public string? Id { get; set; }

        public async Task<IActionResult> OnGetAsync()
        {
            var role = HttpContext.Session.GetString("role");
            if (role != "admin") return RedirectToPage("/AccessDenied");
            if (string.IsNullOrEmpty(Id)) return RedirectToPage("Index");

            var borrowList = await _mongo.GetAllBorrowsAsync();
            var borrow = borrowList.FirstOrDefault(b => b.Id == Id);
            if (borrow == null) return RedirectToPage("Index");

            // Update status
            await _mongo.UpdateBorrowStatusAsync(borrow.Id!, "Rejected");

            // Create report
            var report = new Report
            {
                Type = "borrow_rejected",
                PerformedBy = HttpContext.Session.GetString("username") ?? "Unknown", // Admin approving
                TargetName = borrow.ItemName,
                Details = $"Borrow request rejected for {borrow.Quantity} pcs of '{borrow.ItemName}' by user '{borrow.Username}'",
                Timestamp = DateTime.UtcNow
            };

            await _mongo.CreateReportAsync(report);

            return RedirectToPage("Index");
        }
    }
}
