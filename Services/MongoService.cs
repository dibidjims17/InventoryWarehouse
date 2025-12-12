using MongoDB.Bson;
using MongoDB.Driver;
using MyRazorApp.Helpers;
using MyRazorApp.Models;

namespace MyRazorApp.Services
{
    public class MongoService
    {
        private readonly IMongoCollection<User> _users;
        private readonly IMongoCollection<Item> _items;
        private readonly IMongoCollection<Report> _reports;
        private readonly IMongoCollection<Borrow> _borrows;

        public MongoService(IConfiguration config)
        {
            var client = new MongoClient(config["MongoDB:ConnectionString"]);
            var database = client.GetDatabase(config["MongoDB:Database"]);

            _users = database.GetCollection<User>("users");
            _items = database.GetCollection<Item>("items");
            _reports = database.GetCollection<Report>("reports");
            _borrows = database.GetCollection<Borrow>("borrows");
        }

        // ----------------------------
        // USER METHODS
        // ----------------------------
        public async Task<List<User>> GetAllUsersAsync() =>
            await _users.Find(FilterDefinition<User>.Empty)
                        .SortBy(u => u.Username)
                        .ToListAsync();

        public async Task<User?> GetUserByUsernameAsync(string username) =>
            await _users.Find(u => u.Username == username).FirstOrDefaultAsync();

        public async Task<User?> GetUserByEmailAsync(string email) =>
            await _users.Find(u => u.Email == email).FirstOrDefaultAsync();

        public async Task<User?> GetUserByIdAsync(string userId) =>
            await _users.Find(u => u.UserId == userId).FirstOrDefaultAsync();

        public async Task<User?> GetUserByUsernameOrEmailAsync(string input) =>
            await _users.Find(u => u.Username == input || u.Email == input).FirstOrDefaultAsync();

        public async Task<User?> GetUserBySessionTokenAsync(string token)
        {
            return await _users.Find(u => u.SessionToken == token).FirstOrDefaultAsync();
        }

        public async Task CreateUserAsync(User user) =>
            await _users.InsertOneAsync(user);

        public async Task<User?> GetUserByTokenAsync(string token) =>
            await _users.Find(u => u.SessionToken == token).FirstOrDefaultAsync();

        // ----------------------------
        // ITEM METHODS
        // ----------------------------
        public async Task<List<Item>> GetAllItemsAsync() =>
            await _items.Find(FilterDefinition<Item>.Empty).ToListAsync();

        public async Task<Item?> GetItemByIdAsync(string id) =>
            await _items.Find(i => i.Id == id).FirstOrDefaultAsync();

        public async Task CreateItemAsync(Item item, string? performedBy = null)
        {
            await _items.InsertOneAsync(item);

            if (!string.IsNullOrEmpty(performedBy))
            {
                await CreateReportAsync(new Report
                {
                    Type = "item_add",
                    PerformedBy = performedBy,
                    TargetName = item.Name,
                    Details = $"Added new item '{item.Name}' in category '{item.Category}' with quantity {item.Quantity}."
                });
            }
        }

        public async Task UpdateItemAsync(Item item, string? performedBy = null)
        {
            if (string.IsNullOrEmpty(item.Id))
                throw new ArgumentException("Item ID cannot be null for update.");

            var oldItem = await GetItemByIdAsync(item.Id);

            await _items.ReplaceOneAsync(i => i.Id == item.Id, item);

            if (!string.IsNullOrEmpty(performedBy) && oldItem != null)
            {
                await CreateReportAsync(new Report
                {
                    Type = "item_edit",
                    PerformedBy = performedBy,
                    TargetName = item.Name,
                    Details = $"Item '{item.Name}' updated from quantity {oldItem.Quantity} (category {oldItem.Category}) to quantity {item.Quantity} (category {item.Category})."
                });
            }
        }

