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
    [Route("api/users")]
    [Authorize]
    public class UsersController:ControllerBase
    {
        private readonly MongoService _mongo;
        public UsersController(MongoService mg)
        {
            _mongo = mg;
        }

        private Guid GetUserIdFromToken()
        {
            var claim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            return Guid.Parse(claim!);
        }

        [HttpGet("{id}")]
        public async Task<IActionResult> GetUserById(Guid id)
        {
            var userIdFromToken = GetUserIdFromToken();
            if(userIdFromToken != id) return Forbid();
            
            var user = await _mongo.GetUserById(id);
            if(user == null) return NotFound();

            return Ok(user);
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
    }
}