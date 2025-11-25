using ShopOnlineCore.Models;

namespace ShopOnlineCore.Services;

public interface ICartService
{
    List<CartItem> GetCart();
    void AddToCart(int productId, int quantity);
    void DecreaseQuantity(int productId);
    void IncreaseQuantity(int productId);
    void UpdateQuantity(int productId, int quantity);
    void RemoveFromCart(int productId);
    void ClearCart();
}
