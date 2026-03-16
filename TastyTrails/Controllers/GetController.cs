using System.Runtime.CompilerServices;
using Microsoft.AspNetCore.Mvc;
using TastyTrails.Configurations;
using TastyTrails.Models;
using TastyTrails.Services;

namespace TastyTrails.Controllers
{
    [ApiController]
    [Route("api/get")]
    public class GetController:ControllerBase
    {
        private readonly CassandraService _cassandra;
        private readonly MongoService _mongo;
        private readonly INeo4jService _neo4jService;

        public GetController(MongoService mng, INeo4jService neo4j)
        {
            _cassandra = new CassandraService();
            _mongo = mng;
            _neo4jService = neo4j;
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
            // var mngReviews = await _mongo.GetReviewsByUser(userId);
            // var mngRatings = new List<int>();
            // foreach(var r in mngReviews)
            // {
            //     mngRatings.Add(r.Rating);
            // }
            //nmg da se setim koja je fora s ovim tkd neka ga za sad ovako
            return Ok(ratings);
        }

        //---restaurant_review_events-------------------------------------------------------------------------
        [HttpGet("restaurants/{id}/reviews")]
        public async Task<IActionResult> GetRestaurantReviews(Guid id)
        {
            var reviews = await _cassandra.GetRestaurantReview(id);
            var r = await _mongo.GetRestaurantReviews(id);
    
            return Ok($"cassandra: {reviews}, mongo: {r}");
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

        //---restaurant_rating_summary-------------------------------------------------------
        [HttpGet("restaurants/{id}/rating/average")]
        public async Task<IActionResult> GetAverageRestaurantRating(Guid id)
        {
            var avg = await _cassandra.GetAverageRating(id);

            if (avg == null)
                return Ok(0);

            return Ok(avg);
        }

        [HttpGet("restaurants/{id}/ratingscount")]
        public async Task<IActionResult> GetRestaurantsRatingsCount(Guid id)
        {
            var count = await _cassandra.GetRestaurantRatingsCount(id);
            return Ok(new { RestaurantId = id, RatingsCount = count });
        }

        //-----------------------------------------------------------------------------------
        [HttpGet("mongoRestaurants")]
        public async Task<IActionResult> GetMongoRestaurants()
        {
            var r = await _mongo.GetRestaurants();
            return Ok(r.Count);
        }

        [HttpGet("mongoRest/{id}")]
        public async Task<IActionResult> GetMongoRestaurantById(Guid id)
        {
            var r = await _mongo.GetRestaurantById(id);
            return Ok(r);
        }

        [HttpGet("restaurants/nearme")]
        public async Task<IActionResult> GetRestaurantsNearMe([FromQuery]double lat, [FromQuery] double lng, [FromQuery]double radius=0.01)
        {
            var r = await _mongo.GetRestaurantsNearMe(lat, lng, radius);
            return Ok(r);
        }
        //-----------------------------------------------------------------------------

        [HttpGet("reviews/{id}/mongouser/reviews")]
        public async Task<IActionResult> GetMongoReviewsByUser(Guid id)
        {
            var r = await _mongo.GetReviewsByUser(id);
            return Ok(r);
        }

        //-----------users--------------------------------------------------------
        [HttpGet("user/{id}")]
        public async Task<IActionResult> GetUser(string id)
        {
            var user = await _neo4jService.GetUserByIdAsync(id);
            return user != null ? Ok(user) : NotFound("Korisnik nije pronađen.");
        }

        //--------------------------------------------------------------------------
        [HttpGet("user/{userId}/likes")]
        public async Task<IActionResult> GetLikes(string userId)
        {
            var likes = await _neo4jService.GetUserLikesAsync(userId);
            return Ok(likes);
        }

        //--------------reccomendation-----------------------------------------------
        [HttpGet("user/recommendations/{userId}")]
        public async Task<IActionResult> GetRecommendations(string userId)
        {
            var list = await _neo4jService.GetSmartRecommendationsAsync(userId);
            if (list == null || list.Count == 0) return NotFound("Nema preporuka.");
            return Ok(list);
        }
    }
}