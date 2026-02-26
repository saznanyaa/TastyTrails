using Cassandra.Mapping.Attributes;
namespace TastyTrails.Models
{
    [Table("trending_by_city_cuisine_weekly")]
    public class CassandraTrendingCityCuisineWeekly
    {
        [PartitionKey(0)]
        [Column("city")]
        public string? City { get; set; }
        [PartitionKey(1)]
        [Column("cusine")]
        public string? Cuisine { get; set; }
        [PartitionKey(2)]
        [Column("week_start")]
        public DateTime WeekStart { get; set; }
        [ClusteringKey]
        [Column("restaurant_id")]
        public Guid RestaurantId { get; set; }
        [Column("score")]
        public long Score { get; set; }
    }
}