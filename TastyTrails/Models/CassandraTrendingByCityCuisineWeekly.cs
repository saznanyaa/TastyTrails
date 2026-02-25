using Cassandra.Mapping.Attributes;
namespace TastyTrails.Models
{
    [Table("trending_by_city_cuisine_weekly")]
    public class CassandraTrendingByCityCuisineWeekly
    {
        [PartitionKey(0)]
        public string? City { get; set; }

        [PartitionKey(1)]
        public string? Cuisine { get; set; }

        [PartitionKey(2)]
        public DateTime WeekStart { get; set; }

        [ClusteringKey]
        public Guid RestaurantId { get; set; }

        public long Score { get; set; }
    }
}