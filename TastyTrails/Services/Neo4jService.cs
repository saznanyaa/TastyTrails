using Neo4j.Driver;
using TastyTrails.Models;

namespace TastyTrails.Services
{
    public class Neo4jService : INeo4jService
    {
        private readonly IDriver _driver;

        public Neo4jService(IDriver driver)
        {
            _driver = driver;
        }

        public async Task CreateUserNodeAsync(NeoUserNode user)
        {
            var session = _driver.AsyncSession();
            try
            {
                await session.ExecuteWriteAsync(async tx => {
                    await tx.RunAsync("MERGE (u:User {id: $id}) SET u.username = $username",
                        new { id = user.Id, username = user.Username });
                });
            }
            finally { await session.CloseAsync(); }
        }

        public async Task CreateRestaurantNodeAsync(NeoRestaurantNode restaurant)
        {
            var session = _driver.AsyncSession();
            try
            {
                await session.ExecuteWriteAsync(async tx => {
                    await tx.RunAsync("MERGE (r:Restaurant {id: $id}) SET r.name = $name",
                        new { id = restaurant.Id, name = restaurant.Name });
                });
            }
            finally { await session.CloseAsync(); }
        }

        public async Task ConnectUserToRestaurantAsync(string userId, string restaurantId, string relationType)
        {
            var session = _driver.AsyncSession();
            try
            {
                await session.ExecuteWriteAsync(async tx => {
                    var query = $"MATCH (u:User {{id: $userId}}), (r:Restaurant {{id: $restaurantId}}) " +
                                $"MERGE (u)-[:{relationType.ToUpper()}]->(r)";
                    await tx.RunAsync(query, new { userId, restaurantId });
                });
            }
            finally { await session.CloseAsync(); }
        }

        public async Task<NeoUserNode?> GetUserByIdAsync(string id)
        {
            var session = _driver.AsyncSession();
            try
            {
                return await session.ExecuteReadAsync(async tx => {
                    var cursor = await tx.RunAsync("MATCH (u:User {id: $id}) RETURN u.id, u.username", new { id });
                    if (await cursor.FetchAsync())
                    {
                        return new NeoUserNode
                        {
                            Id = cursor.Current["u.id"].As<string>(),
                            Username = cursor.Current["u.username"].As<string>()
                        };
                    }
                    return null;
                });
            }
            finally { await session.CloseAsync(); }
        }

        public async Task<List<NeoRestaurantNode>> GetUserLikesAsync(string userId)
        {
            var session = _driver.AsyncSession();
            try
            {
                return await session.ExecuteReadAsync(async tx => {
                    var cursor = await tx.RunAsync(
                        "MATCH (u:User {id: $userId})-[:LIKE]->(r:Restaurant) RETURN r.id, r.name",
                        new { userId });
                    var results = await cursor.ToListAsync();
                    return results.Select(record => new NeoRestaurantNode
                    {
                        Id = record["r.id"].As<string>(),
                        Name = record["r.name"].As<string>()
                    }).ToList();
                });
            }
            finally { await session.CloseAsync(); }
        }

        public async Task DeleteUserAsync(string userId)
        {
            var session = _driver.AsyncSession();
            try
            {
                await session.ExecuteWriteAsync(async tx => {
                    await tx.RunAsync("MATCH (u:User {id: $userId}) DETACH DELETE u", new { userId });
                });
            }
            finally { await session.CloseAsync(); }
        }

        public async Task DeleteRestaurantAsync(string restaurantId)
        {
            var session = _driver.AsyncSession();
            try
            {
                await session.ExecuteWriteAsync(async tx => {
                    await tx.RunAsync("MATCH (r:Restaurant {id: $restaurantId}) DETACH DELETE r", new { restaurantId });
                });
            }
            finally { await session.CloseAsync(); }
        }
    }
}