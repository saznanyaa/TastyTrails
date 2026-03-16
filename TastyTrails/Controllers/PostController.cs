using Microsoft.AspNetCore.Mvc;
using TastyTrails.Models;
using TastyTrails.Models.DTOs;
using TastyTrails.Services;
using MongoDB.Driver;
using Microsoft.AspNetCore.Identity;

namespace TastyTrails.Controllers
{
    [ApiController]
    [Route("api/post")]
    public class PostController : ControllerBase
    {
        private readonly OverpassService _overpass;
        private readonly CassandraService _cassandra;
        private readonly MongoService _mongo;
        private readonly IConfiguration _config;
        private readonly IMongoCollection<MongoUser> _users;
        private readonly PasswordHasher<MongoUser> _passwordHasher;
        private readonly AuthService _auth;
        private readonly INeo4jService _neo4jService;

        public PostController(MongoService mg , IConfiguration config, AuthService auth, INeo4jService neo4j)
        {
            _overpass = new OverpassService();
            _cassandra = new CassandraService();
            _mongo = mg;
            _config = config;
            _auth = auth;
            _passwordHasher = new PasswordHasher<MongoUser>();
            _neo4jService = neo4j;
            
            var mongoSettings = _config.GetSection("MongoSettings");
            var connectionString = mongoSettings["ConnectionString"]!;
            var databaseName = mongoSettings["DatabaseName"]!;

            var client = new MongoClient(connectionString);
            var database = client.GetDatabase(databaseName);
            _users = database.GetCollection<MongoUser>("users");
        }

        //it's a get, but it posts to cassandra so that's why it's here
        [HttpGet("GetRestaurantsFromOverpass")]
        public async Task<IActionResult> ImportRestaurants(string city)
        {
            var restaurants = await _overpass.GetRestaurantsAsync(city);

            foreach (var restaurant in restaurants)
            {
                await _cassandra.InsertRestaurantAsync(restaurant);
                await _cassandra.InsertRestaurantCuisineAsync(restaurant);

                var lookup = new RestaurantLookup
                {
                    Id = restaurant.Id,
                    City = restaurant.City,
                    Cuisine = restaurant.Cuisine,
                    Name = restaurant.Name,
                    Latitude = restaurant.Latitude,
                    Longitude = restaurant.Longitude
                };
                await _cassandra.InsertRestaurantLookup(lookup);
            }

            await _mongo.InsertRestaurants(restaurants);

            return Ok($"{restaurants.Count} restaurants inserted for {city}.");
        }

        [HttpPost("PostUser")]
        public async Task<IActionResult> PostUser(CassandraUser u)
        {
            u.Id = Guid.NewGuid();
            await _cassandra.InsertUserAsync(u);

            return Ok($"User {u.Username} inserted with {u.Id} id.");
        }

        [HttpPost("Register")]
        public async Task<IActionResult> Register([FromBody]RegisterDto registerDto)
        {
            var userId = Guid.NewGuid();

            var mongoUser = new MongoUser
            {
                Id = userId,
                Name = registerDto.Name,
                Username = registerDto.Username,
                Email = registerDto.Email,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            };
            
            var cassandraUser = new CassandraUser
            {
                Id = userId,
                Username = registerDto.Username,
                Email = registerDto.Email,
                Role = "user"
            };

            mongoUser.PasswordHash = _passwordHasher.HashPassword(mongoUser, registerDto.Password);
            await _users.InsertOneAsync(mongoUser);

            await _cassandra.InsertUserAsync(cassandraUser);

            var token = _auth.GenerateTokenForUser(mongoUser);
            return Ok(token);
            
        }

        [HttpPost("{id}/view")]
        public async Task<IActionResult> PostRestaurantView(Guid id, [FromBody]Guid userId)
        {
            var view = new CassandraRestaurantView
            {
                RestaurantId = id,
                UserId = userId,
                ViewedAt = DateTime.Now.ToUniversalTime()
            };

            await _cassandra.InsertRestaurantView(view);

            return Ok();
        }
        
        //-----------------------------------------------------
        [HttpPost("users/{userId}/saved/{restaurantId}")]
        public async Task<IActionResult> PostUserSavedrestaurant(Guid userId, Guid restaurantId)
        {
            var saved = new CassandraSavedRestaurants
            {
                UserId = userId,
                RestaurantId = restaurantId,
                SavedAt = DateTime.Now.ToUniversalTime()
            };
            await _cassandra.InsertUserSavedRestaurants(saved);
            return Ok(saved);
        }

        //----------------------------------------------------------------------------
        [HttpPost("restaurants/{id}/rating")]
        public async Task<IActionResult> PostRestaurantRating(Guid id, [FromQuery]Guid userId, [FromQuery]int value)
        {
            if(value < 1 || value > 5)
                return BadRequest("Incorrect rating value!");
            await _cassandra.PostRestaurantRating(id, userId, value);
            return Ok(new {RestaurantId = id, UserId = userId, RatingValue = value});
        }

