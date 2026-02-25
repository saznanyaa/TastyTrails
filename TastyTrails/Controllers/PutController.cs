using Microsoft.AspNetCore.Mvc;
using TastyTrails.Models;
using TastyTrails.Services;

namespace TastyTrails.Controllers
{
    [ApiController]
    [Route("api/put")]
    public class PutController : ControllerBase
    {
        private readonly CassandraService _cassandra;

        public PutController()
        {
            _cassandra = new CassandraService();
        }

        [HttpPut("restaurants/{id}/rating")]
        public async Task<IActionResult> PutRestaurantRating(Guid id, [FromQuery]Guid userId, [FromQuery]int value)
        {
            if(value<1 || value>5)
                return BadRequest("Incorrect rating value!");

            await _cassandra.EditRestaurantRating(id, userId, value);

            return Ok("Rating updated successfully!");
        }
    }
}