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

        //---restaurant_views------------------------------------------------------------
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

        //---user_saved_restaurants------------------------------------------------------------------------
        [HttpGet("users/{userId}/saved")]
        public async Task<IActionResult>GetUserSavedRestaurants(Guid userId)
        {
            var saved = await _cassandra.GetUserSavedRestaurants(userId);
            return Ok(saved);
        }

        //---restaurant_ratings---------------------------------------------------------------------------
        [HttpGet("restaurants/{id}/ratings")]
        public async Task<IActionResult> GetRestaurantRatings(Guid id)
        {
            var ratings = await _cassandra.GetRestaurantRatings(id);
            return Ok(ratings);
        }

        [HttpGet("restaurants/{id}/ratings/user/{userId}")]
        public async Task<IActionResult> GetRestaurantRatingsByUser(Guid id, Guid userId)
        {
            var ratings = await _cassandra.GetRestaurantRatingsByUser(id, userId);
            return Ok(ratings);
        }

        [HttpGet("restaurants/{id}/rating/average")]
        public async Task<IActionResult> GetAverageRestaurantRating(Guid id)
        {
            var ratings = await _cassandra.GetRestaurantRatings(id);

            var avg = 0;
            var i = 0;
            foreach(var r in ratings)
            {
                avg += r.RatingValue;
                i+=1;
            }
            avg /= i;
            return Ok(avg);
        }

        [HttpGet("restaurants/{id}/ratingscount")]
        public async Task<IActionResult> GetRestaurantsRatingsCount(Guid id)
        {
            var count = await _cassandra.GetRestaurantRatingsCount(id);
            return Ok(new {RestaurantId = id, RatingsCount = count});
        }

        //---restaurant_review_events-------------------------------------------------------------------------
        [HttpGet("restaurants/{id}/reviews")]
        public async Task<IActionResult> GetRestaurantReviews(Guid id)
        {
            var reviews = await _cassandra.GetRestaurantReview(id);
    
            return Ok(reviews);
        }

        [HttpGet("restaurants/{id}/reviewsfromto")]
        public async Task<IActionResult> GetRestaurantReviewsToFrom(Guid id, [FromQuery]DateTime to, [FromQuery]DateTime ffrom)
        {
            to = to.ToUniversalTime();
            ffrom = ffrom.ToUniversalTime();
            var reviews = await _cassandra.GetRestaurantReviewsFromTo(id, ffrom, to);
            return Ok(reviews);
        }

        [HttpGet("restaurants/{id}/reviewscount")]
        public async Task<IActionResult> GetRestaurantReviewsCount(Guid id)
        {
            var count = await _cassandra.GetRestaurantReviewCount(id);
            return Ok(new {RestaurantId = id, ReviewCount = count});
        }

        //---restaurant_checkins------------------------------------------------------------
        [HttpGet("restaurants/{id}/checkins")]
        public async Task<IActionResult> GetRestaurantCheckins(Guid id)
        {
            var checkins = await _cassandra.GetRestaurantCheckins(id);
    
            return Ok(checkins);
        }

        [HttpGet("restaurants/{id}/checkinsfromto")]
        public async Task<IActionResult> GetRestaurantCheckinsToFrom(Guid id, [FromQuery]DateTime to, [FromQuery]DateTime ffrom)
        {
            to = to.ToUniversalTime();
            ffrom = ffrom.ToUniversalTime();
            var checkins = await _cassandra.GetRestaurantCheckinsFromTo(id, ffrom, to);
            return Ok(checkins);
        }

        [HttpGet("restaurants/{id}/checkinscount")]
        public async Task<IActionResult> GetRestaurantCheckinsCount(Guid id)
        {
            var count = await _cassandra.GetRestaurantCheckinsCount(id);
            return Ok(new {RestaurantId = id, CheckinsCount = count});
        }
    }
}