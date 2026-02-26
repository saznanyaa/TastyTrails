using Cassandra;
using Cassandra.Mapping;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Mvc.Rendering;
using TastyTrails.Models;

namespace TastyTrails.Services
{
    public class CassandraService
    {
        private readonly Cassandra.ISession _session;
        private readonly IMapper _mapper;

        public struct RestaurantMeta
        {
            public Guid Id { get; init; }
            public string Name { get; init; }
            public string Cuisine { get; init; }
            public double Latitude { get; init; }
            public double Longitude { get; init; }
        }

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
            var data = await GetCityAndCuisine(view.RestaurantId);
            await IncreaseTrending(data.City, data.Cuisine, view.RestaurantId);
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
            var data = await GetCityAndCuisine(r.RestaurantId);
            await IncreaseTrending(data.City, data.Cuisine, r.RestaurantId);
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
        public async Task PostRestaurantRating(Guid restaurantId, Guid userId, int rating_value)
        {
            var query = @"INSERT INTO restaurant_ratings (restaurant_id, user_id, rating_value) VALUES(?,?,?)";
            await _session.ExecuteAsync(new SimpleStatement(query, restaurantId, userId, rating_value));

            var updateQuery = @"UPDATE restaurant_rating_summary 
                SET rating_sum = rating_sum + ?, rating_count = rating_count + 1
                WHERE restaurant_id = ?";
            await _session.ExecuteAsync(new SimpleStatement(updateQuery, (long)rating_value, restaurantId));
            var data = await GetCityAndCuisine(restaurantId);
            await IncreaseTrending(data.City, data.Cuisine, restaurantId);
        }

