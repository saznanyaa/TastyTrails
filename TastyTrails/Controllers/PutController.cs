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
        private readonly MongoService _mongo;

        public PutController(MongoService mng)
        {
            _cassandra = new CassandraService();
            _mongo = mng;
        }

        [HttpPut("restaurants/{id}/rating")]
        public async Task<IActionResult> PutRestaurantRating(Guid id, [FromQuery]Guid userId, [FromQuery]int value)
        {
            if(value<1 || value>5)
                return BadRequest("Incorrect rating value!");

            await _cassandra.EditRestaurantRating(id, userId, value);
            var mongoRev = await _mongo.GetReviewByRestAndUser(id, userId);
            await _mongo.PostReview(id, userId, value, mongoRev.Comment);

            return Ok("Rating updated successfully!");
        }
    }
}