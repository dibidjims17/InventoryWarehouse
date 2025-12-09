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

        public async Task<Dictionary<string, int>> GetTopBorrowedItemsAsync(int top = 5)
        {
            var pipeline = new[]
            {
                new BsonDocument("$group", new BsonDocument
                {
                    { "_id", "$item_name" },
                    { "total", new BsonDocument("$sum", "$quantity") }
                }),
                new BsonDocument("$sort", new BsonDocument("total", -1)),
                new BsonDocument("$limit", top)
            };
            var result = await _borrows.AggregateAsync<BsonDocument>(pipeline);
            return (await result.ToListAsync())
                .ToDictionary(d => d["_id"].AsString, d => d["total"].AsInt32);
        }

        public async Task<Dictionary<string, int>> GetBorrowStatusTotalsAsync()
        {
            var pipeline = new[]
            {
                new BsonDocument("$group", new BsonDocument
                {
                    { "_id", "$status" },
                    { "count", new BsonDocument("$sum", 1) }
                })
            };
            var result = await _borrows.AggregateAsync<BsonDocument>(pipeline);
            return (await result.ToListAsync())
                .ToDictionary(d => d["_id"].AsString, d => d["count"].AsInt32);
        }

        public async Task<Dictionary<string, int>> GetReturnRequestTotalsAsync()
        {
            var pipeline = new[]
            {
                new BsonDocument("$group", new BsonDocument
                {
                    { "_id", new BsonDocument
                        {
                            { "return_requested", "$return_requested" },
                            { "status", "$status" }
                        }
                    },
                    { "count", new BsonDocument("$sum", 1) }
                })
            };

            var result = await _borrows.AggregateAsync<BsonDocument>(pipeline);
            var totals = new Dictionary<string, int> { { "Requested", 0 }, { "Approved", 0 }, { "Rejected", 0 } };

            await result.ForEachAsync(d =>
            {
                bool requested = d["_id"]["return_requested"].AsBoolean;
                string status = d["_id"]["status"].AsString;

                if (requested && status != "Returned") totals["Requested"] += d["count"].AsInt32;
                if (status == "Returned") totals["Approved"] += d["count"].AsInt32;
                if (status == "Rejected" && requested) totals["Rejected"] += d["count"].AsInt32;
            });

            return totals;
        }

        public async Task<Dictionary<string, int>> GetReturnConditionTotalsAsync()
        {
            var pipeline = new[]
            {
                new BsonDocument("$match", new BsonDocument
                {
                    { "status", "Returned" },
                    { "conditions_on_return", new BsonDocument("$ne", BsonNull.Value) }
                }),
                new BsonDocument("$group", new BsonDocument
                {
                    { "_id", BsonNull.Value },
                    { "Good", new BsonDocument("$sum", "$conditions_on_return.Good") },
                    { "Damaged", new BsonDocument("$sum", "$conditions_on_return.Damaged") },
                    { "Lost", new BsonDocument("$sum", "$conditions_on_return.Lost") }
                })
            };

            var result = await _borrows.AggregateAsync<BsonDocument>(pipeline);
            var totals = await result.FirstOrDefaultAsync();

            return new Dictionary<string, int>
            {
                { "Good", totals?["Good"].AsInt32 ?? 0 },
                { "Damaged", totals?["Damaged"].AsInt32 ?? 0 },
                { "Lost", totals?["Lost"].AsInt32 ?? 0 }
            };
        }

        // ------------------- USER ACTIVITY -------------------

        public async Task<Dictionary<string, int>> GetUserActivityTotalsAsync()
        {
            var allUsers = await _users.Find(FilterDefinition<User>.Empty).ToListAsync();
            var totals = new Dictionary<string, int>
            {
                { "Active", allUsers.Count(u => u.Status == "active") },
                { "Inactive", allUsers.Count(u => u.Status != "active") }
            };
            return totals;
        }

        // ------------------- LOW STOCK ITEMS -------------------

        public async Task<Dictionary<string, int>> GetLowStockItemsAsync(int threshold = 50)
        {
            var filter = Builders<Item>.Filter.Lte(i => i.Quantity, threshold);
            var items = await _items.Find(filter).ToListAsync();
            return items.ToDictionary(i => i.Name, i => i.Quantity);
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

        public async Task<Dictionary<string, List<object>>> GetBorrowStatusDetailsAsync()
        {
            var borrows = await _borrows.Find(FilterDefinition<Borrow>.Empty).ToListAsync();

            var result = new Dictionary<string, List<object>>();

            foreach (var b in borrows)
            {
                string status = b.Status ?? "Unknown";

                if (!result.ContainsKey(status))
                    result[status] = new List<object>();

                result[status].Add(new
                {
                    user = b.Username,
                    item = b.ItemName,
                    quantity = b.Quantity,
                    date = b.RequestedAt.ToString("yyyy-MM-dd")
                });
            }

            return result;
        }

        public async Task<Dictionary<string, List<object>>> GetReturnRequestDetailsAsync()
        {
            var borrows = await _borrows.Find(FilterDefinition<Borrow>.Empty).ToListAsync();

            var result = new Dictionary<string, List<object>>
            {
                { "Requested", new List<object>() },
                { "Approved", new List<object>() },
                { "Rejected", new List<object>() }
            };

            foreach (var b in borrows)
            {
                if (b.ReturnRequested && b.Status != "Returned")
                {
                    result["Requested"].Add(new
                    {
                        user = b.Username,
                        item = b.ItemName,
                        quantity = b.Quantity,
                        date = b.RequestedAt.ToString("yyyy-MM-dd")
                    });
                }

                if (b.Status == "Returned")
                {
                    result["Approved"].Add(new
                    {
                        user = b.Username,
                        item = b.ItemName,
                        date = b.ReturnedAt?.ToString("yyyy-MM-dd")
                    });
                }

                if (b.Status == "Rejected" && b.ReturnRequested)
                {
                    result["Rejected"].Add(new
                    {
                        user = b.Username,
                        item = b.ItemName,
                        date = b.RequestedAt.ToString("yyyy-MM-dd")
                    });
                }
            }

            return result;
        }

        public async Task<Dictionary<string, List<object>>> GetReturnConditionDetailsAsync()
        {
            var filter = Builders<Borrow>.Filter.Eq(b => b.Status, "Returned");
            var borrows = await _borrows.Find(filter).ToListAsync();

            var result = new Dictionary<string, List<object>>
            {
                { "Good", new List<object>() },
                { "Damaged", new List<object>() },
                { "Lost", new List<object>() }
            };

            foreach (var b in borrows)
            {
                if (b.ConditionsOnReturn == null) continue;

                // Safely format date as "YYYY-MM-DD"
                var returnDate = b.ReturnedAt?.ToString("yyyy-MM-dd") ?? "";

                // "Good"
                if (b.ConditionsOnReturn.TryGetValue("Good", out int goodQty) && goodQty > 0)
                {
                    result["Good"].Add(new
                    {
                        user = b.Username,
                        item = b.ItemName,
                        qty = goodQty,
                        date = returnDate // <-- use the formatted string
                    });
                }

                // "Damaged"
                if (b.ConditionsOnReturn.TryGetValue("Damaged", out int damagedQty) && damagedQty > 0)
                {
                    result["Damaged"].Add(new
                    {
                        user = b.Username,
                        item = b.ItemName,
                        qty = damagedQty,
                        date = returnDate
                    });
                }

                // "Lost"
                if (b.ConditionsOnReturn.TryGetValue("Lost", out int lostQty) && lostQty > 0)
                {
                    result["Lost"].Add(new
                    {
                        user = b.Username,
                        item = b.ItemName,
                        qty = lostQty,
                        date = returnDate
                    });
                }
            }

            return result;
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
