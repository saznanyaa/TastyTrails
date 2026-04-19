using Microsoft.AspNetCore.Mvc.ViewEngines;
using Microsoft.Extensions.Options;
using MongoDB.Bson;
using MongoDB.Driver;
using MongoDB.Driver.GeoJsonObjectModel;
using TastyTrails.Configurations;
using TastyTrails.Models;
using TastyTrails.Models.DTOs;

namespace TastyTrails.Services
{
    public class MongoService
    {
        private readonly IMongoDatabase _database;

        public IMongoCollection<MongoUser> Users { get; private set; }

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

            Users = _database.GetCollection<MongoUser>("users");
            var euIndex = Builders<MongoUser>.IndexKeys.Ascending(u => u.Email)
                                            .Ascending(u => u.Username);
            var iOptions = new CreateIndexOptions { Unique = true };
            Users.Indexes.CreateOne(new CreateIndexModel<MongoUser>(euIndex, iOptions));
        }

    
        private IMongoCollection<MongoRestaurant> Restaurants => _database.GetCollection<MongoRestaurant>("restaurants");
        private IMongoCollection<MongoReview> Reviews => _database.GetCollection<MongoReview>("reviews");


        public async Task InsertRestaurants(List<SeedRestaurant> seeds)
        {
            if (seeds == null || !seeds.Any()) return;
            var mongoRestaurants = seeds.Select(seed => new MongoRestaurant
            {
                Id = seed.Id,
                Name = seed.Name,
                Cuisine = seed.Cuisine,
                SourceId = seed.SourceId,
                Coordinates = new GeoPoint { Lat = seed.Latitude, Lng = seed.Longitude },
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
            return await Restaurants.Find(_ => true).ToListAsync();
        }

        public async Task<MongoRestaurant> GetRestaurantById(Guid id)
        {
            return await Restaurants.Find(r => r.Id == id).FirstOrDefaultAsync();
        }

        public async Task<List<MongoRestaurant>> GetRestaurantsNearMe(double lat, double lng, double radius)
        {
            var minLat = lat - radius;
            var maxLat = lat + radius;
            var minLng = lng - radius;
            var maxLng = lng + radius;
            return await Restaurants.Find(r => r.Coordinates.Lat >= minLat && r.Coordinates.Lat <= maxLat
            && r.Coordinates.Lng >= minLng && r.Coordinates.Lng <= maxLng).ToListAsync();
        }

        public async Task<List<MongoReview>> GetRestaurantReviews(Guid id)
        {
            return await Reviews.Find(r => r.RestaurantId == id).ToListAsync();
        }

        public async Task<List<MongoReview>> GetReviewsByUser(Guid id)
        {
            return await Reviews.Find(r => r.UserId == id).ToListAsync();
        }

        public async Task<MongoReview> GetReviewByRestAndUser(Guid restId, Guid userId)
        {
            return await Reviews.Find(r => r.RestaurantId == restId && r.UserId == userId).FirstOrDefaultAsync();
        }

        public async Task<MongoReview> PostReview(Guid restaurantId, Guid userId, int rating, string comment)
        {
            var filter = Builders<MongoReview>.Filter.And(
                Builders<MongoReview>.Filter.Eq(r => r.RestaurantId, restaurantId),
                Builders<MongoReview>.Filter.Eq(r => r.UserId, userId)
            );
            var exists = await Reviews.Find(filter).FirstOrDefaultAsync();
            
            var restaurant = await GetRestaurantById(restaurantId);
            if (restaurant == null) throw new Exception("Restaurant not found");

            if (exists != null)
            {
                int oldRating = exists.Rating;

                var update = Builders<MongoReview>.Update.Set(r => r.Rating, rating)
                                                         .Set(r => r.Comment, comment)
                                                         .Set(r => r.UpdatedAt, DateTime.UtcNow);
                await Reviews.UpdateOneAsync(filter, update);

                restaurant.AverageRating = ((restaurant.AverageRating * restaurant.TotalReviews) - oldRating + rating) / restaurant.TotalReviews;

                if(restaurant.AverageRating > 2.5) restaurant.TrendingScore += 10;
                else restaurant.TrendingScore -= 10;

                await UpdateRestaurant(restaurant);

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
                
                restaurant.TotalReviews += 1;
                restaurant.AverageRating = ((restaurant.AverageRating * (restaurant.TotalReviews - 1)) + rating) / restaurant.TotalReviews;

                if(restaurant.AverageRating > 2.5) restaurant.TrendingScore += 10;
                else restaurant.TrendingScore -= 10;

                await UpdateRestaurant(restaurant);

                return review;
            }
        }

        public async Task UpdateReview(Guid userId, Guid restaurantId, UpdateReviewDTO dto)
        {
            var filter = Builders<MongoReview>.Filter.And(
                Builders<MongoReview>.Filter.Eq(r => r.UserId, userId),
                Builders<MongoReview>.Filter.Eq(r => r.RestaurantId, restaurantId)
            );

            var existingReview = await Reviews.Find(filter).FirstOrDefaultAsync();
            if (existingReview == null) throw new Exception("Review not found");

            var updates = new List<UpdateDefinition<MongoReview>>();

            bool ratingChanged = dto.Rating.HasValue && dto.Rating.Value != existingReview.Rating;

            if(dto.Rating.HasValue)
            {
                updates.Add(Builders<MongoReview>.Update.Set(r => r.Rating, dto.Rating.Value));
            }

            if(dto.Comment != null)
            {
                updates.Add(Builders<MongoReview>.Update.Set(r => r.Comment, dto.Comment));
            }

            updates.Add(Builders<MongoReview>.Update.Set(r => r.UpdatedAt, DateTime.UtcNow));

            var updated = Builders<MongoReview>.Update.Combine(updates);

            if(ratingChanged)
            {
                var restaurant = await GetRestaurantById(restaurantId);
                if (restaurant == null) throw new Exception("Restaurant not found");

                restaurant.AverageRating = ((restaurant.AverageRating * restaurant.TotalReviews) - existingReview.Rating + dto.Rating.Value) / restaurant.TotalReviews;

                if(restaurant.AverageRating > 2.5 && existingReview.Rating <= 2.5) restaurant.TrendingScore += 10;
                else if(restaurant.AverageRating <= 2.5 && existingReview.Rating > 2.5) restaurant.TrendingScore -= 10;

                await UpdateRestaurant(restaurant);
            }

            var result = await Reviews.UpdateOneAsync(filter, updated);

            if(result.MatchedCount == 0) throw new Exception("Review not found");
        }

        public async Task UpdateRestaurant(MongoRestaurant restaurant)
        {
            var filter = Builders<MongoRestaurant>.Filter.Eq(r => r.Id, restaurant.Id);
            
            var update = Builders<MongoRestaurant>.Update.Set(r => r.AverageRating, restaurant.AverageRating)
                                                         .Set(r => r.TotalReviews, restaurant.TotalReviews)
                                                         .Set(r => r.TrendingScore, restaurant.TrendingScore);

            await Restaurants.UpdateOneAsync(filter, update);
        }

        public async Task UpdateTrendingScore(Guid restaurantId, double newScore)
        {
            var filter = Builders<MongoRestaurant>.Filter.Eq(r => r.Id, restaurantId);
            var update = Builders<MongoRestaurant>.Update.Set(r => r.TrendingScore, newScore);
            await Restaurants.UpdateOneAsync(filter, update);
        }

        public async Task<bool> DeleteReview(Guid reviewId, Guid userId)
        {
            var review = await Reviews.Find(r => r.Id == reviewId && r.UserId == userId)
                              .FirstOrDefaultAsync();

            if (review == null) return false;

            var rating = review.Rating;

            var restaurantId = review.RestaurantId;

            var result = await Reviews.DeleteOneAsync(r => r.Id == reviewId && r.UserId == userId);

            await DecreaseReviewsOnRestaurant(restaurantId, rating);

            return result.DeletedCount > 0;
        }

        public async Task DecreaseReviewsOnRestaurant(Guid restaurantId, int rating)
        {
            var restaurant = await Restaurants.Find(r => r.Id == restaurantId)
                                      .FirstOrDefaultAsync();

            if (restaurant == null) return;

            restaurant.TotalReviews -= 1;
            restaurant.TrendingScore -= rating;

            if (restaurant.TotalReviews <= 0)
            {
                restaurant.TotalReviews = 0;
                restaurant.TrendingScore = 0;
                restaurant.AverageRating = 0;
            }
            else
            {
                restaurant.AverageRating =
                    (double)restaurant.TrendingScore / restaurant.TotalReviews;
    }

    await Restaurants.ReplaceOneAsync(r => r.Id == restaurantId, restaurant);
        }

        public async Task<MongoRestaurant> GetRestaurantByReviewId(Guid reviewId)
        {
            var review = await Reviews.Find(r => r.Id == reviewId).FirstOrDefaultAsync();
            if (review == null) return null;
            return await GetRestaurantById(review.RestaurantId);
        }

        public async Task<MongoUser> GetUserById(Guid id)
        {
            var user = await Users.Find(u => u.Id == id).FirstOrDefaultAsync();

            return user;
        }

        public async Task<List<MongoUser>> SearchUsersAsync(string username)
        {
            // Koristimo Regex za delimično poklapanje, "i" opcija za case-insensitive
            var filter = Builders<MongoUser>.Filter.Regex(u => u.Username, new BsonRegularExpression(username, "i"));

            // Pretražujemo Users kolekciju koju si definisao gore u konstruktoru
            return await Users
                .Find(filter)
                .Project<MongoUser>(Builders<MongoUser>.Projection
                    .Include(u => u.Id)
                    .Include(u => u.Username)
                    .Include(u => u.Email)) // Možeš dodati polja koja su ti bitna za search
                .Limit(10)
                .ToListAsync();
        }
        public async Task<bool> UpdateUsernameOrEmail(Guid id, UpdateDTO dto)
        {
            var update = Builders<MongoUser>.Update.Set(u => u.Email, dto.Email)
                                                   .Set(u => u.Username, dto.Username);
            var res = await Users.UpdateOneAsync(u => u.Id == id, update);
            return res.ModifiedCount > 0;
        }

        public async Task<bool> DeleteUser(Guid id)
        {
            var res = await Users.DeleteOneAsync(u => u.Id == id);
            return res.DeletedCount > 0;
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
            if (user == null) throw new Exception("User not found");
            var filter = Builders<MongoRestaurant>.Filter.In(r => r.Id, user.SavedRestaurants);
            return await Restaurants.Find(filter).ToListAsync();
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
            if (user == null) throw new Exception("User not found");
            var filter = Builders<MongoRestaurant>.Filter.In(r => r.Id, user.VisitedRestaurants);
            return await Restaurants.Find(filter).ToListAsync();
        }

        public async Task Follow(Guid currentId, Guid targetId)
        {
            if (currentId == targetId) throw new Exception("You cannot follow yourself!");
            var updateCurrent = Builders<MongoUser>.Update.AddToSet(u => u.Following, targetId);
            var updateTarget = Builders<MongoUser>.Update.AddToSet(u => u.Followers, currentId);
            await Users.UpdateOneAsync(u => u.Id == currentId, updateCurrent);
            await Users.UpdateOneAsync(u => u.Id == targetId, updateTarget);
        }

        public async Task Unfollow(Guid currentId, Guid targetId)
        {
            if (currentId == targetId) throw new Exception("You cannot unfollow yourself!");
            var updateCurrent = Builders<MongoUser>.Update.Pull(u => u.Following, targetId);
            var updateTarget = Builders<MongoUser>.Update.Pull(u => u.Followers, currentId);
            await Users.UpdateOneAsync(u => u.Id == currentId, updateCurrent);
            await Users.UpdateOneAsync(u => u.Id == targetId, updateTarget);
        }

        public async Task<bool> UpdateReviewAsync(string reviewId, int newRating, string newComment)
        {
            // 1. Provera validnosti stringa
            if (string.IsNullOrEmpty(reviewId)) return false;

            // 2. Filter - pošto tvoj model kaže BsonType.String, 
            // MongoDB driver očekuje da porediš sa Guid objektom koji on interno mapira u string
            if (!Guid.TryParse(reviewId, out Guid guidId)) return false;

            var filter = Builders<MongoReview>.Filter.Eq(r => r.Id, guidId);

            // 3. Update operacija
            var update = Builders<MongoReview>.Update
                .Set(r => r.Rating, newRating)
                .Set(r => r.Comment, newComment)
                .Set(r => r.UpdatedAt, DateTime.UtcNow);

            var result = await Reviews.UpdateOneAsync(filter, update);

            return result.MatchedCount > 0; // Bolje MatchedCount jer ModifiedCount može biti 0 ako ništa nisi promenio
        }

        public async Task<bool> DeleteReviewAsync(string reviewId)
        {
            if (!Guid.TryParse(reviewId, out Guid guidId))
            {
                return false;
            }

            // Filter traži recenziju sa tim Guid-om
            var filter = Builders<MongoReview>.Filter.Eq(r => r.Id, guidId);

            // Brisanje iz kolekcije "reviews"
            var result = await Reviews.DeleteOneAsync(filter);

            // Vraća true ako je barem jedan dokument obrisan
            return result.DeletedCount > 0;
        }

        public async Task<List<UserPreviewModel>> GetFollowDetailsAsync(Guid userId, string type)
        {
            // 1. Dohvati korisnika iz kolekcije
            var user = await Users.Find(u => u.Id == userId).FirstOrDefaultAsync();

            if (user == null) return new List<UserPreviewModel>();

            // 2. Odaberi pravi niz (Following ili Followers)
            List<Guid> targetIds = type.ToLower() == "followers"
                ? user.Followers
                : user.Following;

            if (targetIds == null || targetIds.Count == 0)
                return new List<UserPreviewModel>();

            // 3. Pronađi sve te korisnike u bazi
            // Pošto su u bazi Guid-ovi (čak i ako su sačuvani kao stringovi, drajver će ih mapirati)
            var filter = Builders<MongoUser>.Filter.In(u => u.Id, targetIds);
            var usersFromDb = await Users.Find(filter).ToListAsync();

            // 4. Mapiranje na UserPreviewModel
            return usersFromDb.Select(u => new UserPreviewModel
            {
                Id = u.Id,
                Username = u.Username,
                ProfileImage = u.ProfileImage
            }).ToList();
        }

        public async Task UpdateUserProfileImage(Guid userId, string imageUrl)
        {
            var filter = Builders<MongoUser>.Filter.Eq(u => u.Id, userId);
            var update = Builders<MongoUser>.Update.Set(u => u.ProfileImage, imageUrl);
            await Users.UpdateOneAsync(filter, update);
        }
    }
}