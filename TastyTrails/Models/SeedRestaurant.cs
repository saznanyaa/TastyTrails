namespace TastyTrails.Models
{
    public class SeedRestaurant
    {
        public Guid Id { get; set; }

        public string? City { get; set; }

        public string? Name { get; set; }

        public double Latitude { get; set; }

        public double Longitude { get; set; }

        public string? Cuisine { get; set; }

        public string? SourceId { get; set; }
    }
}