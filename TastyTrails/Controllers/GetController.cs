using System.Runtime.CompilerServices;
using Microsoft.AspNetCore.Mvc;
using Microsoft.VisualBasic;
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

        //trending resturants
        [HttpGet("restaurants/trending/{city}")]
        public async Task<IActionResult> GetTrendingRestaurants(string city)
        {
            var raw = await _cassandra.GetTrendingByCity(city);

            if(!raw.Any())
                return NotFound("Nema trending restorana za dati grad.");

            var top = raw.OrderByDescending(r => r.score).Take(10).ToList();

            var mongoRestaurants = new List<MongoRestaurant>();

            foreach(var t in top)
            {
                var restaurant = await _mongo.GetRestaurantById(t.id);
                if(restaurant != null)
                    mongoRestaurants.Add(restaurant);
            }

            var res = top.Select(t =>
            {
                var mongo = mongoRestaurants.FirstOrDefault(m => m.Id == t.id);

                return new 
                {
                    Id = t.id,
                    Name = mongo != null ? mongo.Name : "Nepoznato",
                    City = city,
                    AverageRating = mongo != null ? mongo.AverageRating : 0,
                    TotalReviews = mongo != null ? mongo.TotalReviews : 0,
                    Score = t.score,
                    Latitude = mongo?.Coordinates?.Lat,
                    Longitude = mongo?.Coordinates?.Lng
                };
            }
            ).OrderByDescending(r => r.Score).ToList();

            return Ok(res);
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

        [HttpGet("restaurants/{id}/mngReviews")]
        public async Task<IActionResult> GetRestaurantMongoReviews(Guid id)
        {
            var reviews = await _mongo.GetRestaurantReviews(id);
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
            var restaurants = await _mongo.GetRestaurants();

            var res = restaurants.Select(r => new
            {
                Id = r.Id,
                Name = r.Name,
                AverageRating = r.AverageRating,
                TotalReviews = r.TotalReviews,
                Latitude = r.Coordinates?.Lat,
                Longitude = r.Coordinates?.Lng
            }).ToList();

            return Ok(res);
        }

        [HttpGet("mongoRest/{id}")]
        public async Task<IActionResult> GetMongoRestaurantById(Guid id)
        {
            var r = await _mongo.GetRestaurantById(id);
            return Ok(r);
        }

        [HttpGet("user/{id}")]
        public async Task<IActionResult> GetUser(Guid id)
        {
            var user = await _mongo.GetUserById(id);

            if (user == null)
            {
                return NotFound("Korisnik nije pronađen u MongoDB bazi.");
            }

            return Ok(user);
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

        //-----------------------------------------------------------------------
        [HttpGet("users/search")] // Path: /api/get/users/search
        public async Task<IActionResult> SearchUsers([FromQuery] string username)
        {
            if (string.IsNullOrWhiteSpace(username))
                return Ok(new List<MongoUser>());

            var results = await _mongo.SearchUsersAsync(username);
            return Ok(results);
        }

        //-----------users--------------------------------------------------------
        [HttpGet("get/user/{id}")]
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