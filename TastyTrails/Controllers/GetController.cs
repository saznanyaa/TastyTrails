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

            var res = top.Select(async t =>
            {
                var mongo = mongoRestaurants.FirstOrDefault(m => m.Id == t.id);

                return new 
                {
                    Id = t.id,
                    Name = mongo != null ? mongo.Name : "Nepoznato",
                    City = city,
                    AverageRating = await _cassandra.GetAverageRating(t.id) ?? 0,
                    TotalReviews = mongo != null ? mongo.TotalReviews : 0,
                    Score = t.score,
                    Latitude = mongo?.Coordinates?.Lat,
                    Longitude = mongo?.Coordinates?.Lng
                };
            }
            );

            var result = (await Task.WhenAll(res)).OrderByDescending(r => r.Score).ToList();

            return Ok(result);
        }

        [HttpGet("similar/{id}")]
        public async Task<IActionResult> GetSimilarRestaurants(Guid id)
        {
            var similarIds = await _neo4jService.GetSimilarRestaurants(id.ToString());

            var similarRestaurants = new List<MongoRestaurant>();

            foreach (var simId in similarIds)
            {
                var restaurant = await _mongo.GetRestaurantById(Guid.Parse(simId.Id));
                if (restaurant != null)
                    similarRestaurants.Add(restaurant);
            }

            var result = similarRestaurants.Select(r => new
            {
                Id = r.Id,
                Name = r.Name,
                AverageRating = r.AverageRating,
                TotalReviews = r.TotalReviews,
                Latitude = r.Coordinates?.Lat,
                Longitude = r.Coordinates?.Lng
            }).ToList();

            return Ok(result);
        }

        [HttpGet("{id}/analytics/weekly")]
        public async Task<IActionResult> GetWeeklyAnalytics(Guid id)
        {
            var to = DateTime.UtcNow;
            var from = to.AddDays(-7);

            var views = await _cassandra.GetRestaurantViewsFromToAsync(id, from, to);
            var checkins = await _cassandra.GetRestaurantCheckinsFromTo(id, from, to);
            var reviews = await _cassandra.GetRestaurantReviewsFromTo(id, from, to);
            var ratings = await _cassandra.GetAverageRating(id);

            var viewsCount = views.Count();
            var checkinsCount = checkins.Count();
            var reviewsCount = reviews.Count();

            var viewsByDay = views.GroupBy(v => v.ViewedAt.Date)
                                .Select(g => new { Date = g.Key, Count = g.Count() })
                                .OrderBy(g => g.Date)
                                .ToList();

            var analytics = new
            {
                RestaurantId = id,
                AverageRating = ratings ?? 0,
                ViewsCount = viewsCount,
                CheckinsCount = checkinsCount,
                ReviewsCount = reviewsCount,
                ViewsByDay = viewsByDay.Select(v => new { Date = v.Date.ToString("yyyy-MM-dd"), Count = v.Count }).ToList()
            };

            return Ok(analytics);
        }

       
        //---restaurant_review_events-------------------------------------------------------------------------
        [HttpGet("{id}/reviews/recent")]
        public async Task<IActionResult> GetRecentReviews(Guid id)
        {
            var to = DateTime.UtcNow;
            var from = to.AddDays(-3);

            var reviews = await _cassandra.GetRestaurantReviewsFromTo(id, from, to);

            var latest = reviews
                .OrderByDescending(r => r.ReviewedAt)
                .Take(2)
                .ToList();

            var result = new List<object>();
            foreach(var r in latest)
            {
                var mongoReview = await _mongo.GetReviewByRestAndUser(r.RestaurantId, r.UserId);

                var mongoUser = await _mongo.GetUserById(r.UserId);

                result.Add(new
                {
                    UserId = r.UserId,
                    Username = mongoUser?.Username ?? "Nepoznato",
                    ProfilePicture = mongoUser?.ProfileImage ?? "Nema slike",
                    Rating = mongoReview?.Rating ?? 0,
                    Comment = mongoReview?.Comment ?? "Nema komentara",
                    ReviewedAt = r.ReviewedAt
                });
            }
            return Ok(result);
        }

        [HttpGet("restaurants/{id}/mngReviews")]
        public async Task<IActionResult> GetRestaurantMongoReviews(Guid id)
        {
            var reviews = await _mongo.GetRestaurantReviews(id);
            return Ok(reviews);
        }

        //to show most recent reviews in the popup
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


        //----------------------------------------------------------------------------
        [HttpGet("reviews/{userId}")]
        public async Task<IActionResult> GetUserReviews(Guid userId)
        {
            var reviews = await _mongo.GetReviewsByUser(userId);
            return Ok(reviews);
        }
    }
}