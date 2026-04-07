using TastyTrails.Models;

namespace TastyTrails.Services
{
    public interface INeo4jService
    {

       //user i restaurant
        Task CreateUserNodeAsync(NeoUserNode user);
        Task CreateRestaurantNodeAsync(NeoRestaurantNode restaurant);
        Task ConnectUserToRestaurantAsync(string userId, string restaurantId, string relationType);

        Task<List<NeoRestaurantNode>> GetRecommendations(string userId);
        Task<NeoUserNode?> GetUserByIdAsync(string id);
        Task<List<NeoRestaurantNode>> GetUserLikesAsync(string userId);

        Task DeleteUserAsync(string userId);
        Task DeleteRestaurantAsync(string restaurantId);
        
        //cuisine
        Task CreateCuisineNodeAsync(CuisineNode cuisine);
        Task ConnectRestaurantToCuisineAsync(string restaurantId, string cuisineId);

        //review
        Task LinkExternalReviewAsync(string userId, string restaurantId, ReviewRelationNode externalData);
        Task ExecuteWriteAsync(string query, object parameters = null);

        Task ReviewRestaurant(string userId, string restaurantId, int rating);
        Task FollowUserAsync(string followerId, string followedId);
        Task UserLikesCuisineAsync(string userId, string cuisineId);
        Task<List<NeoRestaurantNode>> GetSmartRecommendationsAsync(string userId);
        Task UnfollowUserAsync(string followerId, string followedId);
    }
}