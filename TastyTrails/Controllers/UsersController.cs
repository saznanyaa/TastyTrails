using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Mvc;
using TastyTrails.Models;
using TastyTrails.Services;
using System.Security.Claims;
using TastyTrails.Models.DTOs;

namespace TastyTrails.Controllers
{
    [ApiController]
    [Route("api/user")]
    [Authorize]
    public class UsersController:ControllerBase
    {
        private readonly MongoService _mongo;
        private readonly CassandraService _cassandra;
        private readonly INeo4jService _neo4jService;
        public UsersController(MongoService mg, INeo4jService neo4j)
        {
            _mongo = mg;
            _cassandra = new CassandraService();
            _neo4jService = neo4j;
        }

        private Guid GetUserIdFromToken()
        {
            var claim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            return Guid.Parse(claim!);
        }

        [HttpGet("{id}")]
        public async Task<IActionResult> GetUserById(Guid id)
        {
            try
            {
                var user = await _mongo.GetUserById(id);

                if (user == null)
                {
                    return NotFound(new { message = "Korisnik nije pronadjen.", trazeniId = id });
                }

                return Ok(user);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { error = ex.Message });
            }
        }

        [HttpPut("{id}")]
        public async Task<IActionResult> UpdateUsernameOrEmail(Guid id, UpdateDTO dto)
        {
            var userIdFromToken = GetUserIdFromToken();
            if(userIdFromToken != id) return Forbid();
            
            var updated = await _mongo.UpdateUsernameOrEmail(id, dto);
            if(!updated) return NotFound();

            return Ok(updated);
        }

        [HttpDelete("{id}")]
        public async Task<IActionResult> DeleteUser(Guid id)
        {
            var userIdFromToken = GetUserIdFromToken();
            if(userIdFromToken != id) return Forbid();

            var deleted = await _mongo.DeleteUser(id);

            if (!deleted) return NotFound();

            return NoContent();
        }

//---saved restaurants--------------------------------------------------------------------------------

        [HttpPost("{id}/saved/{restaurantId}")]
        public async Task<IActionResult> PostSavedRestaurant(Guid id, Guid restaurantId)
        {
            var userIdFromToken = GetUserIdFromToken();
            if (userIdFromToken != id) return Forbid();

            var rest = await _cassandra.GetCityAndCuisine(restaurantId);

            try
            {
                var result = await _mongo.PostUserSavedRestaurnts(id, restaurantId);

                var saved = new CassandraSavedRestaurants
                {
                    UserId = id,
                    RestaurantId = restaurantId,
                    SavedAt = DateTime.UtcNow
                };

                await _cassandra.InsertUserSavedRestaurants(saved);
                await _cassandra.IncreaseTrending(rest.City, rest.Cuisine, restaurantId, 5);
                
                return Ok(result);
            }
            catch
            {
                return NotFound();
            }
        }

        [HttpDelete("{id}/saved/{restaurantId}")]
        public async Task<IActionResult> DeleteSavedRestaurant(Guid id, Guid restaurantId)
        {
            var userIdFromToken = GetUserIdFromToken();
            if (userIdFromToken != id) return Forbid();

            try
            {
                await _cassandra.DeleteUserSavedRestaurant(id, restaurantId);
                var result = await _mongo.DeleteUserSavedRestaurant(id, restaurantId);
                return Ok(result);
            }
            catch
            {
                return NotFound();
            }
        }