        public async Task DeleteItemAsync(string id, string? performedBy = null)
        {
            var item = await GetItemByIdAsync(id);
            if (item != null)
            {
                await _items.DeleteOneAsync(i => i.Id == id);

                if (!string.IsNullOrEmpty(performedBy))
                {
                    await CreateReportAsync(new Report
                    {
                        Type = "item_delete",
                        PerformedBy = performedBy,
                        TargetName = item.Name,
                        Details = $"Item '{item.Name}' in category '{item.Category}' was deleted."
                    });
                }
            }
        }

        // ----------------------------
        // USER METHODS WITH REPORTS
        // ----------------------------
        public async Task UpdateUserAsync(User user, string? performedBy = null)
        {
            var oldUser = await GetUserByIdAsync(user.UserId);
            if (oldUser == null) throw new Exception("User not found.");

            await _users.ReplaceOneAsync(u => u.UserId == user.UserId, user);

            if (!string.IsNullOrEmpty(performedBy))
            {
                await CreateReportAsync(new Report
                {
                    Type = "user_edit",
                    PerformedBy = performedBy,
                    TargetName = user.Username,
                    Details = $"User '{user.Username}' updated from Role '{oldUser.Role}' / Status '{oldUser.Status}' to Role '{user.Role}' / Status '{user.Status}'."
                });
            }
        }

        public async Task DeactivateUserAsync(string userId, string performedBy)
        {
            var user = await GetUserByIdAsync(userId);
            if (user == null) return;

            user.Status = "deactivated";
            await _users.ReplaceOneAsync(u => u.UserId == userId, user);

            await CreateReportAsync(new Report
            {
                Type = "user_deactivate",
                PerformedBy = performedBy,
                TargetName = user.Username,
                Details = $"User '{user.Username}' was deactivated."
            });
        }

        public async Task ActivateUserAsync(string userId, string performedBy)
        {
            var user = await GetUserByIdAsync(userId);
            if (user == null) return;

            user.Status = "active";
            await _users.ReplaceOneAsync(u => u.UserId == userId, user);

            await CreateReportAsync(new Report
            {
                Type = "user_activate",
                PerformedBy = performedBy,
                TargetName = user.Username,
                Details = $"User '{user.Username}' was activated."
            });
        }

        // ----------------------------
        // REPORT METHODS
        // ----------------------------
        public async Task<List<Report>> GetReportsAsync(
            string? type = null,
            string? performedBy = null,
            DateTime? startDate = null,
            DateTime? endDate = null,
            int skip = 0,
            int limit = 50)
        {
            var filterBuilder = Builders<Report>.Filter;
            var filter = filterBuilder.Empty;

            if (!string.IsNullOrEmpty(type))
                filter &= filterBuilder.Eq(r => r.Type, type);

            if (!string.IsNullOrEmpty(performedBy))
                filter &= filterBuilder.Eq(r => r.PerformedBy, performedBy);

            if (startDate.HasValue)
                filter &= filterBuilder.Gte(r => r.Timestamp, startDate.Value);

            if (endDate.HasValue)
                filter &= filterBuilder.Lte(r => r.Timestamp, endDate.Value);

            return await _reports.Find(filter)
                                 .SortByDescending(r => r.Timestamp)
                                 .Skip(skip)
                                 .Limit(limit)
                                 .ToListAsync();
        }

        public async Task CreateReportAsync(Report report)
        {
            report.Id = null; // MongoDB generates ObjectId
            if (report.Timestamp == default) report.Timestamp = DateTime.UtcNow;

            await _reports.InsertOneAsync(report);
        }

        // Get all distinct report types
        public async Task<List<string>> GetDistinctReportTypesAsync()
        {
            var types = await _reports.Distinct<string>("Type", FilterDefinition<Report>.Empty).ToListAsync();
            return types.OrderBy(t => t).ToList();
        }

