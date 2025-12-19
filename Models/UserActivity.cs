// Models/UserActivity.cs
namespace MyRazorApp.Models
{
    public class UserActivity
    {
        public string Title { get; set; } = "";
        public string Message { get; set; } = "";
        public string PerformedBy { get; set; } = "";
        public DateTime Timestamp { get; set; } = DateTime.UtcNow;
        public string? CssClass { get; set; } = "";
        public string? Icon { get; set; } = "info-circle";
    }
}
