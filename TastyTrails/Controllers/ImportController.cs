using Microsoft.AspNetCore.Mvc;
using TastyTrails.Models;
using TastyTrails.Services;

namespace TastyTrails.Controllers
{
    [ApiController]
    [Route("api/post")]
    public class ImportController : ControllerBase
    {
        private readonly OverpassService _overpass;
        private readonly CassandraService _cassandra;

        public ImportController()
        {
            _overpass = new OverpassService();
            _cassandra = new CassandraService();
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
            }

            return Ok($"{restaurants.Count} restaurants inserted for {city}.");
        }

        [HttpPost("PostUser")]
        public async Task<IActionResult> PostUser(CassandraUser u)
        {
            u.Id = Guid.NewGuid();
            await _cassandra.InsertUserAsync(u);

            return Ok($"User {u.Username} inserted with {u.Id} id.");
        }

        [HttpPost("{id}/view")]
        public async Task<IActionResult> PostRestaurantView(Guid id, [FromBody]Guid userId)
        {
            var view = new CassandraRestaurantView
            {
                RestaurantId = id,
                UserId = userId,
                ViewedAt = DateTime.UtcNow
            };

            await _cassandra.InsertRestaurantView(view);

            //we'll also be updating rending from here but not atm

            return Ok();
        }
    }
}