using ShopOnlineCore.Models;

namespace ShopOnlineCore.Services
{
    public interface ICartService
    {
        Task<List<CartItem>> GetCartItemsAsync();
        Task<ServiceResult> AddToCartAsync(int productId, int quantity);
        Task<ServiceResult> DecreaseQuantityAsync(int productId);
        Task<ServiceResult> IncreaseQuantityAsync(int productId);
        Task<ServiceResult> UpdateQuantityAsync(int productId, int quantity);
        Task RemoveFromCartAsync(int productId);
        Task ClearCartAsync();
    }
}
