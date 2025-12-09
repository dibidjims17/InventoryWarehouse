namespace MyRazorApp.Models
{
    public class BorrowRecord
    {
        public string Username { get; set; } = "";
        public string ItemName { get; set; } = "";
        public int Quantity { get; set; }
        public DateTime Date { get; set; }
        public string Status { get; set; } = "";
    }

    public class BorrowTrend
    {
        public DateTime Date { get; set; }
        public int Count { get; set; }
    }
}
