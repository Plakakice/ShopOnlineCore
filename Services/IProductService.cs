using ShopOnlineCore.Models;

namespace ShopOnlineCore.Services
{
    public interface IProductService
    {
        Task<List<Product>> GetRandomProductsAsync(int count, int? excludeId = null, string? category = null);
    }
}
