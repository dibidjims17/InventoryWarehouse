using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using MyRazorApp.Models;
using MyRazorApp.Services;

namespace MyRazorApp.Pages.Admin.Inventory
{
    public class CreateModel : PageModel
    {
        private readonly MongoService _mongo;
        private readonly IWebHostEnvironment _env;

        public CreateModel(MongoService mongo, IWebHostEnvironment env)
        {
            _mongo = mongo;
            _env = env;
        }

        // ----------------------------
        // Properties bound to the form
        // ----------------------------
        [BindProperty] public string Name { get; set; } = string.Empty;
        [BindProperty] public int Quantity { get; set; }
        [BindProperty] public string Description { get; set; } = string.Empty;
        [BindProperty] public string Category { get; set; } = string.Empty;
        [BindProperty] public IFormFile? ImageFile { get; set; }

        // For the form dropdown
        public List<string> Categories { get; set; } = new() { "Tools", "Safety Gear", "Equipment", "Office Supplies", "Electronics" };

        public string? SuccessMessage { get; set; }

        public void OnGet()
        {
            // Categories already initialized
        }

        public async Task<IActionResult> OnPostAsync()
        {
            if (!ModelState.IsValid)
                return Page();

            // Save image if provided
            string imageFileName = string.Empty;
            if (ImageFile != null && ImageFile.Length > 0)
            {
                string uploadsFolder = Path.Combine(_env.WebRootPath, "uploads/items");
                Directory.CreateDirectory(uploadsFolder);
                imageFileName = $"{Guid.NewGuid()}{Path.GetExtension(ImageFile.FileName)}";
                string filePath = Path.Combine(uploadsFolder, imageFileName);

                using var stream = new FileStream(filePath, FileMode.Create);
                await ImageFile.CopyToAsync(stream);
            }

            // Generate code based on total number of items
            long itemCount = await _mongo.ItemsCountAsync(); // uses your MongoService method
            string newCode = $"ITM{(itemCount + 1).ToString("D3")}";

            // Create new Item
            var item = new Item
            {
                Code = newCode,
                Name = Name,
                Quantity = Quantity,
                Description = Description,
                Category = Category,
                AddedBy = HttpContext.Session.GetString("username") ?? "Unknown",
                ImagePath = imageFileName
            };

            await _mongo.CreateItemAsync(item);

            // Log report
            var report = new Report
            {
                Type = "item_add",
                PerformedBy = item.AddedBy ?? "Unknown",
                TargetName = item.Name,
                NewValue = $"Quantity: {item.Quantity}, Category: {item.Category}",
                Details = $"Added new item '{item.Name}' in category '{item.Category}' with quantity {item.Quantity}.",
                Timestamp = DateTime.UtcNow
            };

            await _mongo.CreateReportAsync(report);

            SuccessMessage = $"Item '{item.Name}' added successfully with code {item.Code}.";

            // Clear form fields
            Name = string.Empty;
            Quantity = 0;
            Description = string.Empty;
            Category = string.Empty;
            ImageFile = null;

            return Page();
        }

    }
}