        // ----------------------------
        // BORROW METHODS
        // ----------------------------
        public async Task<Borrow?> GetPendingBorrowAsync(string userId, string itemId) =>
            await _borrows.Find(b => b.UserId == userId && b.ItemId == itemId && b.Status == "Pending")
                          .FirstOrDefaultAsync();

        public async Task CreateBorrowAsync(Borrow borrow) =>
            await _borrows.InsertOneAsync(borrow);

        public async Task<List<Borrow>> GetBorrowsByUserAsync(string userId) =>
            await _borrows.Find(b => b.UserId == userId)
                          .SortByDescending(b => b.RequestedAt)
                          .ToListAsync();

        public async Task<List<Borrow>> GetAllBorrowsAsync() =>
            await _borrows.Find(FilterDefinition<Borrow>.Empty)
                          .SortByDescending(b => b.RequestedAt)
                          .ToListAsync();

        public async Task UpdateBorrowStatusAsync(string id, string newStatus)
        {
            var filter = Builders<Borrow>.Filter.Eq(b => b.Id, id);
            var update = Builders<Borrow>.Update.Set(b => b.Status, newStatus);
            await _borrows.UpdateOneAsync(filter, update);
        }

        public async Task ApproveReturnAsync(string borrowId, Dictionary<string, int> conditions)
        {
            var filter = Builders<Borrow>.Filter.Eq(b => b.Id, borrowId);
            var update = Builders<Borrow>.Update
                .Set(b => b.Status, "Returned")
                .Set(b => b.ConditionsOnReturn, conditions)
                .Set(b => b.ReturnedAt, DateTime.UtcNow);

            await _borrows.UpdateOneAsync(filter, update);
        }

        public async Task UpdateBorrowAsync(Borrow borrow)
        {
            if (string.IsNullOrEmpty(borrow.Id))
                throw new ArgumentException("Borrow ID cannot be null.");

            var filter = Builders<Borrow>.Filter.Eq(b => b.Id, borrow.Id);
            await _borrows.ReplaceOneAsync(filter, borrow);
        }

        // ------------------- BORROW ANALYTICS -------------------

        // 1. TOP BORROWED ITEMS
        public async Task<Dictionary<string, int>> GetTopBorrowedItemsAsync(int? month, int? year)
        {
            var filter = GetDateFilter(month, year);
            
            // Aggregate: Match Date -> Group by ItemName -> Sum Quantity -> Sort -> Limit 5
            var result = await _borrows.Aggregate()
                .Match(filter)
                .Group(x => x.ItemName, g => new { Name = g.Key, Total = g.Sum(x => x.Quantity) })
                .SortByDescending(x => x.Total)
                .Limit(5)
                .ToListAsync();

            return result.ToDictionary(x => x.Name, x => x.Total);
        }

        // 2. BORROW STATUS TOTALS
        public async Task<Dictionary<string, int>> GetBorrowStatusTotalsAsync(int? month, int? year)
        {
            var filter = GetDateFilter(month, year);

            var result = await _borrows.Aggregate()
                .Match(filter)
                .Group(x => x.Status, g => new { Status = g.Key, Count = g.Count() })
                .ToListAsync();

            return result.ToDictionary(x => x.Status, x => (int)x.Count);
        }

        // 3. RETURN REQUEST TOTALS (Items where ReturnRequested is true)
        public async Task<Dictionary<string, int>> GetReturnRequestTotalsAsync(int? month, int? year)
        {
            var dateFilter = GetDateFilter(month, year);
            var requestFilter = Builders<Borrow>.Filter.Eq(x => x.ReturnRequested, true);
            
            // Combine filters
            var combinedFilter = Builders<Borrow>.Filter.And(dateFilter, requestFilter);

            // Group by Status (e.g. "Pending Return" vs "Returned")
            var result = await _borrows.Aggregate()
                .Match(combinedFilter)
                .Group(x => x.Status, g => new { Status = g.Key, Count = g.Count() })
                .ToListAsync();

            return result.ToDictionary(x => x.Status, x => (int)x.Count);
        }

