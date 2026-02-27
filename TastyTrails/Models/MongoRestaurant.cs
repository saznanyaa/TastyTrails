using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;

namespace TastyTrails.Models;

public class MongoRestaurant
{
    [BsonId]
    [BsonRepresentation(BsonType.ObjectId)]
    public string Id { get; set; } = null!;

    [BsonElement("name")]
    public string Name { get; set; } = null!;

    [BsonElement("description")]
    public string? Description { get; set; }

    [BsonElement("cuisine")]
    public string Cuisine { get; set; } = null!;

    [BsonElement("city")]
    public string City { get; set; } = null!;

    [BsonElement("address")]
    public string? Address { get; set; }

    [BsonElement("location")]
    public GeoLocation? Location { get; set; }

    [BsonElement("images")]
    public List<string> Images { get; set; } = new();

    [BsonElement("averageRating")]
    public double AverageRating { get; set; } = 0;

    [BsonElement("totalReviews")]
    public int TotalReviews { get; set; } = 0;

    [BsonElement("createdAt")]
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    [BsonElement("updatedAt")]
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
}

public class GeoLocation
{
    [BsonElement("type")]
    public string Type { get; set; } = "Point";

    [BsonElement("coordinates")]
    public double[] Coordinates { get; set; } = null!;
}