using MongoDB.Driver;
using Microsoft.Extensions.Options;
using TastyTrails.Configurations;
using TastyTrails.Models;
using MongoDB.Driver.GeoJsonObjectModel;
using System.Security.Cryptography;
using Microsoft.AspNetCore.Http.HttpResults;

namespace TastyTrails.Services
{
    public class MongoService
    {
        private readonly IMongoDatabase _database;

        public MongoService(IOptions<MongoSettings> settings)
        {
            var client = new MongoClient(settings.Value.ConnectionString);
            _database = client.GetDatabase(settings.Value.DatabaseName);

            var collection = _database.GetCollection<MongoReview>("reviews");
            var indexKeys = Builders<MongoReview>.IndexKeys
                                            .Ascending(r => r.RestaurantId)
                                            .Ascending(r => r.UserId);
            var indexOptions = new CreateIndexOptions { Unique = true };
            collection.Indexes.CreateOne(new CreateIndexModel<MongoReview>(indexKeys, indexOptions));
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

        //---reviews----------------------------------------------------------------------------------

        public async Task<List<MongoReview>> GetRestaurantReviews(Guid id)
        {
            var reviews = await Reviews.Find(r => r.RestaurantId == id).ToListAsync();
            return reviews;
        }

        public async Task<List<MongoReview>> GetReviewsByUser(Guid id)
        {
            var reviews = await Reviews.Find(r => r.UserId == id).ToListAsync();
            return reviews;
        }

        public async Task<MongoReview> PostReview(MongoReview review)
        {
            var filter = Builders<MongoReview>.Filter.And(
                Builders<MongoReview>.Filter.Eq(r => r.RestaurantId, review.RestaurantId),
                Builders<MongoReview>.Filter.Eq(r => r.UserId, review.UserId)
            );

            var exists = await Reviews.Find(filter).FirstOrDefaultAsync();

            if (exists != null)
            {
                var update = Builders<MongoReview>.Update.Set(r => r.Rating, review.Rating)
                                                        .Set(r => r.Comment, review.Comment)
                                                        .Set(r => r.UpdatedAt, DateTime.UtcNow);
                await Reviews.UpdateOneAsync(filter, update);
                exists.Rating = review.Rating;
                exists.Comment = review.Comment;
                exists.UpdatedAt = review.UpdatedAt;
                return exists;
            }
            else
            {
                review.Id = Guid.NewGuid();
                review.CreatedAt = DateTime.UtcNow;
                review.UpdatedAt = DateTime.UtcNow;

                await Reviews.InsertOneAsync(review);
                return review;
            }
        }
    }
}