        // 4. RETURN CONDITION TOTALS
        public async Task<Dictionary<string, int>> GetReturnConditionTotalsAsync(int? month, int? year)
        {
            var filter = GetDateFilter(month, year);
            
            // Fetch all borrows that have conditions
            var borrows = await _borrows.Find(filter).ToListAsync();

            var totals = new Dictionary<string, int>();

            // Process logic in memory (easier than Mongo Aggregate for Dictionaries)
            foreach (var b in borrows)
            {
                if (b.ConditionsOnReturn != null)
                {
                    foreach (var kvp in b.ConditionsOnReturn)
                    {
                        if (totals.ContainsKey(kvp.Key))
                            totals[kvp.Key] += kvp.Value;
                        else
                            totals.Add(kvp.Key, kvp.Value);
                    }
                }
            }
            return totals;
        }

        // 5. USER ACTIVITY (Top Users)
        public async Task<Dictionary<string, int>> GetUserActivityTotalsAsync(int? month, int? year)
        {
            var filter = GetDateFilter(month, year);

            var result = await _borrows.Aggregate()
                .Match(filter)
                .Group(x => x.Username, g => new { User = g.Key, Count = g.Count() })
                .SortByDescending(x => x.Count)
                .Limit(5)
                .ToListAsync();

            return result.ToDictionary(x => x.User, x => (int)x.Count);
        }

        // ------------------- LOW STOCK ITEMS -------------------

        public async Task<Dictionary<string, int>> GetLowStockItemsAsync(int threshold = 50)
        {
            var filter = Builders<Item>.Filter.Lte(i => i.Quantity, threshold);
            var items = await _items.Find(filter).ToListAsync();
            return items.ToDictionary(i => i.Name, i => i.Quantity);
        }

        // Helper to build the date filter
        private FilterDefinition<Borrow> GetDateFilter(int? month, int? year)
        {
            var builder = Builders<Borrow>.Filter;
            
            // If no filter selected, return "Empty" (matches everything)
            if (!month.HasValue || !year.HasValue)
            {
                return builder.Empty;
            }

            // Construct date range for that specific month
            var startDate = new DateTime(year.Value, month.Value, 1, 0, 0, 0, DateTimeKind.Utc);
            var endDate = startDate.AddMonths(1);

            // Filter: RequestedAt >= StartDate AND RequestedAt < EndDate
            return builder.Gte(x => x.RequestedAt, startDate) & builder.Lt(x => x.RequestedAt, endDate);
        }

        // ------------------- MOST BORROWED ITEMS BY MONTH -------------------
        public async Task<Dictionary<string, int>> GetTopBorrowedItemsByMonthAsync(int month, int year, int top = 10)
        {
            // Compute the first and last day of the month
            var monthStart = new DateTime(year, month, 1);
            var monthEnd = monthStart.AddMonths(1).AddSeconds(-1);

            // Filter borrows within that month
            var filterBuilder = Builders<Borrow>.Filter;
            var filter = filterBuilder.Gte(b => b.RequestedAt, monthStart) &
                        filterBuilder.Lte(b => b.RequestedAt, monthEnd);

            // Fetch all borrows in the month
            var borrowsInMonth = await _borrows.Find(filter).ToListAsync();

            // Aggregate quantities per item
            var fullList = new Dictionary<string, int>();
            foreach (var b in borrowsInMonth)
            {
                if (!string.IsNullOrEmpty(b.ItemName) && b.Quantity > 0)
                {
                    if (fullList.ContainsKey(b.ItemName))
                        fullList[b.ItemName] += b.Quantity;
                    else
                        fullList[b.ItemName] = b.Quantity;
                }
            }

            // Sort descending
            var sorted = fullList.OrderByDescending(kv => kv.Value).ToDictionary(kv => kv.Key, kv => kv.Value);

            // Return top N
            return sorted.Take(top).ToDictionary(kv => kv.Key, kv => kv.Value);
        }

