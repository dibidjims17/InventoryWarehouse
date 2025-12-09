using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;

namespace MyRazorApp.Models
{
    public class Report
    {
        [BsonId]
        [BsonRepresentation(BsonType.ObjectId)]
        public string? Id { get; set; }

        // General type of activity (item_add, user_edit, user_deactivate, login, etc.)
        [BsonElement("type")]
        public string Type { get; set; } = string.Empty;

        // Admin or user who performed the action
        [BsonElement("performedBy")]
        public string PerformedBy { get; set; } = string.Empty;

        // Affected entity (user or item name)
        [BsonElement("targetName")]
        public string? TargetName { get; set; }

        // Extra detail text
        [BsonElement("details")]
        public string Details { get; set; } = string.Empty;

        // When action happened
        [BsonElement("timestamp")]
        public DateTime Timestamp { get; set; } = DateTime.UtcNow;

        // OPTIONAL FIELDS (null when unused)
        [BsonElement("oldValue")]
        public string? OldValue { get; set; }

        [BsonElement("newValue")]
        public string? NewValue { get; set; }

        [BsonElement("isRead")]
        public bool? IsRead { get; set; }
        
    }
}
