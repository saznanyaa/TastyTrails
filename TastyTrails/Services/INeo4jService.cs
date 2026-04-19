using TastyTrails.Models;

namespace TastyTrails.Services
{
    public interface INeo4jService
    {
        Task CreateUserNodeAsync(NeoUserNode user);
        Task CreateRestaurantNodeAsync(NeoRestaurantNode restaurant);
        Task ConnectUserToRestaurantAsync(string userId, string restaurantId, string relationType);
        Task UpdateReview(string userId, string restaurantId, int rating);
        Task<List<NeoRestaurantNode>> GetRecommendations(string userId);
        Task<NeoUserNode?> GetUserByIdAsync(string id);
        Task<List<NeoRestaurantNode>> GetUserLikesAsync(string userId);
        Task<List<NeoRestaurantNode>> GetSimilarRestaurants(string restaurantId);
        Task ExecuteWriteAsync(string query, object parameters = null);
        Task ReviewRestaurant(string userId, string restaurantId, int rating);
        Task DeleteReview(string userId, string restaurantId);
        Task FollowUserAsync(string followerId, string followedId);
        Task UnfollowUserAsync(string followerId, string followedId);
    }
}