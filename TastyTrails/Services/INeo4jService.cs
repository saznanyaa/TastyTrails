using TastyTrails.Models;

namespace TastyTrails.Services
{
    public interface INeo4jService
    {
        // Create & Update
        Task CreateUserNodeAsync(NeoUserNode user);
        Task CreateRestaurantNodeAsync(NeoRestaurantNode restaurant);
        Task ConnectUserToRestaurantAsync(string userId, string restaurantId, string relationType);

        // Read
        Task<NeoUserNode?> GetUserByIdAsync(string id);
        Task<List<NeoRestaurantNode>> GetUserLikesAsync(string userId);

        // Delete
        Task DeleteUserAsync(string userId);
        Task DeleteRestaurantAsync(string restaurantId);
    }
}