        // Optional: return full list for the month (without top limit)
        public async Task<Dictionary<string, int>> GetFullBorrowListByMonthAsync(int month, int year)
        {
            var monthStart = new DateTime(year, month, 1);
            var monthEnd = monthStart.AddMonths(1).AddSeconds(-1);

            var filterBuilder = Builders<Borrow>.Filter;
            var filter = filterBuilder.Gte(b => b.RequestedAt, monthStart) &
                        filterBuilder.Lte(b => b.RequestedAt, monthEnd);

            var borrowsInMonth = await _borrows.Find(filter).ToListAsync();

            var fullList = new Dictionary<string, int>();
            foreach (var b in borrowsInMonth)
            {
                if (!string.IsNullOrEmpty(b.ItemName) && b.Quantity > 0)
                {
                    if (fullList.ContainsKey(b.ItemName))
                        fullList[b.ItemName] += b.Quantity;
                    else
                        fullList[b.ItemName] = b.Quantity;
                }
            }

            return fullList.OrderByDescending(kv => kv.Value).ToDictionary(kv => kv.Key, kv => kv.Value);
        }

        // ------------------- TO SEE THE USER ACTIVITY -------------------

        // 6. BORROW STATUS DETAILS
        public async Task<Dictionary<string, List<object>>> GetBorrowStatusDetailsAsync(int? month, int? year)
        {
            var filter = GetDateFilter(month, year);
            var list = await _borrows.Find(filter).SortByDescending(x => x.RequestedAt).Limit(50).ToListAsync();

            var dict = new Dictionary<string, List<object>>();
            var rowList = new List<object>();

            foreach (var item in list)
            {
                rowList.Add(new {
                    user = item.Username,
                    item = item.ItemName,
                    status = item.Status,
                    date = item.RequestedAt.ToString("yyyy-MM-dd")
                });
            }

            // We put everything in one key "all" because the front end flattens it anyway
            dict.Add("all", rowList); 
            return dict;
        }

        // 7. RETURN REQUEST DETAILS
        public async Task<Dictionary<string, List<object>>> GetReturnRequestDetailsAsync(int? month, int? year)
        {
            var dateFilter = GetDateFilter(month, year);
            var reqFilter = Builders<Borrow>.Filter.Eq(x => x.ReturnRequested, true);
            var combined = Builders<Borrow>.Filter.And(dateFilter, reqFilter);

            var list = await _borrows.Find(combined).SortByDescending(x => x.RequestedAt).Limit(50).ToListAsync();
            
            var rowList = new List<object>();
            foreach (var item in list)
            {
                rowList.Add(new {
                    user = item.Username,
                    item = item.ItemName,
                    type = item.Status, // or "Return Requested"
                    date = item.RequestedAt.ToString("yyyy-MM-dd")
                });
            }

            return new Dictionary<string, List<object>> { { "all", rowList } };
        }

        // 8. RETURN CONDITION DETAILS
        public async Task<Dictionary<string, List<object>>> GetReturnConditionDetailsAsync(int? month, int? year)
        {
            var filter = GetDateFilter(month, year);
            // Get items that actually have conditions recorded
            var list = await _borrows.Find(filter).ToListAsync(); 

            var rowList = new List<object>();

            foreach (var item in list)
            {
                if (item.ConditionsOnReturn != null && item.ConditionsOnReturn.Count > 0)
                {
                    // Create a comma-separated string of issues (e.g., "Damaged (1), Lost (1)")
                    var conditionsString = string.Join(", ", item.ConditionsOnReturn.Select(x => $"{x.Key} ({x.Value})"));
                    
                    rowList.Add(new {
                        item = item.ItemName,
                        extra = conditionsString, // 'extra' maps to Condition column in JS
                        date = item.ReturnedAt.HasValue ? item.ReturnedAt.Value.ToString("yyyy-MM-dd") : "-"
                    });
                }
            }

            return new Dictionary<string, List<object>> { { "all", rowList } };
        }