        [HttpGet("{id}/saved")]
        public async Task<IActionResult> GetSavedRestaurants(Guid id)
        {
            var userIdFromToken = GetUserIdFromToken();
            if (userIdFromToken != id) return Forbid();

            try
            {
                var restaurants = await _mongo.GetUserSavedRestaurants(id);
                return Ok(restaurants);
            }
            catch
            {
                return NotFound();
            }
        }

//---visited restaurants--------------------------------------------------------------------------------
        [HttpPost("{id}/visited/{restaurantId}")]
        public async Task<IActionResult> PostVisitedRestaurant(Guid id, Guid restaurantId)
        {
            var userIdFromToken = GetUserIdFromToken();
            if (userIdFromToken != id) return Forbid();

            var rest = await _cassandra.GetCityAndCuisine(restaurantId);

            try
            {
                var result = await _mongo.PostUserVisitedRestaurant(id, restaurantId);

                var visited = new CassandraRestaurantCheckins
                {
                    RestaurantId = restaurantId,
                    CheckedInAt = DateTime.UtcNow,
                    UserId = id
                };

                await _cassandra.PostRestaurantCheckin(visited);
                await _cassandra.IncreaseTrending(rest.City, rest.Cuisine, restaurantId, 7);

                return Ok(result);
            }
            catch
            {
                return NotFound();
            }
        }

//if recently viewed gets imlemented
        [HttpGet("{id}/visited")]
        public async Task<IActionResult> GetVisitedRestaurants(Guid id)
        {
            var userIdFromToken = GetUserIdFromToken();
            if (userIdFromToken != id) return Forbid();

            try
            {
                var restaurants = await _mongo.GetUserVisitedRestaurants(id);
                return Ok(restaurants);
            }
            catch
            {
                return NotFound();
            }
        }
//---follow/unfollow--------------------------------------------------------------------------------
        [HttpPost("follow/{targetId}")]
        public async Task<IActionResult> Follow([FromRoute] Guid targetId)
        {
            var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (string.IsNullOrEmpty(userIdClaim)) return Unauthorized("Korisnik nije prepoznat iz tokena");

            var currentId = Guid.Parse(userIdClaim);
            await _mongo.Follow(currentId, targetId);
            await _neo4jService.FollowUserAsync(currentId.ToString(), targetId.ToString());
           
            return Ok(new { message = "Followed successfully", followerId = currentId, followedId = targetId });
        }

        [HttpDelete("unfollow/{targetId}")]
        public async Task<IActionResult> Unfollow(Guid targetId)
        {
            var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (string.IsNullOrEmpty(userIdClaim)) return Unauthorized();

            var currentId = Guid.Parse(userIdClaim);
            await _mongo.Unfollow(currentId, targetId);
            await _neo4jService.UnfollowUserAsync(currentId.ToString(), targetId.ToString());
            return Ok(new { message = "Unfollowed successfully", followerId = currentId, followedId = targetId });
        }
  
//---reviews--------------------------------------------------------------------------------
        [HttpPost("{id}/review/{restaurantId}")]
        public async Task<IActionResult> PostRestaurantReview(Guid id, Guid restaurantId, [FromBody] MongoReview mongoReview)
        {
            var userIdFromToken = GetUserIdFromToken();
            if (userIdFromToken != id) return Forbid();

            var rest = await _cassandra.GetCityAndCuisine(restaurantId);
    
            var review = new CassandraRestaurantReview
            {
                RestaurantId = restaurantId,
                UserId = id,
                ReviewedAt = DateTime.Now.ToUniversalTime()
            };
            await _cassandra.PostRestaurantReview(review);

            await _cassandra.PostRestaurantRating(restaurantId, id, mongoReview.Rating);

            await _mongo.PostReview(restaurantId, id, mongoReview.Rating, mongoReview.Comment);

            await _neo4jService.ReviewRestaurant(id.ToString(), restaurantId.ToString(), mongoReview.Rating);

            await _cassandra.IncreaseTrending(rest.City, rest.Cuisine, restaurantId, 10);

            return Ok(new { message = "Review posted successfully."});
        }
        
        //you do not delete reviews from cassandra, because you can't undo an action there, because it stores history, not reviews themselves
        [HttpDelete("{userId}/review/{rId}")]
        public async Task<IActionResult> DeleteReview(Guid userId, Guid rId)
        {
            var restaurant = await _mongo.GetRestaurantByReviewId(rId);
            var restId = restaurant.Id;
            await _neo4jService.DeleteReview(userId.ToString(), restId.ToString());
            var deleted = await _mongo.DeleteReview(rId, userId);
            if(!deleted) return NotFound("Review not found!");
            return Ok(deleted);
        } 

