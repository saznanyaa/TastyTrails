using Cassandra.Mapping.Attributes;
namespace TastyTrails.Models
{
    [Table("user_saved_restaurants")]
    public class CassandraSavedRestaurants
    {
        [PartitionKey]
        [Column("user_id")]
        public Guid UserId { get; set; }
        [ClusteringKey]
        [Column("restaurant_id")]
        public Guid RestaurantId { get; set; }
        [Column("saved_at")]
        public DateTime SavedAt { get; set; }
    }
}