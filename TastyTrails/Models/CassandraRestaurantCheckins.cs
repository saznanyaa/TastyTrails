using Cassandra.Mapping.Attributes;
namespace TastyTrails.Models
{
    [Table("restaurant_checkins")]
    public class CassandraRestaurantCheckins
    {
        [PartitionKey]
        [Column("restaurant_id")]
        public Guid RestaurantId { get; set; }
        [ClusteringKey(0)]
        [Column("checked_in_at")]
        public DateTime CheckedInAt { get; set; }
        [ClusteringKey(1)]
        [Column("user_id")]
        public Guid UserId { get; set; }
    }
}