using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using MyRazorApp.Models;
using MyRazorApp.Services;

namespace MyRazorApp.Pages.Admin.BorrowLog
{
    public class ApproveReturnModel : PageModel
    {
        private readonly MongoService _mongo;

        public ApproveReturnModel(MongoService mongo)
        {
            _mongo = mongo;
        }

        [BindProperty(SupportsGet = true)]
        public string? Id { get; set; }

        [BindProperty]
        public Borrow Borrow { get; set; } = new Borrow();

        [BindProperty]
        public Dictionary<string, int> Conditions { get; set; } = new Dictionary<string, int>
        {
            { "Good", 0 },
            { "Damaged", 0 },
            { "Lost", 0 }
        };

        [BindProperty]
        public string? SuccessMessage { get; set; }

        public async Task<IActionResult> OnGetAsync()
        {
            var role = HttpContext.Session.GetString("role");
            if (role != "admin")
                return RedirectToPage("/AccessDenied");

            if (string.IsNullOrEmpty(Id))
                return RedirectToPage("/Admin/BorrowLog/Index");

            var borrow = await _mongo.GetAllBorrowsAsync();
            var foundBorrow = borrow.FirstOrDefault(b => b.Id == Id);
            if (foundBorrow == null)
                return RedirectToPage("/Admin/BorrowLog/Index");

            Borrow = foundBorrow;

            // Pre-fill existing conditions
            if (Borrow.ConditionsOnReturn != null)
            {
                foreach (var key in Conditions.Keys.ToList())
                {
                    if (Borrow.ConditionsOnReturn.ContainsKey(key))
                        Conditions[key] = Borrow.ConditionsOnReturn[key];
                }
            }

            return Page();
        }

        public async Task<IActionResult> OnPostAsync()
        {
            if (string.IsNullOrEmpty(Id))
                return RedirectToPage("/Admin/BorrowLog/Index");

            // Approve return
            await _mongo.ApproveReturnAsync(Id, Conditions);

            var updatedBorrow = (await _mongo.GetAllBorrowsAsync())
                .FirstOrDefault(b => b.Id == Id);

            if (updatedBorrow == null)
                return Page();

            var item = await _mongo.GetItemByIdAsync(updatedBorrow.ItemId!);
            if (item != null)
            {
                // Update stock
                int good = Conditions.GetValueOrDefault("Good", 0);
                int damaged = Conditions.GetValueOrDefault("Damaged", 0);
                int lost = Conditions.GetValueOrDefault("Lost", 0);

                item.Quantity += good;        // returned good items
                item.Quantity -= lost;        // lost items reduce stock

                await _mongo.UpdateItemAsync(item);

                // Build readable report sentence with borrower name
                var reportDetails = $"Approve the return of '{updatedBorrow.ItemName}'. " +
                                    $"{good} good, {damaged} damaged, {lost} lost. " +
                                    $"From user '{updatedBorrow.Username}'.";

                var report = new Report
                {
                    Type = "borrow_returned",
                    PerformedBy = HttpContext.Session.GetString("username") ?? "Unknown", // admin approving
                    TargetName = updatedBorrow.ItemName,
                    Details = reportDetails,
                    Timestamp = DateTime.UtcNow
                };

                await _mongo.CreateReportAsync(report);

                SuccessMessage = $"Return approved for '{updatedBorrow.ItemName}'!";
            }

            return Page();
        }

    }
}
