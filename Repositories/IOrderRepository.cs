using ShopOnlineCore.Models;

namespace ShopOnlineCore.Repositories
{
    public interface IOrderRepository
    {
        Task<(List<Order> Orders, int TotalCount)> GetOrdersAsync(
            string? status, 
            string? search,
            DateTime? from,
            DateTime? to,
            int pageIndex, 
            int pageSize);
            
        Task<Order?> GetOrderByIdAsync(int id);
        Task<List<Order>> GetOrdersByUserIdAsync(string userId);
        Task AddAsync(Order order);
        Task UpdateStatusAsync(int orderId, string status);
    }
}
