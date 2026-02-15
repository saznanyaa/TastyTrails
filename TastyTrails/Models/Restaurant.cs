using System;

namespace TastyTrails.Models
{
    public class Restaurant
    {
        public Guid Id { get; set; }
        public string? Name { get; set; }
        public double Latitude { get; set; }
        public double Longitude { get; set; }
        public string? Cuisine { get; set; }
    }
}