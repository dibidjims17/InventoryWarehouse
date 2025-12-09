using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using MyRazorApp.Models;
using MyRazorApp.Services;

namespace MyRazorApp.Pages.Admin.Reports
{
    public class ReportsModel : PageModel
    {
        private readonly MongoService _mongo;

        public ReportsModel(MongoService mongo)
        {
            _mongo = mongo;
        }

        [BindProperty(SupportsGet = true)]
        public string ReportTypeFilter { get; set; } = "";

        public List<Report> Reports { get; set; } = new();
        public List<string> ReportTypes { get; set; } = new(); // for dropdown

        public async Task OnGetAsync()
        {
            // Fetch all report types for dropdown
            ReportTypes = await _mongo.GetDistinctReportTypesAsync();

            // Fetch latest 100 reports, optionally filtered by type
            Reports = await _mongo.GetReportsAsync(type: ReportTypeFilter, limit: 100);
        }

        // Helper methods for display
        public string FormatDate(DateTime dt) => dt.ToLocalTime().ToString("yyyy-MM-dd");
        public string FormatTime(DateTime dt) => dt.ToLocalTime().ToString("HH:mm:ss");
    }
}
