using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Mvc;
using TastyTrails.Models;
using TastyTrails.Services;

namespace TastyTrails.Controllers
{
    [ApiController]
    [Route("api/delete")]
    public class DeleteController:ControllerBase
    {
        private readonly CassandraService _cassandra;
        private readonly MongoService _mongo;

        public DeleteController(MongoService mg)
        {
            _cassandra = new CassandraService();
            _mongo = mg;
        }

        [HttpDelete("users/{userId}/saved/{restaurantId}")]
        public async Task<IActionResult> DeleteUserSavedRestaurant(Guid userId, Guid restaurantId)
        {
            await _cassandra.DeleteUserSavedRestaurant(userId, restaurantId);
            return Ok(new { Message = $"Restaurant {restaurantId} removed from user {userId}'s saved list." });
        }

        [HttpDelete("review/{rId}")]
        public async Task<IActionResult> DeleteReview(Guid rId, [FromQuery]Guid userId)
        {
            var deleted = await _mongo.DeleteReview(rId, userId);
            if(!deleted) return NotFound("Review not found!");
            return Ok(deleted);
        }
    }
}