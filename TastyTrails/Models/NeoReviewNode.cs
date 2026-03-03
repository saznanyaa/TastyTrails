namespace TastyTrails.Models
{
    public class ReviewRelationNode
    {
        public string MongoReviewId { get; set; } = null!; // Link ka MongoDB
        public string CassandraRatingId { get; set; } = null!; // Link ka Cassandri
        public int Rating { get; set; } // Čuvaš ocenu i kod sebe zbog brzih graf upita
    }
}