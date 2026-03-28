using Cassandra.Mapping.Attributes;
namespace TastyTrails.Models
{
    [Table("trending_results_by_city_cuisine_weekly")]
    public class CassandraTrendingCityCuisineResults
    {
        [PartitionKey(0)]
        [Column("city")]
        public string? City { get; set; }
        [PartitionKey(1)]
        [Column("cuisine")]
        public string? Cuisine { get; set; }
        [PartitionKey(2)]
        [Column("week_start")]
        public DateTime WeekStart { get; set; }
        [ClusteringKey]
        [Column("rank")]
        public int Rank { get; set; }
        [Column("restaurant_id")]
        public int RestaurantId { get; set; }
        [Column("name")]
        public string? Name { get; set; }
        [Column("latitude")]
        public double Latitude { get; set; }
        [Column("longitude")]
        public double Longitude { get; set; }
        [Column("score")]
        public int Score { get; set; }

    }
}