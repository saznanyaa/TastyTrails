using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Mvc;
using TastyTrails.Models;
using TastyTrails.Services;

namespace TastyTrails.Controllers
{
    [ApiController]
    [Route("api/admin")]
    public class AdminController:ControllerBase
    {
        private readonly CassandraService _cassandra;

        public AdminController()
        {
            _cassandra = new CassandraService();
        }

        [HttpPost("weeklytrending")]
        public async Task<IActionResult> PostWeekly([FromQuery]string city, [FromQuery]string cuisine,[FromQuery]DateTime weekStart)
        {
            await _cassandra.FillWeeklyTrendingResults(city, cuisine, weekStart);
            return Ok($"Weekly trending generated for {city} - {cuisine} ({weekStart:yyyy-MM-dd}).");
        }

        [HttpDelete("weekly")]
        public async Task<IActionResult> DeleteWeekly([FromQuery] string city,[FromQuery]string cuisine, [FromQuery] DateTime weekStart)
        {
            await _cassandra.DeleteTrendingWeeklyResults(city, cuisine, weekStart);

            return Ok($"Weekly trending deleted for {city}-{cuisine} ({weekStart:yyyy-MM-dd}).");
        }

    }
}