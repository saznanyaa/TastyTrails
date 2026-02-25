using System.ComponentModel.DataAnnotations;
using Cassandra.Mapping.Attributes;
namespace TastyTrails.Models
{
    [Table("restaurant_ratings")]
    public class CassandraRestaurantRatings
    {
        [PartitionKey]
        [Column("restaurant_id")]
        public Guid RestaurantId { get; set; }
        [ClusteringKey]
        [Column("user_id")]
        public Guid UserId { get; set; }
        [Range(1, 5)]
        [Column("rating_value")]
        public int RatingValue { get; set; }
    }
}