using Cassandra;
using Microsoft.AspNetCore.Mvc.ViewEngines;
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
                    await tx.RunAsync("MERGE (r:Restaurant {id: $id}) SET r.name = $name, r.location = $location, r.cuisine = $cuisine",
                        new { id = restaurant.Id, name = restaurant.Name, location = restaurant.Location, cuisine = restaurant.Cuisine });
                });
            }
            finally { await session.CloseAsync(); }
        }

        public async Task ConnectUserToRestaurantAsync(string userId, string restaurantId, string relationType)
        {
            if (relationType != "LIKES" && relationType != "DISLIKES")
                throw new ArgumentException("Invalid relation type");

            var session = _driver.AsyncSession();

            try
            {
                await session.ExecuteWriteAsync(async tx =>
                {
                    var query = @"
                    MATCH (u:User {id: $userId}), (r:Restaurant {id: $restaurantId})

                    OPTIONAL MATCH (u)-[l:LIKES]->(r)
                    DELETE l
                    WITH u, r // Prenosimo u i r nakon brisanja prvog odnosa

                    OPTIONAL MATCH (u)-[d:DISLIKES]->(r)
                    DELETE d
                    WITH u, r // Prenosimo ih ponovo nakon brisanja drugog odnosa
                    ";

                    query += relationType == "LIKES"
                        ? "MERGE (u)-[:LIKES]->(r)"
                        : "MERGE (u)-[:DISLIKES]->(r)";

                    await tx.RunAsync(query, new { userId, restaurantId });
                });
            }
            finally { await session.CloseAsync(); }
        }

        public async Task ReviewRestaurant(string userId, string restaurantId, int rating)
        {
            var session = _driver.AsyncSession();
            try
            {
                await session.ExecuteWriteAsync(async tx =>
                {
                    await tx.RunAsync(@"
                        MATCH (u:User {id: $userId}), (res:Restaurant {id: $restaurantId})

                        MERGE (u)-[r:RATED]->(res)
                        SET r.score = $rating
                    ", new
                    {
                        userId,
                        restaurantId,
                        rating
                    });
                });

                if (rating >= 3)
                {
                    await ConnectUserToRestaurantAsync(userId, restaurantId, "LIKES");
                }
                else
                {
                    await ConnectUserToRestaurantAsync(userId, restaurantId, "DISLIKES");
                }
            }
            finally
            {
                await session.CloseAsync();
            }
        }
        public async Task FollowUserAsync(string followerId, string followedId)
        {
            var session = _driver.AsyncSession();
            try
            {
                await session.ExecuteWriteAsync(async tx => {
                    await tx.RunAsync(@"
                MATCH (a:User {id: $fId}), (b:User {id: $bId})
                MERGE (a)-[:FOLLOWS]->(b)",
                        new { fId = followerId, bId = followedId });
                Console.WriteLine($"Unfollow attempt: {followerId} -> {followedId}");
                });
            }
            finally { await session.CloseAsync(); }
        }

        public async Task UnfollowUserAsync(string followerId, string followedId)
        {
            var session = _driver.AsyncSession(o => o.WithDatabase("neo4j"));
            try
            {
                await session.ExecuteWriteAsync(async tx => {
                    await tx.RunAsync("MATCH (a:User {id: $fId})-[r:FOLLOWS]->(b:User {id: $bId}) DELETE r",
                        new { fId = followerId, bId = followedId });
                });
            }
            finally { await session.CloseAsync(); }
        }

        public async Task<List<NeoRestaurantNode>> GetRecommendations(string userId)
        {
            var session = _driver.AsyncSession();

            try
            {
                return await session.ExecuteReadAsync(async tx =>
                {
                    var cursor = await tx.RunAsync(@"
                        MATCH (u:User {id: $userId})

                        OPTIONAL MATCH (u)-[:FOLLOWS]->(f:User)-[rfRel:RATED]->(rf:Restaurant)
                        WHERE rfRel.score >= 3
                        AND NOT (u)-[:RATED|LIKES|DISLIKES]->(rf)

                        OPTIONAL MATCH (u)-[r1Rel:RATED]->(r1:Restaurant)
                        WHERE r1Rel.score >= 3

                        OPTIONAL MATCH (r1)<-[r2Rel:RATED]-(other:User)
                        WHERE other <> u AND r2Rel.score >= 3

                        OPTIONAL MATCH (other)-[rsRel:RATED]->(rs:Restaurant)
                        WHERE rsRel.score >= 3
                        AND NOT (u)-[:RATED|LIKES|DISLIKES]->(rs)

                        WITH 
                            rf, rs,
                            SUM(rfRel.score) AS followerScore,
                            SUM(rsRel.score) AS similarScore
                            WITH 
                            CASE 
                                WHEN rf IS NOT NULL THEN rf 
                                ELSE rs 
                            END AS restaurant,
                            followerScore,
                            similarScore

                        WHERE restaurant IS NOT NULL

                        RETURN 
                            restaurant.id AS id,
                            restaurant.name AS name,
                            restaurant.location AS location,
                            restaurant.cuisine AS cuisine,

                            (coalesce(followerScore,0) * 3 + coalesce(similarScore,0) * 2) AS score

                        ORDER BY score DESC
                        LIMIT 10
                    ", new { userId });

                    var results = new List<NeoRestaurantNode>();

                    await cursor.ForEachAsync(record =>
                    {
                        results.Add(new NeoRestaurantNode
                        {
                            Id = record["id"].As<string>(),
                            Name = record["name"].As<string>(),
                            Location = record["location"].As<string>(),
                            Cuisine = record["cuisine"].As<string>(),
                            Score = record["score"].As<int>()
                        });
                    });

                    return results;
                });
            }
            finally
            {
                await session.CloseAsync();
            }
        }

        public async Task<List<NeoRestaurantNode>> GetSimilarRestaurants(string restaurantId)
        {
            var session = _driver.AsyncSession();

            var query = @"
                MATCH (r:Restaurant {id: $id})

                MATCH (u:User)-[rel]->(r)
                WHERE type(rel) IN ['LIKES', 'RATED']

                MATCH (u)-[rel2]->(rec:Restaurant)
                WHERE rec.id <> $id 
                AND type(rel2) IN ['LIKES', 'RATED']
                AND NOT (u)-[:DISLIKES]->(rec)

                WITH rec,
                    sum(
                        CASE
                            WHEN type(rel2) = 'LIKES' THEN 3
                            WHEN type(rel2) = 'RATED' THEN rel2.score
                            ELSE 0
                        END
                    ) AS Score

                RETURN rec.id AS Id, rec.name AS Name, Score
                ORDER BY Score DESC
                LIMIT 5
            ";

            try
            {
                var result = await session.RunAsync(query, new { id = restaurantId });

                var list = new List<NeoRestaurantNode>();

                await result.ForEachAsync(record =>
                {
                    list.Add(new NeoRestaurantNode
                    {
                        Id = record["Id"].As<string>(),
                        Name = record["Name"].As<string>(),
                        Score = record["Score"].As<int>()
                    });
                });

                return list;
            }
             finally
            {
                await session.CloseAsync();
            }
        }

        public async Task DeleteReview(string userId, string restaurantId)
        {
            var session = _driver.AsyncSession();

            try
            {
                await session.ExecuteWriteAsync(async tx =>
                {
                    await tx.RunAsync(@"
                        MATCH (u:User {id: $userId})-[r]->(res:Restaurant {id: $restaurantId})
                        WHERE type(r) IN ['RATED', 'LIKES', 'DISLIKES']
                        DELETE r
                    ", new
                    {
                        userId,
                        restaurantId
                    });
                });
            }
            finally
            {
                await session.CloseAsync();
            }
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

        public async Task CreateCuisineNodeAsync(CuisineNode cuisine)
        {
            var session = _driver.AsyncSession();
            try
            {
                await session.ExecuteWriteAsync(async tx => {
                    await tx.RunAsync("MERGE (c:Cuisine {id: $id}) SET c.name = $name",
                        new { id = cuisine.Id, name = cuisine.Name });
                });
            }
            finally { await session.CloseAsync(); }
        }

        public async Task ConnectRestaurantToCuisineAsync(string restaurantId, string cuisineId)
        {
            var session = _driver.AsyncSession();
            try
            {
                await session.ExecuteWriteAsync(async tx => {
                    await tx.RunAsync(
                        "MATCH (r:Restaurant {id: $restaurantId}), (c:Cuisine {id: $cuisineId}) " +
                        "MERGE (r)-[:SERVES]->(c)",
                        new { restaurantId, cuisineId });
                });
            }
            finally { await session.CloseAsync(); }
        }

        public async Task LinkExternalReviewAsync(string userId, string restaurantId, ReviewRelationNode externalData)
        {
            var session = _driver.AsyncSession();
            try
            {
                await session.ExecuteWriteAsync(async tx =>
                {
                    await tx.RunAsync(
                        @"MATCH (u:User {id: $userId}), (res:Restaurant {id: $restaurantId})
                  MERGE (u)-[:WROTE]->(rev:Review {
                      mongoId: $mongoId, 
                      cassandraId: $cassId, 
                      rating: $rating
                  })-[:ABOUT]->(res)",
                        new
                        {
                            userId,
                            restaurantId,
                            mongoId = externalData.MongoReviewId,
                            cassId = externalData.CassandraRatingId,
                            rating = externalData.Rating
                        });
                });
            }
            finally
            {
                await session.CloseAsync();
            }
        }

        public async Task ExecuteWriteAsync(string query, object parameters = null)
        {
            using var session = _driver.AsyncSession();
            await session.ExecuteWriteAsync(async tx =>
            {
                await tx.RunAsync(query, parameters);
            });
        }

        public async Task UserLikesCuisineAsync(string userId, string cuisineId)
        {
            var session = _driver.AsyncSession();
            try
            {
                await session.ExecuteWriteAsync(async tx => {
                    await tx.RunAsync(@"
                MATCH (u:User {id: $uId}), (c:Cuisine {id: $cId})
                MERGE (u)-[:LIKES_CUISINE]->(c)",
                        new { uId = userId, cId = cuisineId });
                });
            }
            finally { await session.CloseAsync(); }
        }

        public async Task<List<NeoRestaurantNode>> GetSmartRecommendationsAsync(string userId)
        {
            var session = _driver.AsyncSession();
            try
            {
                return await session.ExecuteReadAsync(async tx => {
                    // Upit je restruktuiran da izbegne konflikt sa WHERE klauzulom
                    var query = @"
                MATCH (u:User {id: $userId})
                
                // Pronalazimo restorane preko kuhinja koje korisnik voli
                OPTIONAL MATCH (u)-[:LIKES_CUISINE]->(:Cuisine)<-[:SERVES]-(r1:Restaurant)
                
                // Pronalazimo restorane preko prijatelja (ocena >= 4)
                OPTIONAL MATCH (u)-[:FOLLOWS]->(:User)-[:WROTE]->(rev:Review)-[:ABOUT]->(r2:Restaurant)
                WHERE rev.rating >= 4

                // Skupljamo sve u listu i uklanjamo duplikate
                WITH u, collect(DISTINCT r1) + collect(DISTINCT r2) AS allCandidates
                
                // Razmotavamo listu
                UNWIND allCandidates AS r
                
                // Filtriramo: r ne sme biti null i korisnik ne sme imati recenziju za taj restoran
                WITH u, r 
                WHERE r IS NOT NULL 
                  AND NOT (u)-[:WROTE]->(:Review)-[:ABOUT]->(r)
                
                // Vracamo konacne rezultate
                RETURN DISTINCT r.id AS id, r.name AS name
                LIMIT 10";

                    var cursor = await tx.RunAsync(query, new { userId });
                    var results = await cursor.ToListAsync();

                    return results.Select(record => new NeoRestaurantNode
                    {
                        Id = record["id"].As<string>(),
                        Name = record["name"].As<string>()
                    }).ToList();
                });
            }
            finally { await session.CloseAsync(); }
        }
    }
}
