using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using MyRazorApp.Models;
using MyRazorApp.Services;

namespace MyRazorApp.Pages.Client.ClientItems
{
    public class IndexModel : PageModel
    {
        private readonly MongoService _mongo;

        public IndexModel(MongoService mongo)
        {
            _mongo = mongo;
        }

        public List<Item> Items { get; set; } = new();
        public List<string> Categories { get; set; } = new() { "Tools", "Safety Gear", "Equipment", "Office Supplies", "Electronics" };

        [BindProperty(SupportsGet = true)]
        public string Search { get; set; } = string.Empty;

        [BindProperty(SupportsGet = true)]
        public string FilterCategory { get; set; } = string.Empty;

        public async Task OnGetAsync()
        {
            var allItems = await _mongo.GetAllItemsAsync();

            // Filter by category
            if (!string.IsNullOrEmpty(FilterCategory) && Categories.Contains(FilterCategory))
                allItems = allItems.Where(i => i.Category == FilterCategory).ToList();

            // Filter by search
            if (!string.IsNullOrEmpty(Search))
            {
                allItems = allItems.Where(i =>
                    (i.Name?.Contains(Search, StringComparison.OrdinalIgnoreCase) ?? false) ||
                    (i.Code?.Contains(Search, StringComparison.OrdinalIgnoreCase) ?? false)
                ).ToList();
            }

            Items = allItems;
        }
    }
}
