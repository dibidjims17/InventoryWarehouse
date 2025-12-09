using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using MyRazorApp.Services;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace MyRazorApp.Pages.Admin.DataReport
{
    public class IndexModel : PageModel
    {
        private readonly MongoService _mongoService;

        public IndexModel(MongoService mongoService)
        {
            _mongoService = mongoService;
        }

        // ----------- CHART TOTALS ------------
        public Dictionary<string, int> TopBorrowedItems { get; set; } = new();
        public Dictionary<string, int> BorrowStatusTotals { get; set; } = new();
        public Dictionary<string, int> ReturnRequestTotals { get; set; } = new();
        public Dictionary<string, int> ReturnConditionTotals { get; set; } = new();
        public Dictionary<string, int> UserActivityTotals { get; set; } = new();
        public Dictionary<string, int> LowStockItems { get; set; } = new();

        // ----------- DETAIL TABLES ------------
        public Dictionary<string, List<object>> BorrowStatusDetails { get; set; } = new();
        public Dictionary<string, List<object>> ReturnRequestDetails { get; set; } = new();
        public Dictionary<string, List<object>> ReturnConditionDetails { get; set; } = new();

        public async Task OnGetAsync()
        {
            // ------- LOAD TOTALS -------
            TopBorrowedItems = await _mongoService.GetTopBorrowedItemsAsync();
            BorrowStatusTotals = await _mongoService.GetBorrowStatusTotalsAsync();
            ReturnRequestTotals = await _mongoService.GetReturnRequestTotalsAsync();
            ReturnConditionTotals = await _mongoService.GetReturnConditionTotalsAsync();
            UserActivityTotals = await _mongoService.GetUserActivityTotalsAsync();
            LowStockItems = await _mongoService.GetLowStockItemsAsync();

            // ------- LOAD DETAILS -------
            BorrowStatusDetails = await _mongoService.GetBorrowStatusDetailsAsync();
            ReturnRequestDetails = await _mongoService.GetReturnRequestDetailsAsync();
            ReturnConditionDetails = await _mongoService.GetReturnConditionDetailsAsync();
        }

        // OPTIONAL — TOP ITEMS BY MONTH (unchanged)
        public async Task<JsonResult> OnGetMonthDataAsync(int month)
        {
            if (month < 1 || month > 12)
                month = DateTime.Now.Month;

            int year = DateTime.Now.Year;

            var top10 = await _mongoService.GetTopBorrowedItemsByMonthAsync(month, year, 10);
            var fullList = await _mongoService.GetFullBorrowListByMonthAsync(month, year);

            return new JsonResult(new
            {
                top10 = top10,
                fullList = fullList
            });
        }
    }
}
