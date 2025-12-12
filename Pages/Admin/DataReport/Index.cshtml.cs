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

        // Properties for initial page load (Server Side Rendering)
        public Dictionary<string, int> TopBorrowedItems { get; set; } = new();
        public Dictionary<string, int> BorrowStatusTotals { get; set; } = new();
        public Dictionary<string, int> ReturnRequestTotals { get; set; } = new();
        public Dictionary<string, int> ReturnConditionTotals { get; set; } = new();
        public Dictionary<string, int> UserActivityTotals { get; set; } = new();
        public Dictionary<string, int> LowStockItems { get; set; } = new();

        public Dictionary<string, List<object>> BorrowStatusDetails { get; set; } = new();
        public Dictionary<string, List<object>> ReturnRequestDetails { get; set; } = new();
        public Dictionary<string, List<object>> ReturnConditionDetails { get; set; } = new();

        public async Task OnGetAsync()
        {
            // Initial Load: Defaults to "All Time" (pass nulls)
            await LoadData(null, null);
        }

        // ---------------------------------------------------------
        // NEW: Central Handler for the Global Filter
        // ---------------------------------------------------------
        public async Task<JsonResult> OnGetDashboardDataAsync(string view, int? month, int? year)
        {
            // If view is 'all', we pass nulls to the service. 
            // If view is 'month', we pass the specific values.
            int? m = (view == "month") ? month : null;
            int? y = (view == "month") ? year : null;

            await LoadData(m, y);

            // Return everything as JSON to the frontend
            return new JsonResult(new
            {
                // Charts
                topBorrowed = TopBorrowedItems,
                borrowStatus = BorrowStatusTotals,
                returnRequests = ReturnRequestTotals,
                returnConditions = ReturnConditionTotals,
                userActivity = UserActivityTotals,
                lowStock = LowStockItems,

                // Tables
                details_borrow = BorrowStatusDetails,
                details_return = ReturnRequestDetails,
                details_condition = ReturnConditionDetails
            });
        }

        // Helper to avoid duplicating code between OnGet and OnGetDashboardData
        private async Task LoadData(int? month, int? year)
        {
            // NOTE: You must update your MongoService methods to accept (int? month, int? year)
            // If month/year are null, the service should return "All Time" data.
            
            TopBorrowedItems = await _mongoService.GetTopBorrowedItemsAsync(month, year);
            BorrowStatusTotals = await _mongoService.GetBorrowStatusTotalsAsync(month, year);
            ReturnRequestTotals = await _mongoService.GetReturnRequestTotalsAsync(month, year);
            ReturnConditionTotals = await _mongoService.GetReturnConditionTotalsAsync(month, year);
            UserActivityTotals = await _mongoService.GetUserActivityTotalsAsync(month, year);
            
            // Low stock is usually "Current State", so it often ignores date filters, 
            // but we reload it just in case logic changes.
            LowStockItems = await _mongoService.GetLowStockItemsAsync(); 

            // Details
            BorrowStatusDetails = await _mongoService.GetBorrowStatusDetailsAsync(month, year);
            ReturnRequestDetails = await _mongoService.GetReturnRequestDetailsAsync(month, year);
            ReturnConditionDetails = await _mongoService.GetReturnConditionDetailsAsync(month, year);
        }
    }
}