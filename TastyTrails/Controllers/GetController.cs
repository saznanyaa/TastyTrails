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

        //trending resturants by city
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

        //similar restaurants to the selected one
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
            }).ToList().Take(3);

            return Ok(result);
        }

        //weekly popularity
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

        //restaurants by city
        [HttpGet("restaurants/bycity/{city}")]
        public async Task<IActionResult> GetRestaurantsByCity(string city) 
        {
            var restaurantIds = await _cassandra.GetRestaurantIdsByCity(city);

            var restaurants = new List<MongoRestaurant>();

            foreach(var id in restaurantIds)
            {
                var rest = await _mongo.GetRestaurantById(id);
                if(rest != null)
                    restaurants.Add(rest);
            }

             var result = restaurants.Select(r => new
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

                if(mongoReview == null)
                    continue;

                var mongoUser = await _mongo.GetUserById(r.UserId);

                result.Add(new
                {
                    UserId = r.UserId,
                    Username = mongoUser?.Username ?? "Nepoznato",
                    ProfilePicture = mongoUser?.ProfileImage ?? "Nema slike",
                    Rating = mongoReview.Rating,
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

        [HttpGet("restaurants/{id}/reviewscount")]
        public async Task<IActionResult> GetRestaurantReviewsCount(Guid id)
        {
            var count = await _cassandra.GetRestaurantReviewCount(id);
            return Ok(new {RestaurantId = id, ReviewCount = count});
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

        //-----------------------------------------------------------------------
        [HttpGet("users/search")] // Path: /api/get/users/search
        public async Task<IActionResult> SearchUsers([FromQuery] string username)
        {
            if (string.IsNullOrWhiteSpace(username))
                return Ok(new List<MongoUser>());

            var results = await _mongo.SearchUsersAsync(username);
            return Ok(results);
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