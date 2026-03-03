using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;

namespace TastyTrails.Models;

public class MongoUser
{
    [BsonId]
    [BsonRepresentation(BsonType.String)]
    public Guid Id { get; set; }

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
    [BsonRepresentation(BsonType.String)]
    public List<Guid> SavedRestaurants { get; set; } = new();

    [BsonElement("visitedRestaurants")]
    [BsonRepresentation(BsonType.String)]
    public List<Guid> VisitedRestaurants { get; set; } = new();

    [BsonElement("isActive")]
    public bool IsActive { get; set; } = true;

    [BsonElement("createdAt")]
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    [BsonElement("updatedAt")]
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
}