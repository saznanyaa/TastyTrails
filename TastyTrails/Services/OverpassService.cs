using System.Net.Http;
using Cassandra.DataStax.Graph;
using Newtonsoft.Json.Linq;
using TastyTrails.Models;

namespace TastyTrails.Services
{
    public class OverpassService
    {
        private readonly HttpClient _httpClient;

        public OverpassService()
        {
            _httpClient = new HttpClient();
        }

        //(44.7866,20.4489,44.8166,20.4789)BG
        //(45.20,19.78,45.30,19.90)NS
        public async Task<List<Restaurant>> GetRestaurantsAsync()
        {
            var query = @"
            [out:json];
            node[""amenity""=""restaurant""](45.20,19.78,45.30,19.90);
            out;
            ";

            var content = new StringContent(query);

            var response = await _httpClient.PostAsync("https://overpass-api.de/api/interpreter", content);

            var json = await response.Content.ReadAsStringAsync();
            var data = JObject.Parse(json);

            var restaurants = new List<Restaurant>();

            foreach (var element in data["elements"])
            {
                // Skip if tags are missing
                var tags = element["tags"];
                if (tags == null)
                    continue;

                // Skip if name is missing
                var nameToken = tags["name"];
                if (nameToken == null)
                    continue;

                // Skip if lat/lon missing
                var latToken = element["lat"];
                var lonToken = element["lon"];
                if (latToken == null || lonToken == null)
                    continue;

                // Handle multiple cuisines
                var cuisines = tags["cuisine"]?.ToString().Split(';') ?? new string[] { "unknown" };

                foreach (var cuisine in cuisines)
                {
                    restaurants.Add(new Restaurant
                    {
                        Id = Guid.NewGuid(),
                        Name = nameToken.ToString(),
                        Latitude = latToken.ToObject<double>(),
                        Longitude = lonToken.ToObject<double>(),
                        Cuisine = cuisine.Trim()
                    });
                }
            }

            return restaurants; 
        }
    }
}