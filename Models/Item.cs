using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;

namespace MyRazorApp.Models
{
    public class Item
    {
        [BsonId]
        [BsonRepresentation(BsonType.ObjectId)]
        public string? Id { get; set; }

        [BsonElement("code")]
        public string Code { get; set; } = string.Empty;

        [BsonElement("name")]
        public string Name { get; set; } = string.Empty;

        [BsonElement("description")]
        public string? Description { get; set; }

        [BsonElement("category")]
        public string Category { get; set; } = string.Empty;

        [BsonElement("quantity")]
        public int Quantity { get; set; }

        [BsonElement("added_by")]
        public string AddedBy { get; set; } = string.Empty; // <-- this is required

        [BsonElement("imagePath")]
        public string? ImagePath { get; set; }
    }
}
