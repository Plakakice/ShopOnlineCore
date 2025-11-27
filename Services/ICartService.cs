using ShopOnlineCore.Models;

namespace ShopOnlineCore.Services;

public interface ICartService
{
    List<CartItem> GetCart();
    ServiceResult AddToCart(int productId, int quantity);
    ServiceResult DecreaseQuantity(int productId);
    ServiceResult IncreaseQuantity(int productId);
    ServiceResult UpdateQuantity(int productId, int quantity);
    void RemoveFromCart(int productId);
    void ClearCart();
}
