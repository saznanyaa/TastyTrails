using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Mvc;
using TastyTrails.Models;
using TastyTrails.Services;
using System.Security.Claims;
using TastyTrails.Models.DTOs;

namespace TastyTrails.Controllers
{
    [ApiController]
    [Route("api/user")]
    [Authorize]
    public class UsersController:ControllerBase
    {
        private readonly MongoService _mongo;
        private readonly CassandraService _cassandra;
        private readonly INeo4jService _neo4jService;
        public UsersController(MongoService mg, INeo4jService neo4j)
        {
            _mongo = mg;
            _cassandra = new CassandraService();
            _neo4jService = neo4j;
        }

        private Guid GetUserIdFromToken()
        {
            var claim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            return Guid.Parse(claim!);
        }

        [HttpGet("{id}")]
        public async Task<IActionResult> GetUserById(Guid id)
        {
            try
            {
                var user = await _mongo.GetUserById(id);

                if (user == null)
                {
                    return NotFound(new { message = "Korisnik nije pronadjen.", trazeniId = id });
                }

                return Ok(user);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { error = ex.Message });
            }
        }

        [HttpPut("{id}")]
        public async Task<IActionResult> UpdateUsernameOrEmail(Guid id, UpdateDTO dto)
        {
            var userIdFromToken = GetUserIdFromToken();
            if(userIdFromToken != id) return Forbid();
            
            var updated = await _mongo.UpdateUsernameOrEmail(id, dto);
            if(!updated) return NotFound();

            return Ok(updated);
        }

        [HttpDelete("{id}")]
        public async Task<IActionResult> DeleteUser(Guid id)
        {
            var userIdFromToken = GetUserIdFromToken();
            if(userIdFromToken != id) return Forbid();

            var deleted = await _mongo.DeleteUser(id);

            if (!deleted) return NotFound();

            return NoContent();
        }

        [HttpPost("{id}/saved/{restaurantId}")]
        public async Task<IActionResult> PostSavedRestaurant(Guid id, Guid restaurantId)
        {
            var userIdFromToken = GetUserIdFromToken();
            if (userIdFromToken != id) return Forbid();

            try
            {
                var result = await _mongo.PostUserSavedRestaurnts(id, restaurantId);

                var saved = new CassandraSavedRestaurants
                {
                    UserId = id,
                    RestaurantId = restaurantId,
                    SavedAt = DateTime.UtcNow
                };

                await _cassandra.InsertUserSavedRestaurants(saved);
                
                return Ok(result);
            }
            catch
            {
                return NotFound();
            }
        }

        [HttpDelete("{id}/saved/{restaurantId}")]
        public async Task<IActionResult> DeleteSavedRestaurant(Guid id, Guid restaurantId)
        {
            var userIdFromToken = GetUserIdFromToken();
            if (userIdFromToken != id) return Forbid();

            try
            {
                var result = await _mongo.DeleteUserSavedRestaurant(id, restaurantId);
                return Ok(result);
            }
            catch
            {
                return NotFound();
            }
        }

        [HttpGet("{id}/saved")]
        public async Task<IActionResult> GetSavedRestaurants(Guid id)
        {
            var userIdFromToken = GetUserIdFromToken();
            if (userIdFromToken != id) return Forbid();

            try
            {
                var restaurants = await _mongo.GetUserSavedRestaurants(id);
                return Ok(restaurants);
            }
            catch
            {
                return NotFound();
            }
        }

        [HttpPost("{id}/visited/{restaurantId}")]
        public async Task<IActionResult> PostVisitedRestaurant(Guid id, Guid restaurantId)
        {
            var userIdFromToken = GetUserIdFromToken();
            if (userIdFromToken != id) return Forbid();

            try
            {
                var result = await _mongo.PostUserVisitedRestaurant(id, restaurantId);

                var visited = new CassandraRestaurantCheckins
                {
                    RestaurantId = restaurantId,
                    CheckedInAt = DateTime.UtcNow,
                    UserId = id
                };

                await _cassandra.PostRestaurantCheckin(visited);

                return Ok(result);
            }
            catch
            {
                return NotFound();
            }
        }

        [HttpGet("{id}/visited")]
        public async Task<IActionResult> GetVisitedRestaurants(Guid id)
        {
            var userIdFromToken = GetUserIdFromToken();
            if (userIdFromToken != id) return Forbid();

            try
            {
                var restaurants = await _mongo.GetUserVisitedRestaurants(id);
                return Ok(restaurants);
            }
            catch
            {
                return NotFound();
            }
        }

        [HttpPost("follow/{targetId}")]
        public async Task<IActionResult> Follow([FromRoute] Guid targetId)
        {
            var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (string.IsNullOrEmpty(userIdClaim)) return Unauthorized("Korisnik nije prepoznat iz tokena");

            var currentId = Guid.Parse(userIdClaim);
            await _mongo.Follow(currentId, targetId);
            await _neo4jService.FollowUserAsync(currentId.ToString(), targetId.ToString());
           
            return Ok(new { message = "Followed successfully", followerId = currentId, followedId = targetId });
        }

        [HttpPost("unfollow/{targetId}")]
        public async Task<IActionResult> Unfollow(Guid targetId)
        {
            var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (string.IsNullOrEmpty(userIdClaim)) return Unauthorized();

            var currentId = Guid.Parse(userIdClaim);
            await _mongo.Unfollow(currentId, targetId);
            await _neo4jService.UnfollowUserAsync(currentId.ToString(), targetId.ToString());
            return Ok(new { message = "Unfollowed successfully", followerId = currentId, followedId = targetId });
        }

        [HttpPost("connect/{restaurantId}")]
        public async Task<IActionResult> Connect(Guid restaurantId)
        {
            var userIdClaim = User.FindFirstValue(ClaimTypes.NameIdentifier);

            if (string.IsNullOrEmpty(userIdClaim))
                return Unauthorized();

            var currentId = Guid.Parse(userIdClaim);
            var type = "LIKE";

            await _neo4jService.ConnectUserToRestaurantAsync(currentId.ToString(), restaurantId.ToString(), type);

            return Ok(new { message = "Connected successfully." });
        }  
    
        [HttpPost("{id}/review/{restaurantId}")]
        public async Task<IActionResult> PostRestaurantReview(Guid id, Guid restaurantId, [FromBody] MongoReview mongoReview)
        {
                var userIdFromToken = GetUserIdFromToken();
                if (userIdFromToken != id) return Forbid();
    
                var review = new CassandraRestaurantReview
                {
                    RestaurantId = restaurantId,
                    UserId = id,
                    ReviewedAt = DateTime.Now.ToUniversalTime()
                };
                await _cassandra.PostRestaurantReview(review);

                await _cassandra.PostRestaurantRating(restaurantId, id, mongoReview.Rating);

                await _mongo.PostReview(restaurantId, id, mongoReview.Rating, mongoReview.Comment);
    
                return Ok(new { message = "Review posted successfully." });
        }
    }
}