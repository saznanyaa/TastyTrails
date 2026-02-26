using Cassandra.Mapping.Attributes;
namespace TastyTrails.Models
{
    [Table("trending_by_city_weekly")]
    public class CassandraTrendingCityWeekly
    {
        [PartitionKey(0)]
        [Column("city")]
        public string? City { get; set; }
        [PartitionKey(1)]
        [Column("week_start")]
        public DateTime WeekStart { get; set; }
        [ClusteringKey]
        [Column("restaurant_id")]
        public Guid RestaurantId { get; set; }
        [Column("score")]
        public long Score { get; set; }
    }
}