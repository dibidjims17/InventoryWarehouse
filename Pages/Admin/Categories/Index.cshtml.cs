using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using MyRazorApp.Models;
using MyRazorApp.Services;

namespace MyRazorApp.Pages.Admin.Inventory
{
    public class CategoriesModel : PageModel
    {
        private readonly MongoService _mongo;

        public CategoriesModel(MongoService mongo)
        {
            _mongo = mongo;
        }

        // Filters bound to query string
        [BindProperty(SupportsGet = true)]
        public string Search { get; set; } = string.Empty;

        [BindProperty(SupportsGet = true)]
        public string CategoryFilter { get; set; } = string.Empty;

        // Data for dropdown & display
        public List<string> Categories { get; private set; } = new();
        public List<Item> Items { get; private set; } = new();

        public async Task OnGetAsync()
        {
            // Fetch all items from MongoDB
            var allItems = await _mongo.GetAllItemsAsync();

            // Load distinct categories for the dropdown
            Categories = allItems
                .Where(i => !string.IsNullOrEmpty(i.Category))
                .Select(i => i.Category)
                .Distinct()
                .OrderBy(c => c)
                .ToList();

            // Apply filters
            Items = allItems;

            if (!string.IsNullOrWhiteSpace(Search))
            {
                Items = Items
                    .Where(i =>
                        (!string.IsNullOrEmpty(i.Name) && i.Name.Contains(Search, StringComparison.OrdinalIgnoreCase)) ||
                        (!string.IsNullOrEmpty(i.Code) && i.Code.Contains(Search, StringComparison.OrdinalIgnoreCase)))
                    .ToList();
            }

            if (!string.IsNullOrWhiteSpace(CategoryFilter))
            {
                Items = Items
                    .Where(i => string.Equals(i.Category, CategoryFilter, StringComparison.OrdinalIgnoreCase))
                    .ToList();
            }
        }
    }
}
