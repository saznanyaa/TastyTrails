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
        public async Task<List<Restaurant>> GetRestaurantsAsync(string city)
        {
            string bbox = city switch
            {
                "Beograd" => "(44.7866,20.4489,44.8166,20.4789)",
                "Novi Sad" => "(45.20,19.78,45.30,19.90)",
                _ => throw new ArgumentException("Unsupported city!")
            };
            var query = $@"
            [out:json];
            node[""amenity""=""restaurant""]{bbox};
            out;
            ";

            var content = new StringContent(query);

            var response = await _httpClient.PostAsync("https://overpass-api.de/api/interpreter", content);

            var json = await response.Content.ReadAsStringAsync();
            var data = JObject.Parse(json);

            var restaurants = new List<Restaurant>();

            foreach (var element in data["elements"])
            {
                var tags = element["tags"];
                if (tags == null)
                    continue;

                var nameToken = tags["name"];
                if (nameToken == null)
                    continue;

                var latToken = element["lat"];
                var lonToken = element["lon"];
                if (latToken == null || lonToken == null)
                    continue;
                
                var cuisines = tags["cuisine"]?.ToString().Split(';') ?? new string[] { "unknown" };

                var restaurantId = Guid.NewGuid();

                foreach (var cuisine in cuisines)
                {
                    restaurants.Add(new Restaurant
                    {
                        Id = restaurantId,
                        City = city,
                        Name = nameToken.ToString(),
                        Latitude = latToken.ToObject<double>(),
                        Longitude = lonToken.ToObject<double>(),
                        Cuisine = cuisine.Trim(),
                        PopularityScore = 0
                    });
                }
            }

            return restaurants; 
        }
    }
}