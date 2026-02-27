using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;

namespace TastyTrails.Models;

public class MongoUser
{
    [BsonId]
    [BsonRepresentation(BsonType.ObjectId)]
    public string Id { get; set; } = null!;

    [BsonElement("name")]
    public string Name { get; set; } = null!;

    [BsonElement("username")]
    public string Username { get; set; } = null!;

    [BsonElement("email")]
    public string Email { get; set; } = null!;

    [BsonElement("passwordHash")]
    public string PasswordHash { get; set; } = null!;

    [BsonElement("profileImage")]
    public string? ProfileImage { get; set; }

    [BsonElement("savedRestaurants")]
    [BsonRepresentation(BsonType.ObjectId)]
    public List<string> SavedRestaurants { get; set; } = new();

    [BsonElement("visitedRestaurants")]
    [BsonRepresentation(BsonType.ObjectId)]
    public List<string> VisitedRestaurants { get; set; } = new();

    [BsonElement("reviewedRestaurants")]
    [BsonRepresentation(BsonType.ObjectId)]
    public List<string> ReviewedRestaurants { get; set; } = new();

    [BsonElement("createdAt")]
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    [BsonElement("updatedAt")]
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
}