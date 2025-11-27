using ShopOnlineCore.Models;
using ShopOnlineCore.Models.Identity;

namespace ShopOnlineCore.Services;

public interface IOrderService
{
    Task<ServiceResult> PlaceOrderAsync(ApplicationUser user, Order orderDetails, List<CartItem> cartItems);
    Task<ServiceResult> QuickCheckoutAsync(ApplicationUser user, List<CartItem> cartItems);
}

public class ServiceResult
{
    public bool Success { get; set; }
    public string Message { get; set; }
    public object? Data { get; set; }

    public static ServiceResult Ok(string message = "Success", object? data = null)
    {
        return new ServiceResult { Success = true, Message = message, Data = data };
    }

    public static ServiceResult Fail(string message)
    {
        return new ServiceResult { Success = false, Message = message };
    }
}
