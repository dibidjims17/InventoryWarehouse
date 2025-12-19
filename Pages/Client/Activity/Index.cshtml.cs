using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.Mvc;
using MyRazorApp.Models;
using MyRazorApp.Services;
using Microsoft.AspNetCore.Http;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace MyRazorApp.Pages.Client.Activity
{
    public class IndexModel : PageModel
    {
        private readonly MongoService _mongo;

        public IndexModel(MongoService mongo)
        {
            _mongo = mongo;
        }

        // Add [BindProperty(SupportsGet = true)] so the dropdown posts back correctly
        [BindProperty(SupportsGet = true)]
        public string? ActivityTypeFilter { get; set; }

        public List<Report> Reports { get; set; } = new List<Report>();

        public List<string> ActivityTypes { get; set; } = new List<string>();

        private readonly string[] ClientAllowedTypes = new string[]
        {
            "user_login",
            "user_logout",
            "borrow",
            "borrow_approved",
            "borrow_rejected",
            "borrow_return_request",
            "borrow_returned"
        };

        public async Task OnGetAsync()
        {
            var username = HttpContext.Session.GetString("username") ?? "";

            var allTypes = await _mongo.GetDistinctReportTypesAsync();
            ActivityTypes = allTypes.Where(t => ClientAllowedTypes.Contains(t))
                                    .OrderBy(t => t)
                                    .ToList();

            // Get reports filtered by type (if selected)
            Reports = await _mongo.GetReportsForUserAsync(username, ActivityTypeFilter, 100);
        }

        public string FormatDate(DateTime dt) => dt.ToLocalTime().ToString("yyyy-MM-dd");
        public string FormatTime(DateTime dt) => dt.ToLocalTime().ToString("HH:mm:ss");
    }
}
