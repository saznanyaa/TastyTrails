using MongoDB.Driver;
using Microsoft.Extensions.Options;
using TastyTrails.Configurations;
using TastyTrails.Models;
using MongoDB.Driver.GeoJsonObjectModel;
using System.Security.Cryptography;
using Microsoft.AspNetCore.Http.HttpResults;
using TastyTrails.Models.DTOs;
using Cassandra;

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

            var users = _database.GetCollection<MongoUser>("users");
            var euIndex = Builders<MongoUser>.IndexKeys.Ascending(u => u.Email)
                                            .Ascending(u => u.Username);
            var iOptions = new CreateIndexOptions { Unique = true };
            users.Indexes.CreateOne(new CreateIndexModel<MongoUser>(euIndex, iOptions));
        }

        public IMongoCollection<T> GetCollection<T>(string collectionName)
        {
            return _database.GetCollection<T>(collectionName);
        }

        private IMongoCollection<MongoRestaurant> Restaurants => _database.GetCollection<MongoRestaurant>("restaurants");
        private IMongoCollection<MongoReview> Reviews => _database.GetCollection<MongoReview>("reviews");
        private IMongoCollection<MongoUser> Users => _database.GetCollection<MongoUser>("users");
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

        public async Task<MongoReview> GetReviewByRestAndUser(Guid restId, Guid userId)
        {
            var review = await Reviews.Find(r => r.RestaurantId == restId && r.UserId == userId).FirstOrDefaultAsync();
            return review;
        }

        public async Task<MongoReview> PostReview(Guid restaurantId, Guid userId, int rating, string comment)
        {
            var filter = Builders<MongoReview>.Filter.And(
                Builders<MongoReview>.Filter.Eq(r => r.RestaurantId, restaurantId),
                Builders<MongoReview>.Filter.Eq(r => r.UserId, userId)
            );

            var exists = await Reviews.Find(filter).FirstOrDefaultAsync();

            if (exists != null)
            {
                var update = Builders<MongoReview>.Update.Set(r => r.Rating, rating)
                                                        .Set(r => r.Comment, comment)
                                                        .Set(r => r.UpdatedAt, DateTime.UtcNow);
                await Reviews.UpdateOneAsync(filter, update);
                exists.Rating = rating;
                exists.Comment = comment;
                exists.UpdatedAt = DateTime.UtcNow;
                return exists;
            }
            else
            {
                var review = new MongoReview
                {
                    Id = Guid.NewGuid(),
                    RestaurantId = restaurantId,
                    UserId = userId,
                    Rating = rating,
                    Comment = comment,
                    CreatedAt = DateTime.UtcNow,
                    UpdatedAt = DateTime.UtcNow
                };

                await Reviews.InsertOneAsync(review);
                return review;
            }
        }

        public async Task<bool> DeleteReview(Guid reviewId, Guid userId)
        {
            var result = await Reviews.DeleteOneAsync(r => r.Id == reviewId && r.UserId == userId);
            return result.DeletedCount > 0;
        }

        //---users---------------------------------------------------------------------------
        public async Task<MongoUser> GetUserById(Guid id)
        {
            var user = await Users.Find(u => u.Id == id).FirstOrDefaultAsync();
            if(user == null) throw new Exception("User not found");
            return user;
        }

        public async Task<bool> UpdateUsernameOrEmail(Guid id, UpdateDTO dto)
        {
            var update = Builders<MongoUser>.Update.Set(u => u.Email, dto.Email)
                                                    .Set(u => u.Username, dto.Username);

            var res = await Users.UpdateOneAsync(u => u.Id == id, update);
            return res.ModifiedCount>0;
        }

        public async Task<bool> DeleteUser(Guid id)
        {
            var res = await Users.DeleteOneAsync(u => u.Id == id);
            return res.DeletedCount>0;
        }

        public async Task<Guid> PostUserSavedRestaurnts(Guid userId, Guid restaurantId)
        {
            var filter = Builders<MongoUser>.Filter.Eq(u => u.Id, userId);
            var update = Builders<MongoUser>.Update.AddToSet(u => u.SavedRestaurants, restaurantId);

            var result = await Users.UpdateOneAsync(filter, update);
            if (result.MatchedCount == 0) throw new Exception("User not found");

            return restaurantId;
        }

        public async Task<Guid> DeleteUserSavedRestaurant(Guid userId, Guid restaurantId)
        {
            var filter = Builders<MongoUser>.Filter.Eq(u => u.Id, userId);
            var update = Builders<MongoUser>.Update.Pull(u => u.SavedRestaurants, restaurantId);

            var result = await Users.UpdateOneAsync(filter, update);
            if (result.MatchedCount == 0) throw new Exception("User not found");

            return restaurantId;
        }

        public async Task<List<MongoRestaurant>> GetUserSavedRestaurants(Guid userId)
        {
            var user = await GetUserById(userId);
            if(user == null) throw new Exception("User not found");

            var filter = Builders<MongoRestaurant>.Filter.In(r => r.Id, user.SavedRestaurants);
            var restaurants = await Restaurants.Find(filter).ToListAsync();

            return restaurants;
        }

        public async Task<Guid> PostUserVisitedRestaurant(Guid userId, Guid restaurantId)
        {
            var filter = Builders<MongoUser>.Filter.Eq(u => u.Id, userId);
            var update = Builders<MongoUser>.Update.AddToSet(u => u.VisitedRestaurants, restaurantId);

            var result = await Users.UpdateOneAsync(filter, update);
            if (result.MatchedCount == 0) throw new Exception("User not found");

            return restaurantId;
        }

        public async Task<List<MongoRestaurant>> GetUserVisitedRestaurants(Guid userId)
        {
            var user = await GetUserById(userId);
            if(user == null) throw new Exception("User not found");

            var filter = Builders<MongoRestaurant>.Filter.In(r => r.Id, user.VisitedRestaurants);
            var restaurants = await Restaurants.Find(filter).ToListAsync();

            return restaurants;
        }

        public async Task Follow(Guid currentId, Guid targetId)
{
        if (currentId == targetId) throw new Exception("You cannot follow yourself!");

        var current = await GetUserById(currentId);
        var target = await GetUserById(targetId);

        if (current == null || target == null) throw new Exception("User not found!");

        if (current.Following.Contains(targetId)) return;

        var updateCurrent = Builders<MongoUser>.Update.AddToSet(u => u.Following, targetId);

        var updateTarget = Builders<MongoUser>.Update.AddToSet(u => u.Followers, currentId);

        await Users.UpdateOneAsync(u => u.Id == currentId, updateCurrent);
        await Users.UpdateOneAsync(u => u.Id == targetId, updateTarget);
}

        public async Task Unfollow(Guid currentId, Guid targetId)
        {
            if (currentId == targetId) throw new Exception("You cannot unfollow yourself!");

            var current = await GetUserById(currentId);
            var target = await GetUserById(targetId);

            if(current == null || target == null) throw new Exception("User not found!");

            if(!current.Following.Contains(targetId)) return;

            var updateCurrent = Builders<MongoUser>.Update.Pull(u => u.Following, targetId);
            var updateTarget = Builders<MongoUser>.Update.Pull(u => u.Followers, currentId);

            await Users.UpdateOneAsync(u => u.Id == currentId, updateCurrent);
            await Users.UpdateOneAsync(u => u.Id == targetId, updateTarget);
        }

        
    }
}