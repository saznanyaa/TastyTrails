using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Mvc;
using TastyTrails.Models;
using TastyTrails.Services;

namespace TastyTrails.Controllers
{
    [ApiController]
    [Route("api/delete")]
    public class DeleteController:ControllerBase
    {
        private readonly CassandraService _cassandra;
        private readonly MongoService _mongo;
        private readonly INeo4jService _neo4jService;

        public DeleteController(MongoService mg, INeo4jService neo4j)
        {
            _cassandra = new CassandraService();
            _mongo = mg;
            _neo4jService = neo4j;
        }

        [HttpDelete("review/{rId}")]
        public async Task<IActionResult> DeleteReview(Guid rId, [FromQuery]Guid userId)
        {
            var deleted = await _mongo.DeleteReview(rId, userId);
            if(!deleted) return NotFound("Review not found!");
            return Ok(deleted);
        }

        [HttpDelete("user/{id}")]
        public async Task<IActionResult> DeleteUser(string id)
        {
            await _neo4jService.DeleteUserAsync(id);
            return Ok("Korisnik obrisan.");
        }

        [HttpDelete("restaurant/{id}")]
        public async Task<IActionResult> DeleteRestaurant(string id)
        {
            await _neo4jService.DeleteRestaurantAsync(id);
            return Ok("Restoran obrisan.");
        }

        /*[HttpDelete("sync-delete-user/{userId}")]
        public async Task<IActionResult> SyncDeleteUser(string userId)
        {
            try
            {
                // 1. NEO4J DEO - Koristimo metodu koju već imaš u servisu (linija 37 tvog koda)
                await _neo4jService.DeleteUserAsync(userId);

                // 2. MONGODB DEO - Moramo pretvoriti string u Guid jer tvoj Mongo to traži
                if (Guid.TryParse(userId, out Guid userGuid))
                {
                    // Pozivamo sinhronu metodu bez await-a jer tako definisana u tvom servisu
                    _mongo.DeleteUser(userGuid);
                }
                else
                {
                    return BadRequest("Prosleđeni ID nije u ispravnom Guid formatu.");
                }

                return Ok(new
                {
                    Message = "Korisnik uspešno obrisan iz obe baze (Synchronized).",
                    UserId = userId
                });
            }
            catch (Exception ex)
            {
                return StatusCode(500, $"Greška pri sinhronizovanom brisanju: {ex.Message}");
            }
        }*/

        [HttpDelete("user/unfollow/{followerId}/{followedId}")]
        public async Task<IActionResult> UnfollowUser(string followerId, string followedId)
        {
            await _neo4jService.UnfollowUserAsync(followerId, followedId);
            return Ok(new { Message = "Korisnik više ne prati drugog korisnika." });
        }

    }
}