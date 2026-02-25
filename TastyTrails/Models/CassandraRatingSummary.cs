using Cassandra.Mapping;
using Cassandra.Mapping.Attributes;
namespace TastyTrails.Models
{
    [Table("restaurant_rating_summary")]
    public class CassandraRatingSummary
    {
        [PartitionKey]
        [Column("restaurant_id")]
        public Guid RestaurantId { get; set; }
        [Column("rating_sum")]
        public long RatingSum { get; set; }
        [Column("rating_count")]
        public long RatingCount { get; set; }
    }
}