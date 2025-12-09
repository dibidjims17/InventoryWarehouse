using Microsoft.AspNetCore.Mvc.RazorPages;
using MyRazorApp.Models;
using MyRazorApp.Services;
using MongoDB.Bson;

namespace MyRazorApp.Pages.Admin.Inventory
{
    public class IndexModel : PageModel
    {
        private readonly MongoService _mongoService;
        public List<Item> Items { get; set; } = new List<Item>();

        public IndexModel(MongoService mongoService)
        {
            _mongoService = mongoService;
        }

        public void OnGet()
        {
            // Fetch all items from Mongo
            Items = _mongoService.GetAllItemsAsync().Result;
        }
    }
}