        // ----------------------------
        // DASHBOARD METHODS (unchanged)
        // ----------------------------
        public async Task<long> UsersCountAsync() =>
            await _users.CountDocumentsAsync(FilterDefinition<User>.Empty);

        public async Task<long> ItemsCountAsync() =>
            await _items.CountDocumentsAsync(FilterDefinition<Item>.Empty);

        public async Task<List<BorrowRecord>> GetRecentBorrowsAsync(int limit = 5)
        {
            var borrows = await _borrows.Find(FilterDefinition<Borrow>.Empty)
                                        .SortByDescending(b => b.RequestedAt)
                                        .Limit(limit)
                                        .ToListAsync();

            return borrows.Select(b => new BorrowRecord
            {
                Username = b.Username ?? "",
                ItemName = b.ItemName ?? "",
                Quantity = b.Quantity,
                Date = b.RequestedAt,
                Status = b.Status ?? ""
            }).ToList();
        }

        public async Task<List<BorrowRecord>> GetRecentBorrowsByUserAsync(string userId, int limit = 5)
        {
            var borrows = await _borrows.Find(b => b.UserId == userId)
                                        .SortByDescending(b => b.RequestedAt)
                                        .Limit(limit)
                                        .ToListAsync();

            return borrows.Select(b => new BorrowRecord
            {
                Username = b.Username ?? "",
                ItemName = b.ItemName ?? "",
                Quantity = b.Quantity,
                Date = b.RequestedAt,
                Status = b.Status ?? ""
            }).ToList();
        }

        public async Task<List<BorrowTrend>> GetBorrowsLastNDaysAsync(int days, string? userId = null)
        {
            var startDate = DateTime.UtcNow.Date.AddDays(-days + 1);

            FilterDefinition<Borrow> filter = Builders<Borrow>.Filter.Gte(b => b.RequestedAt, startDate);
            if (!string.IsNullOrEmpty(userId))
                filter &= Builders<Borrow>.Filter.Eq(b => b.UserId, userId);

            var borrows = await _borrows.Find(filter).ToListAsync();

            var trends = borrows.GroupBy(b => b.RequestedAt.Date)
                                .Select(g => new BorrowTrend
                                {
                                    Date = g.Key,
                                    Count = g.Sum(b => b.Quantity)
                                })
                                .OrderBy(t => t.Date)
                                .ToList();

            var fullTrends = new List<BorrowTrend>();
            for (int i = 0; i < days; i++)
            {
                var date = startDate.AddDays(i);
                var trend = trends.FirstOrDefault(t => t.Date == date);
                fullTrends.Add(new BorrowTrend
                {
                    Date = date,
                    Count = trend?.Count ?? 0
                });
            }

            return fullTrends;
        }
        // MongoService.cs
        public async Task<int> GetOverdueBorrowsAsync(int overdueDays = 7)
        {
            var cutoffDate = DateTime.UtcNow.AddDays(-overdueDays);

            // Filter: borrows requested before cutoff and not returned
            var filter = Builders<Borrow>.Filter.Lt(b => b.RequestedAt, cutoffDate) &
                        Builders<Borrow>.Filter.Ne(b => b.Status, "Returned");

            return (int)await _borrows.CountDocumentsAsync(filter);
        }

        public async Task<int> GetPendingUsersAsync()
        {
            var filter = Builders<User>.Filter.Eq(u => u.Status, "pending");
            return (int)await _users.CountDocumentsAsync(filter);
        }

        public async Task<int> GetPendingBorrowsByUserAsync(string userId)
        {
            var pending = await _borrows.CountDocumentsAsync(b => b.UserId == userId && b.Status == "Pending");
            return (int)pending;
        }


    }
}
