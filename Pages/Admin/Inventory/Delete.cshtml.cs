using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using MyRazorApp.Models;
using MyRazorApp.Services;

namespace MyRazorApp.Pages.Admin.Inventory
{
    public class DeleteModel : PageModel
    {
        private readonly MongoService _mongo;

        public DeleteModel(MongoService mongo)
        {
            _mongo = mongo;
        }

        [BindProperty] public string Id { get; set; } = string.Empty;
        public string ItemName { get; private set; } = string.Empty;
        public string Category { get; private set; } = string.Empty;

        public string? SuccessMessage { get; private set; }

        // Load item details to show on confirmation page
        public async Task<IActionResult> OnGetAsync(string id)
        {
            if (string.IsNullOrWhiteSpace(id))
                return RedirectToPage("/Admin/Inventory/Index");

            var item = await _mongo.GetItemByIdAsync(id);
            if (item == null)
                return RedirectToPage("/Admin/Inventory/Index");

            Id = item.Id ?? string.Empty;
            ItemName = item.Name ?? string.Empty;
            Category = item.Category ?? string.Empty;

            return Page();
        }

        // Handle deletion
        public async Task<IActionResult> OnPostAsync()
        {
            if (string.IsNullOrWhiteSpace(Id))
                return RedirectToPage("/Admin/Inventory/Index");

            string performedBy = HttpContext.Session.GetString("username") ?? "Unknown";

            await _mongo.DeleteItemAsync(Id, performedBy);

            SuccessMessage = $"Item '{ItemName}' has been deleted successfully.";

            // Redirect back to inventory index after deletion
            return RedirectToPage("/Admin/Inventory/Index");
        }
    }
}
