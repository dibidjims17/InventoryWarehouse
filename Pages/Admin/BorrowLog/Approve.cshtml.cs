using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using MyRazorApp.Models;
using MyRazorApp.Services;

namespace MyRazorApp.Pages.Admin.BorrowLog
{
    public class ApproveModel : PageModel
    {
        private readonly MongoService _mongo;

        public ApproveModel(MongoService mongo)
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

            // Deduct stock
            var item = await _mongo.GetItemByIdAsync(borrow.ItemId!);
            if (item == null)
            {
                TempData["ErrorMessage"] = "Item not found.";
                return RedirectToPage("Index");
            }

            if (item.Quantity < borrow.Quantity)
            {
                TempData["ErrorMessage"] = "Not enough stock to approve this borrow.";
                return RedirectToPage("Index");
            }

            item.Quantity -= borrow.Quantity;
            await _mongo.UpdateItemAsync(item);

            // Update borrow status
            await _mongo.UpdateBorrowStatusAsync(borrow.Id!, "Approved");

            // Create report
            var report = new Report
            {
                Type = "borrow_approved",
                PerformedBy = HttpContext.Session.GetString("username") ?? "Unknown", // Admin approving
                TargetName = borrow.ItemName,
                Details = $"Borrow request approved for {borrow.Quantity} pcs of '{borrow.ItemName}' by user '{borrow.Username}'",
                Timestamp = DateTime.UtcNow
            };

            await _mongo.CreateReportAsync(report);

            return RedirectToPage("Index");
        }

    }
}
