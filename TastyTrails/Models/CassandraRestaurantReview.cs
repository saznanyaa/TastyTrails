using Cassandra.Mapping.Attributes;
namespace TastyTrails.Models
{
    [Table("restaurant_review_events")]
    public class CassandraRestaurantReview
    {
        [PartitionKey]
        [Column("restaurant_id")]
        public Guid RestaurantId { get; set; }
        [ClusteringKey(0)]
        [Column("reviewed_at")]
        public DateTime ReviewedAt { get; set; }
        [ClusteringKey(1)]
        [Column("user_id")]
        public Guid UserId { get; set; }
    }
}