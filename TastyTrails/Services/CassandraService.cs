using Cassandra;
using TastyTrails.Models;

namespace TastyTrails.Services
{
    public class CassandraService
    {
        private readonly Cassandra.ISession _session;

        public CassandraService()
        {
            var cluster = Cluster.Builder()
                                .AddContactPoint("127.0.0.1") //cassandra host
                                .Build();

            _session = cluster.Connect("tastytrails"); //keyspace
        }

        public async Task InsertRestaurantAsync(Restaurant r)
        {
            var statement = await _session.PrepareAsync("INSERT INTO restaurants (id, name, latitude, longitude, cuisine) VALUES (?,?,?,?,?)");
            await _session.ExecuteAsync(statement.Bind(
                r.Id,
                r.Name,
                r.Latitude,
                r.Longitude,
                r.Cuisine
            ));
        }
    }
}