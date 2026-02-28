using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;

namespace TastyTrails.Models;

public class MongoRestaurant
{
    [BsonId]
    [BsonRepresentation(BsonType.String)]
    public Guid Id { get; set; }

    [BsonElement("name")]
    public string Name { get; set; } = "";

    [BsonElement("coords")]
    public GeoPoint Coordinates { get; set; } = new();

    [BsonElement("cuisine")]
    public string? Cuisine { get; set; }

    [BsonElement("description")]
    public string? Description { get; set; }

    [BsonElement("images")]
    public List<string> Images { get; set; } = new();

    [BsonElement("averageRating")]
    public double AverageRating { get; set; } = 0;

    [BsonElement("totalReviews")]
    public int TotalReviews { get; set; } = 0;

    [BsonElement("lastViewedAt")]
    public DateTime? LastViewedAt { get; set; }

    [BsonElement("trendingScore")]
    public double TrendingScore { get; set; } = 0;

    [BsonElement("sourceId")]
    public string? SourceId { get; set; }
}