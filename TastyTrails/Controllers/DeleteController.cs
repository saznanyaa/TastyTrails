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

        public DeleteController()
        {
            _cassandra = new CassandraService();
        }

        [HttpDelete("users/{userId}/saved/{restaurantId}")]
        public async Task<IActionResult> DeleteUserSavedRestaurant(Guid userId, Guid restaurantId)
        {
            await _cassandra.DeleteUserSavedRestaurant(userId, restaurantId);
            return Ok(new { Message = $"Restaurant {restaurantId} removed from user {userId}'s saved list." });
        }
    }
}