using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using MyRazorApp.Models;
using MyRazorApp.Services;

namespace MyRazorApp.Pages.Client.ClientItems
{
    public class BorrowItemModel : PageModel
    {
        private readonly MongoService _mongo;

        public BorrowItemModel(MongoService mongo)
        {
            _mongo = mongo;
        }

        [BindProperty(SupportsGet = true)]
        public string ItemId { get; set; } = string.Empty;

        [BindProperty]
        public int Quantity { get; set; } = 1;

        public Item? ItemToBorrow { get; set; }
        public string? ErrorMessage { get; set; }
        public string? SuccessMessage { get; set; }

        // Helper properties for session info
        private string CurrentUserId => HttpContext.Session.GetString("user_id") ?? string.Empty;
        private string CurrentUsername => HttpContext.Session.GetString("username") ?? string.Empty;

        public async Task OnGetAsync()
        {
            if (string.IsNullOrEmpty(ItemId))
            {
                ErrorMessage = "Item ID not specified.";
                return;
            }

            ItemToBorrow = await _mongo.GetItemByIdAsync(ItemId);
            if (ItemToBorrow == null)
            {
                ErrorMessage = "Item not found.";
            }
        }

        public async Task<IActionResult> OnPostAsync()
        {
            if (string.IsNullOrEmpty(ItemId))
            {
                ErrorMessage = "Item ID not specified.";
                return Page();
            }

            ItemToBorrow = await _mongo.GetItemByIdAsync(ItemId);
            if (ItemToBorrow == null)
            {
                ErrorMessage = "Item not found.";
                return Page();
            }

            if (Quantity < 1)
            {
                ErrorMessage = "Quantity must be at least 1.";
                return Page();
            }

            if (Quantity > ItemToBorrow.Quantity)
            {
                ErrorMessage = "Requested quantity exceeds available stock.";
                return Page();
            }

            // Create borrow record
            var borrow = new Borrow
            {
                UserId = CurrentUserId,
                Username = CurrentUsername,
                ItemId = ItemToBorrow.Id!,
                ItemCode = ItemToBorrow.Code,
                ItemName = ItemToBorrow.Name,
                ItemCategory = ItemToBorrow.Category,
                Quantity = Quantity,
                Status = "Pending",
                RequestedAt = DateTime.UtcNow,
                Returned = false,
                ReturnRequested = false
            };

            await _mongo.CreateBorrowAsync(borrow);

            // Create report using full sentence style
            await _mongo.CreateReportAsync(new Report
            {
                Type = "borrow",
                PerformedBy = CurrentUsername,
                TargetName = ItemToBorrow.Name,
                Details = $"User '{CurrentUsername}' requested to borrow {Quantity} pcs of '{ItemToBorrow.Name}' from category '{ItemToBorrow.Category}'.",
                Timestamp = DateTime.UtcNow
            });

            SuccessMessage = $"Borrow request for '{ItemToBorrow.Name}' submitted successfully!";
            return Page();
        }
    }
}
