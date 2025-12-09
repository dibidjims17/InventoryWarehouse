using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using MyRazorApp.Models;
using MyRazorApp.Services;

namespace MyRazorApp.Pages.Admin.Users
{
    public class IndexModel : PageModel
    {
        private readonly MongoService _mongo;

        public IndexModel(MongoService mongo)
        {
            _mongo = mongo;
        }

        [BindProperty(SupportsGet = true)]
        public string Search { get; set; } = string.Empty;

        public List<User> Users { get; set; } = new();

        public string CurrentUsername { get; set; } = string.Empty;

        public List<string> Roles { get; set; } = new() { "admin", "employee" };

        public async Task OnGetAsync()
        {
            CurrentUsername = HttpContext.Session.GetString("username") ?? "";

            var allUsers = await _mongo.GetAllUsersAsync();

            if (!string.IsNullOrWhiteSpace(Search))
            {
                Search = Search.Trim().ToLower();
                Users = allUsers.Where(u =>
                    (u.Username?.ToLower().Contains(Search) ?? false) ||
                    (u.Email?.ToLower().Contains(Search) ?? false) ||
                    (u.Role?.ToLower().Contains(Search) ?? false) ||
                    (u.Status?.ToLower().Contains(Search) ?? false) ||
                    u.LastLogin != default &&
                    u.LastLogin.ToString("yyyy-MM-dd HH:mm:ss").ToLower().Contains(Search)
                ).OrderBy(u => u.Username).ToList();
            }
            else
            {
                Users = allUsers.OrderBy(u => u.Username).ToList();
            }
        }
    }
}
