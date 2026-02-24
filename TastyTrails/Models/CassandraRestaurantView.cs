using Cassandra.Mapping.Attributes;
namespace TastyTrails.Models
{
    [Table("restaurant_views")]
    public class CassandraRestaurantView
    {
        [PartitionKey]
        [Column("restaurant_id")]
        public Guid RestaurantId { get; set; }
        [ClusteringKey(0)]
        [Column("viewed_at")]
        public DateTime ViewedAt { get; set; }
        [ClusteringKey(1)]
        [Column("user_id")]
        public Guid UserId { get; set; }
    }
}