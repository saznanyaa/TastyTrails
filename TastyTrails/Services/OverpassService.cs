using System.Net.Http;
using Newtonsoft.Json.Linq;
using TastyTrails.Models;
using System.Collections.Generic;
using System.Threading.Tasks;
using System;
using System.Linq;

namespace TastyTrails.Services
{
    public class OverpassService
    {
        private readonly HttpClient _httpClient;

        public OverpassService()
        {
            _httpClient = new HttpClient();
            _httpClient.DefaultRequestHeaders.Add("User-Agent", "TastyTrails/1.0");
        }

        public async Task<List<SeedRestaurant>> GetRestaurantsAsync(string city)
        {
            string bbox = city switch
            {
                "Beograd" => "(44.7866,20.4489,44.8166,20.4789)",
                "Novi Sad" => "(45.20,19.78,45.30,19.90)",
                "Nis" => "(43.28,21.85,43.35,21.95)",
                _ => throw new ArgumentException("Unsupported city!")
            };

            var query = $@"
                [out:json][timeout:25];
                (
                node[""amenity""=""restaurant""]{bbox};
                way[""amenity""=""restaurant""]{bbox};
                );
                out 25;
                ";

            HttpResponseMessage response = null;

            try
            {
                for (int i = 0; i < 3; i++)
                {
                    var content = new FormUrlEncodedContent(new[]
                    {
                        new KeyValuePair<string, string>("data", query)
                    });

                    response = await _httpClient.PostAsync("https://overpass-api.de/api/interpreter", content);

                    if (response.IsSuccessStatusCode)
                        break;

                    await Task.Delay(1000);
                }
                //var response = await _httpClient.PostAsync("https://overpass-api.de/api/interpreter", content);

                if (!response.IsSuccessStatusCode)
                {
                    var errorBody = await response.Content.ReadAsStringAsync();
                    throw new Exception($"Overpass API greška: {response.StatusCode}. Detalji: {errorBody}");
                }

                var json = await response.Content.ReadAsStringAsync();

                if (!json.TrimStart().StartsWith("{"))
                {
                    Console.WriteLine("Overpass returned non-JSON response:");
                    Console.WriteLine(json);

                    return new List<SeedRestaurant>();
                }

                var data = JObject.Parse(json);

                var restaurants = new List<SeedRestaurant>();

                if (data["elements"] == null) return restaurants;

                foreach (var element in data["elements"])
                {
                    if (element["type"]?.ToString() != "node")
                        continue;
                    
                    var tags = element["tags"];
                    if (tags == null || tags["name"] == null) continue;

                    var latToken = element["lat"];
                    var lonToken = element["lon"];
                    if (latToken == null || lonToken == null) continue;

                    var cuisines = tags["cuisine"]?.ToString().Split(';') ?? new string[] { "unknown" };
                    var sourceId = element["id"]?.ToString();
                    var restaurantId = Guid.NewGuid();

                    foreach (var cuisine in cuisines)
                    {
                        restaurants.Add(new SeedRestaurant
                        {
                            Id = restaurantId,
                            City = city,
                            Name = tags["name"].ToString(),
                            Latitude = latToken.ToObject<double>(),
                            Longitude = lonToken.ToObject<double>(),
                            Cuisine = cuisine.Trim(),
                            SourceId = sourceId
                        });
                    }
                }

                return restaurants.Take(25).ToList();
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Greška u OverpassService: {ex.Message}");
                return new List<SeedRestaurant>();
            }
        }
    }
}