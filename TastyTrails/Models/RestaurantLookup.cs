using Cassandra.Mapping.Attributes;
namespace TastyTrails.Models
{
    public class RestaurantLookup
{
    public Guid Id { get; set; }

    public string? City { get; set; }

    public string? Cuisine { get; set; }
}
}