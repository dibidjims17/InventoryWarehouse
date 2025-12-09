using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;
using System;
using System.Collections.Generic;

namespace MyRazorApp.Models
{
    public class Borrow
    {
        [BsonId]
        [BsonRepresentation(BsonType.ObjectId)]
        public string? Id { get; set; }

        [BsonElement("user_id")]
        public string UserId { get; set; } = string.Empty;

        [BsonElement("username")]
        public string Username { get; set; } = string.Empty;

        [BsonElement("item_id")]
        public string ItemId { get; set; } = string.Empty;

        [BsonElement("item_code")]
        public string ItemCode { get; set; } = string.Empty;

        [BsonElement("item_name")]
        public string ItemName { get; set; } = string.Empty;

        [BsonElement("item_category")]
        public string ItemCategory { get; set; } = string.Empty;

        [BsonElement("quantity")]
        public int Quantity { get; set; }

        [BsonElement("status")]
        public string Status { get; set; } = "Pending"; // Pending, Approved, Rejected, Returned

        [BsonElement("requested_at")]
        public DateTime RequestedAt { get; set; } = DateTime.UtcNow;

        [BsonElement("returned")]
        public bool Returned { get; set; } = false;

        [BsonElement("return_requested")]
        public bool ReturnRequested { get; set; } = false;

        [BsonElement("returned_at")]
        public DateTime? ReturnedAt { get; set; } // When the borrow was returned

        [BsonElement("conditions_on_return")]
        public Dictionary<string, int>? ConditionsOnReturn { get; set; } // e.g., Good, Damaged, Lost
    }
}
