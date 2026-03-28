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
    //[Authorize]
    public class UsersController:ControllerBase
    {
        private readonly MongoService _mongo;
        private readonly CassandraService _cassandra;
        public UsersController(MongoService mg)
        {
            _mongo = mg;
            _cassandra = new CassandraService();
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

        [HttpPost("{targetId}")]
        public async Task<IActionResult> Follow(Guid targetId)
        {
            var userIdClaim = User.FindFirstValue(ClaimTypes.NameIdentifier);

            if (string.IsNullOrEmpty(userIdClaim))
                return Unauthorized();

            var currentId = Guid.Parse(userIdClaim);

            await _mongo.Follow(currentId, targetId);

            return Ok(new { message = "Followed successfully." });
        }

        [HttpDelete("delete/{targetId}")]
        public async Task<IActionResult> Unfollow(Guid targetId)
        {
            var userIdClaim = User.FindFirstValue(ClaimTypes.NameIdentifier);

            if (string.IsNullOrEmpty(userIdClaim))
                return Unauthorized();

            var currentId = Guid.Parse(userIdClaim);

            await _mongo.Unfollow(currentId, targetId);

            return Ok(new { message = "Unfollowed successfully." });
        }
    }
}