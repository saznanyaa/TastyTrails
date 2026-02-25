using Microsoft.AspNetCore.Mvc;
using TastyTrails.Models;
using TastyTrails.Services;

namespace TastyTrails.Controllers
{
    [ApiController]
    [Route("api/post")]
    public class PostController : ControllerBase
    {
        private readonly OverpassService _overpass;
        private readonly CassandraService _cassandra;

        public PostController()
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
                ViewedAt = DateTime.Now.ToUniversalTime()
            };

            await _cassandra.InsertRestaurantView(view);

            //we'll also be updating rending from here but not atm

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
        public async Task<IActionResult> PostRestaurantReview(Guid id, [FromQuery]Guid userId)
        {
            var review = new CassandraRestaurantReview
            {
                RestaurantId = id,
                UserId = userId,
                ReviewedAt = DateTime.Now.ToUniversalTime()
            };
            await _cassandra.PostRestaurantReview(review);
            return Ok(review);
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

    }
}