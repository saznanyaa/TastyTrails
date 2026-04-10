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
    }
}