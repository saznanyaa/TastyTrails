using Microsoft.AspNetCore.Mvc;
using TastyTrails.Models;
using TastyTrails.Models.DTOs;
using TastyTrails.Services;
using MongoDB.Driver;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Authorization;

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

        //it's a get, but it posts to databases so that's why it's here
        [HttpGet("GetRestaurantsFromOverpass")]
        public async Task<IActionResult> ImportRestaurants(string city)
        {
            var restaurants = await _overpass.GetRestaurantsAsync(city);

            foreach (var restaurant in restaurants)
            {
                await _cassandra.InsertRestaurantAsync(restaurant);
                await _cassandra.InsertRestaurantCuisineAsync(restaurant);

                await _neo4jService.CreateRestaurantNodeAsync(new NeoRestaurantNode
                {
                    Id = restaurant.Id.ToString(),
                    Name = restaurant.Name,
                    Location = restaurant.City,
                    Cuisine = restaurant.Cuisine
                });

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

        //posts users to all 3 databases
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

            string struserId = userId.ToString();

            var neo4jUser = new NeoUserNode
            {
                Id = struserId,
                Username = registerDto.Username
            };

            mongoUser.PasswordHash = _passwordHasher.HashPassword(mongoUser, registerDto.Password);
            await _users.InsertOneAsync(mongoUser);

            await _cassandra.InsertUserAsync(cassandraUser);

            await _neo4jService.CreateUserNodeAsync(neo4jUser);

            var token = _auth.GenerateTokenForUser(mongoUser);
            return Ok(new { message = "Registration successful", token = token});
            
        }

//---cassandra_analytics-----------------------------------------------------------------------------
        [HttpPost("{id}/view")]
        public async Task<IActionResult> PostRestaurantView(Guid id)
        {
            var view = new CassandraRestaurantView
            {
                RestaurantId = id,
                ViewedAt = DateTime.Now.ToUniversalTime()
            };

            await _cassandra.InsertRestaurantView(view);

            var restaurant = await _mongo.GetRestaurantById(id);

            await _mongo.UpdateTrendingScore(id, restaurant.TrendingScore + 2);

            return Ok();
        }
//--------------------------------------------------------------------------------------------

        [Authorize]
        [HttpPost("user/follow")]
        public async Task<IActionResult> FollowUser([FromQuery] string targetId)
        {
            // 1. Izvlačenje tvog ID-a iz tokena
            var followerIdStr = User.FindFirst("sub")?.Value
                                ?? User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;

            if (string.IsNullOrEmpty(followerIdStr)) return Unauthorized();

            // 2. Parsiranje u Guid (jer tvoj MongoService traži Guid)
            if (!Guid.TryParse(followerIdStr, out Guid currentId) ||
                !Guid.TryParse(targetId, out Guid targetGuid))
            {
                return BadRequest("Nevalidan ID format.");
            }

            try
            {
                // 3. Poziv tvoje funkcije iz servisa
                await _mongo.Follow(currentId, targetGuid);

                return Ok(new { message = "Uspešno zapraćeno!" });
            }
            catch (Exception ex)
            {
                return BadRequest(new { error = ex.Message });
            }
        }

        //----------------------------------------------------------------------------------------
        [Authorize]
        [HttpPost("user/unfollow")]
        public async Task<IActionResult> UnfollowUser([FromQuery] string targetId)
        {
            // 1. Izvuci svoj ID (pazi da koristiš isti način kao u Follow metodi)
            var followerIdStr = User.FindFirst("sub")?.Value
                                ?? User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;

            if (string.IsNullOrEmpty(followerIdStr)) return Unauthorized();

            // 2. Parsiraj stringove u Guid
            if (!Guid.TryParse(followerIdStr, out Guid currentId) ||
                !Guid.TryParse(targetId, out Guid targetGuid))
            {
                return BadRequest("Nevalidan ID format.");
            }

            try
            {
                // 3. ISKORISTI SVOJU FUNKCIJU IZ SERVISA (ona već radi Pull na obe strane)
                await _mongo.Unfollow(currentId, targetGuid);

                // Opciono: Ako ikada dodaš Neo4j, ovde otkomentarišeš
                // await _neo4jService.UnfollowUserAsync(followerIdStr, targetId);

                return Ok(new { Message = "Uspešno otpraćeno!" });
            }
            catch (Exception ex)
            {
                return BadRequest(ex.Message);
            }
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