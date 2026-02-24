using Microsoft.AspNetCore.Mvc;
using TastyTrails.Models;
using TastyTrails.Services;

namespace TastyTrails.Controllers
{
    [ApiController]
    [Route("api/get")]
    public class GetController:ControllerBase
    {
        private readonly CassandraService _cassandra;

        public GetController()
        {
            _cassandra = new CassandraService();
        }

        [HttpGet("restaurants/{id}/views")]
        public async Task<IActionResult> GetRestaurantViews(Guid id)
        {
            var views = await _cassandra.GetRestaurantViewsAsync(id);
    
            return Ok(views);
        }

        [HttpGet("restaurants/{id}/viewsfromto")]
        public async Task<IActionResult> GetRestaurantViewsToFrom(Guid id, [FromQuery]DateTime to, [FromQuery]DateTime ffrom)
        {
            to = to.ToUniversalTime();
            ffrom = ffrom.ToUniversalTime();
            var views = await _cassandra.GetRestaurantViewsFromToAsync(id, ffrom, to);
            return Ok(views);
        }

        [HttpGet("restaurants/{id}/viewscount")]
        public async Task<IActionResult> GetRestaurantViewsCount(Guid id)
        {
            var count = await _cassandra.GetRestaurantViewCountAsync(id);
            return Ok(new {RestaurantId = id, ViewCount = count});
        }
    }
}