        public async Task EditRestaurantRating(Guid restaurantId, Guid userId, int newValue)
        {
            var selectQuery = @"
                SELECT rating_value
                FROM restaurant_ratings
                WHERE restaurant_id = ? AND user_id = ?;";

            var result = await _session.ExecuteAsync(new SimpleStatement(selectQuery, restaurantId, userId));

            var row = result.FirstOrDefault();

            if (row == null)
                throw new Exception("Rating does not exist.");

            int oldValue = row.GetValue<int>("rating_value");

            long delta = (long)newValue - (long)oldValue;

            var updateQuery = @"
                UPDATE restaurant_ratings
                SET rating_value = ?
                WHERE restaurant_id = ? AND user_id = ?;";

            await _session.ExecuteAsync(new SimpleStatement(updateQuery, newValue, restaurantId, userId));

            var summaryQuery = @"
                UPDATE restaurant_rating_summary
                SET rating_sum = rating_sum + ?
                WHERE restaurant_id = ?;";

            await _session.ExecuteAsync(
                new SimpleStatement(summaryQuery, delta, restaurantId));
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

        public async Task<long> GetRestaurantRatingsCount(Guid restaurantId)
        {
            var query = @"
                SELECT rating_count
                FROM restaurant_rating_summary
                WHERE restaurant_id = ?;";

            var result = await _session.ExecuteAsync(new SimpleStatement(query, restaurantId));

            var row = result.FirstOrDefault();

            return row?.GetValue<long>("rating_count") ?? 0;
        }

        //---restaurant_review_events----------------------------------------------------------------------------
        public async Task PostRestaurantReview(CassandraRestaurantReview r)
        {
            await _mapper.InsertAsync(r);
            var data = await GetCityAndCuisine(r.RestaurantId);
            await IncreaseTrending(data.City, data.Cuisine, r.RestaurantId);
        }

        public async Task<List<CassandraRestaurantReview>> GetRestaurantReview(Guid id)
        {
            var query = "WHERE restaurant_id=?";
            var reviews = await _mapper.FetchAsync<CassandraRestaurantReview>(query, id);
            return reviews.ToList();
        }

        public async Task<List<CassandraRestaurantReview>> GetRestaurantReviewsFromTo(Guid id, DateTime from, DateTime to)
        {
            var query = @"
            SELECT * FROM restaurant_review_events
            WHERE restaurant_id = ?
            AND reviewed_at >= ? AND reviewed_at <= ?
            ";

            from = from.ToUniversalTime();
            to = to.ToUniversalTime();
            to = to.AddMilliseconds(1);
            var statement = new SimpleStatement(query, id, from, to);
            var rows = await _session.ExecuteAsync(statement);

            return rows.Select(r => new CassandraRestaurantReview
            {
                RestaurantId = r.GetValue<Guid>("restaurant_id"),
                UserId = r.GetValue<Guid>("user_id"),
                ReviewedAt = r.GetValue<DateTime>("reviewed_at")

            }).ToList();
        }

        public async Task<long> GetRestaurantReviewCount(Guid id)
        {
            var query = "SELECT COUNT(*) FROM restaurant_review_events WHERE restaurant_id=?";
            var row = await _session.ExecuteAsync(new SimpleStatement(query, id));
            return row.FirstOrDefault()?.GetValue<long>("count")??0;
        }

        //---restaurant_checkins----------------------------------------------------------
        public async Task PostRestaurantCheckin(CassandraRestaurantCheckins c)
        {
            await _mapper.InsertAsync(c);
            var data = await GetCityAndCuisine(c.RestaurantId);
            await IncreaseTrending(data.City, data.Cuisine, c.RestaurantId);
            
        }

        public async Task<List<CassandraRestaurantCheckins>> GetRestaurantCheckins(Guid id)
        {
            var query = "WHERE restaurant_id=?";
            var checkins = await _mapper.FetchAsync<CassandraRestaurantCheckins>(query, id);
            return checkins.ToList();
        }

        public async Task<List<CassandraRestaurantCheckins>> GetRestaurantCheckinsFromTo(Guid id, DateTime from, DateTime to)
        {
            var query = @"
            SELECT * FROM restaurant_checkins
            WHERE restaurant_id = ?
            AND checked_in_at >= ? AND checked_in_at <= ?
            ";

            from = from.ToUniversalTime();
            to = to.ToUniversalTime();
            to = to.AddMilliseconds(1);
            var statement = new SimpleStatement(query, id, from, to);
            var rows = await _session.ExecuteAsync(statement);

            return rows.Select(r => new CassandraRestaurantCheckins
            {
                RestaurantId = r.GetValue<Guid>("restaurant_id"),
                UserId = r.GetValue<Guid>("user_id"),
                CheckedInAt = r.GetValue<DateTime>("checked_in_at")

            }).ToList();
        }

        public async Task<long> GetRestaurantCheckinsCount(Guid id)
        {
            var query = "SELECT COUNT(*) FROM restaurant_checkins WHERE restaurant_id=?";
            var row = await _session.ExecuteAsync(new SimpleStatement(query, id));
            return row.FirstOrDefault()?.GetValue<long>("count")??0;
        }

        //---restaurant_rating_summary
        public async Task<CassandraRatingSummary?> GetRatingSummaryAsync(Guid restaurantId)
        {
            var query = @"
                SELECT rating_sum, rating_count
                FROM restaurant_rating_summary
                WHERE restaurant_id = ?;";

            var result = await _session.ExecuteAsync(
                new SimpleStatement(query, restaurantId));

            var row = result.FirstOrDefault();

            if (row == null)
                return null;

            return new CassandraRatingSummary
            {
                RestaurantId = restaurantId,
                RatingSum = row.GetValue<long>("rating_sum"),
                RatingCount = row.GetValue<long>("rating_count")
            };
        }

        public async Task<double?> GetAverageRating(Guid restaurantId)
        {
            var summary = await GetRatingSummaryAsync(restaurantId);

            if (summary == null || summary.RatingCount == 0)
                return null;

            return (double)summary.RatingSum / summary.RatingCount;
        }

        //---trending_weekly-------------------------------------------------------------------
        public async Task IncreaseTrending(string city, string cuisine, Guid restaurantId)
        {
            var now = DateTime.UtcNow;
            var diff = (7+(now.DayOfWeek - DayOfWeek.Monday))%7;
            var weekStart = now.Date.AddDays(-diff);

            var query1 = @"UPDATE trending_by_city_weekly
                SET score = score + 1
                WHERE city = ?
                AND week_start = ?
                AND restaurant_id = ?";

            var query2 = @"UPDATE trending_by_city_cuisine_weekly
                SET score = score + 1
                WHERE city = ?
                AND cuisine = ?
                AND week_start = ?
                AND restaurant_id = ?";
            
            await _session.ExecuteAsync(new SimpleStatement(query1, city, weekStart, restaurantId));
            await _session.ExecuteAsync(new SimpleStatement(query2, city, cuisine, weekStart, restaurantId));
        }

        public async Task InsertRestaurantLookup(RestaurantLookup lookup)
        {
            var query = @"INSERT INTO restaurant_lookup (id, city, cuisine, name, latitude, longitude) VALUES (?, ?, ?, ?, ?, ?);";

            await _session.ExecuteAsync(new SimpleStatement(query, lookup.Id, lookup.City, lookup.Cuisine, lookup.Name, lookup.Latitude, lookup.Longitude));
        }

        //---trending_results----------------------------------------------------------------------
        public async Task FillWeeklyTrendingResults(string city, string cuisine, DateTime weekStart)
        {
            var q1 = @"SELECT id FROM restaurants_by_city_and_cuisine WHERE city = ? and cuisine = ?";
            var rIdsRes = await _session.ExecuteAsync(new SimpleStatement(q1, city, cuisine));

            var rIds = rIdsRes.Select(r => r.GetValue<Guid>("id")).ToList();

            if(!rIds.Any()) return;

            var restaurantMetadata = new Dictionary<Guid, RestaurantMeta>();
            var scoreList = new List<(RestaurantMeta Meta, int Score)>();

            var weekStartUtc = weekStart.ToUniversalTime();

            foreach(var r in rIds)
            {
                var q3 = @"SELECT id, name, cuisine, latitude, longitude 
                    FROM restaurant_lookup WHERE id = ?";
                var row = (await _session.ExecuteAsync(new SimpleStatement(q3, r))).FirstOrDefault();
                if(row != null)
                {
                    var meta = new RestaurantMeta
                    {
                        Id = row.GetValue<Guid>("id"),
                        Name = row.GetValue<string>("name"),
                        Cuisine = row.GetValue<string>("cuisine"),
                        Latitude = row.GetValue<double>("latitude"),
                        Longitude = row.GetValue<double>("longitude")
                    };
                    restaurantMetadata[r] = meta;

                    var q2 = @"SELECT score FROM trending_by_city_weekly WHERE
                        city = ? AND week_start = ? AND restaurant_id = ?";

                    var row1 = await _session.ExecuteAsync(new SimpleStatement(q2, city, weekStartUtc, r));

                    var score = row1.FirstOrDefault();
                    if(score != null)
                    {
                        long scoreLong = score.GetValue<long>("score");
                        int scoreInt = checked((int)scoreLong);
                        scoreList.Add((meta, scoreInt));
                    }
                }
            }
            if(!scoreList.Any()) return;
            
            var sortedScores = scoreList.OrderByDescending(x => x.Score).ToList();

            int rank = 1;
            foreach(var s in sortedScores)
            {

                var q4 = @"INSERT INTO trending_results_by_city_weekly
                    (city, week_start, rank, restaurant_id, name, cuisine, latitude, longitude, score)
                    VALUES (?, ?, ?, ?, ?, ?, ?, ?, ?)";
                var q5 = @"INSERT INTO trending_results_by_city_cuisine_weekly
                    (city, week_start, rank, restaurant_id, name, cuisine, latitude, longitude, score)
                    VALUES (?, ?, ?, ?, ?, ?, ?, ?, ?)";
                await _session.ExecuteAsync(new SimpleStatement(q4, city, weekStartUtc, rank, s.Meta.Id, s.Meta.Name, s.Meta.Cuisine, s.Meta.Latitude, s.Meta.Longitude, s.Score));
                await _session.ExecuteAsync(new SimpleStatement(q5, city, weekStartUtc, rank, s.Meta.Id, s.Meta.Name, s.Meta.Cuisine, s.Meta.Latitude, s.Meta.Longitude, s.Score));
                rank++;
            }
        }

        public async Task DeleteTrendingWeeklyResults(string city, string cuisine, DateTime weekStart)
        {
            var q1 = @"DELETE FROM trending_results_by_city_weekly WHERE city = ? AND week_start = ?";
            var q2 = @"DELETE FROM trending_results_by_city_cuisine_weekly WHERE city = ? AND cuisine = ? AND week_start = ?";

            await _session.ExecuteAsync(new SimpleStatement(q1, city, weekStart));
            await _session.ExecuteAsync(new SimpleStatement(q2, city, cuisine, weekStart));
        }

        //---helpers-------------------------------------------------------------------------------

        public async Task<RestaurantLookup> GetCityAndCuisine(Guid id)
        {
            var query = @"
                SELECT city, cuisine, name, latitude, longitude
                FROM restaurant_lookup
                WHERE id = ?;";

            var result = await _session.ExecuteAsync(new SimpleStatement(query, id));

            var row = result.FirstOrDefault();

            if (row == null)
                throw new Exception($"Restaurant {id} not found in lookup table.");

            return new RestaurantLookup
            {
                Id = id,
                City = row.GetValue<string>("city"),
                Cuisine = row.GetValue<string>("cuisine"),
                Name = row.GetValue<string>("name"),
                Latitude = row.GetValue<double>("latitude"),
                Longitude = row.GetValue<double>("longitude")
            };
        }
    }
}