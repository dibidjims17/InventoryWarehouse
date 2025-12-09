using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.Mvc.Rendering;
using MyRazorApp.Models;
using MyRazorApp.Services;

namespace MyRazorApp.Pages.Admin.Inventory
{
    public class EditModel : PageModel
    {
        private readonly MongoService _mongo;
        private readonly IWebHostEnvironment _env;

        public EditModel(MongoService mongo, IWebHostEnvironment env)
        {
            _mongo = mongo;
            _env = env;
        }

        // ----------------------------
        // Properties bound to the form
        // ----------------------------
        [BindProperty] 
        public string Id { get; set; } = string.Empty;

        [BindProperty] 
        public string Name { get; set; } = string.Empty;

        [BindProperty] 
        public int Quantity { get; set; }

        [BindProperty] 
        public string Description { get; set; } = string.Empty;

        [BindProperty] 
        public string Category { get; set; } = string.Empty;

        [BindProperty] 
        public IFormFile? ImageFile { get; set; }

        // Holds the existing image path
        public string ExistingImagePath { get; private set; } = string.Empty;

        // Dropdown options
        public List<string> Categories { get; } = new() 
        { 
            "Tools", "Safety Gear", "Equipment", "Office Supplies", "Electronics" 
        };

        public string? SuccessMessage { get; private set; }

        // ----------------------------
        // Load the existing item
        // ----------------------------
        public async Task<IActionResult> OnGetAsync(string id)
        {
            if (string.IsNullOrWhiteSpace(id))
                return RedirectToPage("/Admin/Inventory/Index");

            var item = await _mongo.GetItemByIdAsync(id);
            if (item == null)
                return RedirectToPage("/Admin/Inventory/Index");

            // Populate form fields
            Id = item.Id ?? string.Empty;
            Name = item.Name ?? string.Empty;
            Quantity = item.Quantity;
            Description = item.Description ?? string.Empty;
            Category = item.Category ?? string.Empty;
            ExistingImagePath = item.ImagePath ?? string.Empty;

            return Page();
        }

        // ----------------------------
        // Save changes
        // ----------------------------
        public async Task<IActionResult> OnPostAsync()
        {
            if (!ModelState.IsValid)
                return Page();

            var item = await _mongo.GetItemByIdAsync(Id);
            if (item == null)
                return RedirectToPage("/Admin/Inventory/Index");

            // ----------------------------
            // Capture old values BEFORE update
            // ----------------------------
            var oldValue = $"Quantity: {item.Quantity}, Category: {item.Category}";

            // ----------------------------
            // Update item fields
            // ----------------------------
            item.Name = Name ?? string.Empty;
            item.Quantity = Quantity;
            item.Description = Description ?? string.Empty;
            item.Category = Category ?? string.Empty;

            // ----------------------------
            // Handle new image upload
            // ----------------------------
            if (ImageFile != null && ImageFile.Length > 0)
            {
                // Delete old image if exists
                if (!string.IsNullOrEmpty(item.ImagePath))
                {
                    var oldPath = Path.Combine(_env.WebRootPath, "uploads/items", item.ImagePath);
                    if (System.IO.File.Exists(oldPath))
                        System.IO.File.Delete(oldPath);
                }

                // Save new image
                var uploadsFolder = Path.Combine(_env.WebRootPath, "uploads/items");
                Directory.CreateDirectory(uploadsFolder);

                var imageFileName = $"{Guid.NewGuid()}{Path.GetExtension(ImageFile.FileName)}";
                var filePath = Path.Combine(uploadsFolder, imageFileName);

                await using var stream = new FileStream(filePath, FileMode.Create);
                await ImageFile.CopyToAsync(stream);

                item.ImagePath = imageFileName;
            }

            // ----------------------------
            // Update item in DB
            // ----------------------------
            await _mongo.UpdateItemAsync(item);

            // ----------------------------
            // Log report
            // ----------------------------
            var report = new Report
            {
                Type = "item_edit",
                PerformedBy = HttpContext.Session.GetString("username") ?? "Unknown",
                TargetName = item.Name,
                OldValue = oldValue,
                NewValue = $"Quantity: {item.Quantity}, Category: {item.Category}",
                Details = $"Edited item in category '{item.Category}'."
            };
            await _mongo.CreateReportAsync(report);

            // ----------------------------
            // Show success
            // ----------------------------
            SuccessMessage = $"Item '{item.Name}' updated successfully.";
            ExistingImagePath = item.ImagePath ?? string.Empty;

            return Page();
        }
    }
}