        //----------------------------------------------------------------------------
        [HttpPost("restaurants/{id}/review")]
        public async Task<IActionResult> PostRestaurantReview(Guid id, [FromQuery]Guid userId, [FromBody]MongoReview mongoReview)
        {
            var review = new CassandraRestaurantReview
            {
                RestaurantId = id,
                UserId = userId,
                ReviewedAt = DateTime.Now.ToUniversalTime()
            };
            await _cassandra.PostRestaurantReview(review);

            var mReview = await _mongo.PostReview(id, userId, mongoReview.Rating, mongoReview.Comment);
            return Ok($"Cassandra review: {review}, and Mongo review: {mReview}");
        }

        //---restaurant_checkins-----------------------------------------------------
        [HttpPost("restaurants/{id}/chekin")]
        public async Task<IActionResult> PostRestaurantCheckin(Guid id, [FromQuery]Guid userId)
        {
            var checkin = new CassandraRestaurantCheckins
            {
                RestaurantId = id,
                UserId = userId,
                CheckedInAt = DateTime.Now.ToUniversalTime()
            };
            await _cassandra.PostRestaurantCheckin(checkin);
            return Ok(checkin);
        }

        //-------------------------------------------------------------------------------
        [HttpPost("user")]
        public async Task<IActionResult> CreateUser([FromBody] NeoUserNode user)
        {
            await _neo4jService.CreateUserNodeAsync(user);
            return Ok($"Korisnik {user.Username} uspešno kreiran/ažuriran.");
        }

        //--------------------------------------------------------------------------------
        [HttpPost("restaurant")]
        public async Task<IActionResult> CreateRestaurant([FromBody] NeoRestaurantNode restaurant)
        {
            await _neo4jService.CreateRestaurantNodeAsync(restaurant);
            return Ok($"Restoran {restaurant.Name} uspešno kreiran/ažuriran.");
        }

        //--------------------------------------------------------------------------------
        [HttpPost("connect")]
        public async Task<IActionResult> Connect(string userId, string restaurantId, string type = "LIKE")
        {
            await _neo4jService.ConnectUserToRestaurantAsync(userId, restaurantId, type);
            return Ok($"Veza [{type}] uspostavljena između korisnika {userId} i restorana {restaurantId}.");
        }

        //-------------------------------------------------------------------------------
        //[HttpPost("cuisine")]
        // public async Task<IActionResult> CreateCuisine([FromBody] CuisineNode cuisine)
        // {
        //     await _neo4jService.CreateCuisineNodeAsync(cuisine);
        //     return Ok($"Kuhinja {cuisine.Name} je kreirana.");
        // }

        //-------------------------------------------------------------------------------
        //[HttpPost("restaurant/serve-cuisine")]
        // public async Task<IActionResult> ServeCuisine(string restaurantId, string cuisineId)
        // {
        //     await _neo4jService.ConnectRestaurantToCuisineAsync(restaurantId, cuisineId);
        //     return Ok("Restoran je uspešno povezan sa tipom kuhinje.");
        // }

        //--------------------------------------------------------------------------------
        //[HttpPost("link-external-review")]
    //     public async Task<IActionResult> LinkExternalReview(
    // [FromQuery] string userId,
    // [FromQuery] string restaurantId,
    // [FromBody] ReviewRelationNode externalData)
    //     {
    //         try
    //         {
    //             await _neo4jService.LinkExternalReviewAsync(userId, restaurantId, externalData);
    //             return Ok(new
    //             {
    //                 Message = $"Uspešno povezana recenzija za korisnika {userId} i restoran {restaurantId}.",
    //                 MongoId = externalData.MongoReviewId,
    //                 CassandraId = externalData.CassandraRatingId
    //             });
    //         }
    //         catch (Exception ex)
    //         {
    //             return BadRequest($"Greška prilikom povezivanja: {ex.Message}");
    //         }
    //     }

        //----------------------------------------------------------------------------------------
        [HttpPost("user/follow/{followerId}/{followedId}")]
        public async Task<IActionResult> FollowUser(string followerId, string followedId)
        {
            await _neo4jService.FollowUserAsync(followerId, followedId);
            return Ok(new { Message = "Veza FOLLOWS uspešno kreirana." });
        }

        //---------------------------------------------------------------------------------------
        [HttpPost("user/like-cuisine/{userId}/{cuisineId}")]
        public async Task<IActionResult> LikeCuisine(string userId, string cuisineId)
        {
            await _neo4jService.UserLikesCuisineAsync(userId, cuisineId);
            return Ok(new { Message = "Veza LIKES_CUISINE uspešno kreirana." });
        }

    }
}