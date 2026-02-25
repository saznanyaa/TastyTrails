using Cassandra;
using Cassandra.Mapping;
using TastyTrails.Models;

namespace TastyTrails.Services
{
    public class CassandraService
    {
        private readonly Cassandra.ISession _session;
        private readonly IMapper _mapper;

        public CassandraService()
        {
            var cluster = Cluster.Builder()
                                .AddContactPoint("127.0.0.1") //cassandra host
                                .Build();

            _session = cluster.Connect("tastytrails"); //keyspace
            _mapper = new Mapper(_session);
        }

        public async Task InsertRestaurantAsync(Restaurant r)
        {
            var statement = await _session.PrepareAsync("INSERT INTO restaurants_by_city (city, id, name, latitude, longitude, cuisine, popularity_score) VALUES (?,?,?,?,?,?,?)");
            await _session.ExecuteAsync(statement.Bind(
                r.City,
                r.Id,
                r.Name,
                r.Latitude,
                r.Longitude,
                r.Cuisine,
                r.PopularityScore
            ));
        }

        public async Task InsertRestaurantCuisineAsync(Restaurant r)
        {
            var statement = await _session.PrepareAsync("INSERT INTO restaurants_by_city_and_cuisine (city, id, name, latitude, longitude, cuisine, popularity_score) VALUES (?,?,?,?,?,?,?)");
            await _session.ExecuteAsync(statement.Bind(
                r.City,
                r.Id,
                r.Name,
                r.Latitude,
                r.Longitude,
                r.Cuisine,
                r.PopularityScore
            ));
        }

        public async Task InsertUserAsync(CassandraUser u)
        {
            var statement = await _session.PrepareAsync("INSERT INTO users (user_id, username, email, role) VALUES (?,?,?,?)");
            await _session.ExecuteAsync(statement.Bind(
                u.Id,
                u.Username,
                u.Email,
                u.Role
            ));
        }

        //----------------------------------------------------------------------------

        public async Task InsertRestaurantView(CassandraRestaurantView view)
        {
            await _mapper.InsertAsync(view);
        }

        public async Task<List<CassandraRestaurantView>> GetRestaurantViewsAsync(Guid id)
        {
            var query = "WHERE restaurant_id=?";
            var views = await _mapper.FetchAsync<CassandraRestaurantView>(query, id);
            return views.ToList();
        }

        public async Task<List<CassandraRestaurantView>> GetRestaurantViewsFromToAsync(Guid id, DateTime from, DateTime to)
        {
            var query = @"
            SELECT * FROM restaurant_views
            WHERE restaurant_id = ?
            AND viewed_at >= ? AND viewed_at <= ?
            ";

            from = from.ToUniversalTime();
            to = to.ToUniversalTime();
            to = to.AddMilliseconds(1);
            var statement = new SimpleStatement(query, id, from, to);
            var rows = await _session.ExecuteAsync(statement);

            return rows.Select(r => new CassandraRestaurantView
            {
                RestaurantId = r.GetValue<Guid>("restaurant_id"),
                UserId = r.GetValue<Guid>("user_id"),
                ViewedAt = r.GetValue<DateTime>("viewed_at")

            }).ToList();
        }

        public async Task<long> GetRestaurantViewCountAsync(Guid id)
        {
            var query = "SELECT COUNT(*) FROM restaurant_views WHERE restaurant_id=?";
            var row = await _session.ExecuteAsync(new SimpleStatement(query, id));
            return row.FirstOrDefault()?.GetValue<long>("count")??0;
        }

        //-----------------------------------------------------------------------------
        public async Task InsertUserSavedRestaurants(CassandraSavedRestaurants r)
        {
            await _mapper.InsertAsync(r);
        }

        public async Task<List<CassandraSavedRestaurants>> GetUserSavedRestaurants(Guid id)
        {
            var query = "WHERE user_id=?";
            var saved = await _mapper.FetchAsync<CassandraSavedRestaurants>(query, id);
            return saved.ToList();
        }

        public async Task DeleteUserSavedRestaurant(Guid userId, Guid restaurantId)
        {
            var query = "DELETE FROM user_saved_restaurants WHERE user_id=? AND restaurant_id=?";
            
            await _session.ExecuteAsync(new SimpleStatement(query, userId, restaurantId));
        }

        //----------------------------------------------------------------------------
        public async Task PostRestaurantRating(CassandraRestaurantRatings r)
        {
            await _mapper.InsertAsync(r);
        }

        public async Task EditRestaurantRating(Guid restaurantId, Guid userId, int value)
        {
            var query = @"INSERT INTO restaurant_ratings(restaurant_id, user_id, rating_value) VALUES (?,?,?)";
            await _session.ExecuteAsync(new SimpleStatement(query, restaurantId, userId, value));
        }

        public async Task<List<CassandraRestaurantRatings>> GetRestaurantRatings(Guid id)
        {
            var query = "WHERE restaurant_id=?";
            var ratings = await _mapper.FetchAsync<CassandraRestaurantRatings>(query, id);
            return ratings.ToList();
        }

        public async Task<List<CassandraRestaurantRatings>> GetRestaurantRatingsByUser(Guid rId, Guid uId)
        {
            var query = "WHERE restaurant_id = ? and user_id = ?";
            var ratings = await _mapper.FetchAsync<CassandraRestaurantRatings>(query, rId, uId);
            return ratings.ToList();
        }

        public async Task<long> GetRestaurantRatingsCount(Guid id)
        {
            var query = "SELECT COUNT(*) FROM restaurant_ratings WHERE restaurant_id=?";
            var row = await _session.ExecuteAsync(new SimpleStatement(query, id));
            return row.FirstOrDefault()?.GetValue<long>("count")??0;
        }
    }
}