using Microsoft.EntityFrameworkCore;
using ShopOnlineCore.Models;

namespace ShopOnlineCore.Services
{
    public class ProductService : IProductService
    {
        private readonly ApplicationDbContext _context;

        public ProductService(ApplicationDbContext context)
        {
            _context = context;
        }

        public async Task<List<Product>> GetRandomProductsAsync(int count, int? excludeId = null, string? category = null)
        {
            var query = _context.Products.AsNoTracking();

            if (excludeId.HasValue)
            {
                query = query.Where(p => p.Id != excludeId.Value);
            }

            if (!string.IsNullOrEmpty(category))
            {
                // Assuming Category is stored in the Category field (string) or CategoryObj.Name
                // Based on the user's snippet, it seems they use the string field or relation.
                // I'll check both or just the string field as per the model I saw earlier.
                // The model has `public string? Category { get; set; }` and `CategoryObj`.
                // I will use the string field for now as it's likely what's being used.
                query = query.Where(p => p.Category == category);
            }

            // Optimized Random Selection:
            // 1. Get all available IDs
            var ids = await query.Select(p => p.Id).ToListAsync();

            if (ids.Count == 0)
            {
                return new List<Product>();
            }

            // 2. Shuffle IDs in memory and take 'count'
            var random = new Random();
            var selectedIds = ids.OrderBy(x => random.Next()).Take(count).ToList();

            // 3. Fetch products by selected IDs
            var products = await _context.Products
                .Where(p => selectedIds.Contains(p.Id))
                .ToListAsync();

            // 4. Restore random order (since SQL 'IN' doesn't guarantee order)
            return products.OrderBy(p => selectedIds.IndexOf(p.Id)).ToList();
        }
    }
}
