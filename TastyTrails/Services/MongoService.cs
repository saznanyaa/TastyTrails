using MongoDB.Driver;
using Microsoft.Extensions.Options;
using TastyTrails.Configurations;
using TastyTrails.Models;
using MongoDB.Driver.GeoJsonObjectModel;
using System.Security.Cryptography;

namespace TastyTrails.Services
{
    public class MongoService
    {
        private readonly IMongoDatabase _database;

        public MongoService(IOptions<MongoSettings> settings)
        {
            var client = new MongoClient(settings.Value.ConnectionString);
            _database = client.GetDatabase(settings.Value.DatabaseName);
        }

        public IMongoCollection<T> GetCollection<T>(string collectionName)
        {
            return _database.GetCollection<T>(collectionName);
        }

        private IMongoCollection<MongoRestaurant> Restaurants => _database.GetCollection<MongoRestaurant>("restaurants");
        private IMongoCollection<MongoReview> Reviews => _database.GetCollection<MongoReview>("reviews");
        public async Task InsertRestaurants(List<SeedRestaurant> seeds)
        {
            if(seeds == null || !seeds.Any()) return;

            var mongoRestaurants = seeds.Select(seed => new MongoRestaurant
            {
                Id = seed.Id,
                Name = seed.Name,
                Cuisine = seed.Cuisine,
                SourceId = seed.SourceId,
                Coordinates = new GeoPoint
                {
                    Lat = seed.Latitude,
                    Lng = seed.Longitude
                },
                Description = null,
                Images = new List<string>(),
                AverageRating = 0,
                TotalReviews = 0,
                LastViewedAt = null,
                TrendingScore = 0
            }).ToList();
        
            foreach (var restaurant in mongoRestaurants)
            {
                var filter = Builders<MongoRestaurant>.Filter.Eq(r => r.Id, restaurant.Id);
                await Restaurants.ReplaceOneAsync(filter, restaurant, new ReplaceOptions { IsUpsert = true });
            }
        }

        public async Task<List<MongoRestaurant>> GetRestaurants()
        {
            var restaurants = await Restaurants.Find(_ => true).ToListAsync();
            return restaurants;
        }

        public async Task<MongoRestaurant> GetRestaurantById(Guid id)
        {
            var r = await Restaurants.Find(r => r.Id == id).FirstOrDefaultAsync();
            return r;
        }

        public async Task<List<MongoRestaurant>> GetRestaurantsNearMe(double lat, double lng, double radius)
        {
            var minLat = lat - radius;
            var maxLat = lat + radius;
            var minLng = lng - radius;
            var maxLng = lng + radius;
            
            var rs = await Restaurants.Find(r => r.Coordinates.Lat >= minLat && r.Coordinates.Lat <= maxLat
            && r.Coordinates.Lng >= minLng && r.Coordinates.Lng <= maxLng).ToListAsync();

            return rs;

        }

        public async Task<List<MongoReview>> GetRestaurantReviews(Guid id)
        {
            var reviews = await Reviews.Find(r => r.RestaurantId == id).ToListAsync();
            return reviews;
        }
    }
}