        [HttpGet("{id}/reviews")]
        public async Task<IActionResult> GetUserReviews(Guid id)
        {
            var userIdFromToken = GetUserIdFromToken();
            if (userIdFromToken != id) return Forbid();

            try
            {
                var reviews = await _mongo.GetReviewsByUser(id);
                return Ok(reviews);
            }
            catch
            {
                return NotFound();
            }
        }

        [HttpPut("review/update")]
        public async Task<IActionResult> UpdateReview([FromBody] System.Text.Json.JsonElement data)
        {
            try
            {
                // Izvlačenje podataka iz JsonElement-a (ne puca ako je tip pogrešan)
                string id = data.GetProperty("id").GetString();

                // Sigurno izvlačenje broja
                int rating = 0;
                if (data.GetProperty("rating").ValueKind == System.Text.Json.JsonValueKind.Number)
                    rating = data.GetProperty("rating").GetInt32();
                else if (data.GetProperty("rating").ValueKind == System.Text.Json.JsonValueKind.String)
                    int.TryParse(data.GetProperty("rating").GetString(), out rating);

                string comment = data.GetProperty("comment").GetString();

                if (string.IsNullOrEmpty(id) || rating < 1 || rating > 5)
                    return BadRequest("Podaci nisu validni.");

                bool success = await _mongo.UpdateReviewAsync(id, rating, comment);

                return success ? Ok() : NotFound("Recenzija nije pronađena.");
            }
            catch (Exception ex)
            {
                // Ako ovde uđe, ispisaće ti u konzoli tačno šta fali u JSON-u
                Console.WriteLine($"Greška: {ex.Message}");
                return StatusCode(500, "Greška pri obradi JSON-a");
            }
        }

//---recommendations--------------------------------------------------------------------------------
        [HttpGet("{id}/recommendations/{city}")]
        public async Task<IActionResult> GetRecommendations(Guid id, string city)
        {
            var userIdFromToken = GetUserIdFromToken();
            if (userIdFromToken != id) return Forbid();

            try
            {
                var recommendations = await _neo4jService.GetRecommendations(id.ToString());
                var trending = await _cassandra.GetTrendingByCity(city);
                var trendingScores = trending.ToDictionary(
                    x => x.id,
                    x => (int)x.score
                );

                foreach(var r in recommendations)
                {
                    var restId = Guid.Parse(r.Id);

                    if(trendingScores.TryGetValue(restId, out var score))
                    {
                        r.Score = (int)(r.Score*0.7 + score*0.3);
                    }

                }

                var final = recommendations.OrderByDescending(r => r.Score).Take(10).ToList();

                var finalWithLocation = final.Select(async r => {
                    var mongoRest = await _mongo.GetRestaurantById(Guid.Parse(r.Id));
                    if (mongoRest != null)
                    {
                        return new
                        {
                            r.Id,
                            r.Name,
                            r.Score,
                            r.Cuisine,
                            Latitude = mongoRest.Coordinates?.Lat,
                            Longitude = mongoRest.Coordinates?.Lng,
                            mongoRest.AverageRating,
                            mongoRest.TotalReviews
                        };
                    }
                    return null; 
                });

                var fin = ( await Task.WhenAll(finalWithLocation))
                                .Where(x => x != null).ToList();

                return Ok(fin);    
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { error = ex.Message });
            }
        }

       

        [HttpGet("relations/{userId}/{type}")]
        public async Task<IActionResult> GetRelations(Guid userId, string type)
        {
            // Validacija tipa (opciono, ali dobra praksa)
            if (type.ToLower() != "followers" && type.ToLower() != "following")
                return BadRequest("Tip mora biti 'followers' ili 'following'.");

            try
            {
                var result = await _mongo.GetFollowDetailsAsync(userId, type);
                return Ok(result);
            }
            catch (Exception ex)
            {
                return StatusCode(500, $"Greška na serveru: {ex.Message}");
            }
        }
    }
}