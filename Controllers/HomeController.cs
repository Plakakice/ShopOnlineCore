using Microsoft.AspNetCore.Mvc;
using ShopOnlineCore.Models;
using System.Text.Json;
using Microsoft.EntityFrameworkCore;

namespace ShopOnlineCore.Controllers
{
    public class HomeController : Controller
    {
        private readonly ApplicationDbContext _context;

        public HomeController(ApplicationDbContext context)
        {
            _context = context;
        }

        public async Task<IActionResult> Index()
        {
            // OPTIMIZED: Fetch only IDs, shuffle in memory
            var allIds = await _context.Products.Select(p => p.Id).ToListAsync();
            
            // Shuffle IDs
            var random = new Random();
            var shuffledIds = allIds.OrderBy(x => random.Next()).ToList();
            
            // Save to Session
            var json = JsonSerializer.Serialize(shuffledIds);
            HttpContext.Session.SetString("RandomOrder_All", json);
            
            // Save random order for specific categories
            var categories = new[] { "chuột", "laptop", "bàn phím", "âm thanh", "bags" };
            
            // Pre-fetch category info to avoid N+1 queries if possible, or just filter IDs
            // Since we only have IDs, we might need to fetch Category info map if we want to filter by category purely in memory
            // OR we can just fetch IDs for each category separately if the dataset is huge.
            // For simplicity and performance balance: Fetch ID + CategoryName
            var productCategories = await _context.Products
                .Select(p => new { p.Id, p.Category })
                .ToListAsync();

            foreach (var category in categories)
            {
                string sessionKey = $"RandomOrder_{category}";
                
                var categoryProductIds = productCategories
                    .Where(p => !string.IsNullOrEmpty(p.Category) && 
                               p.Category.ToLower().Contains(category.ToLower()))
                    .Select(p => p.Id)
                    .OrderBy(x => random.Next()) // Shuffle category specific list too
                    .ToList();

                var categoryJson = JsonSerializer.Serialize(categoryProductIds);
                HttpContext.Session.SetString(sessionKey, categoryJson);
            }

            // Fetch full details for the first batch (e.g., 8 items) to display immediately if needed
            // But the view expects 'allProducts' (List<Product>). 
            // The original code passed 'allProducts' (List<Product>) to the view.
            // If the View iterates over ALL products, that's a bad design for large DBs.
            // Let's check the View. If it just renders a few, we should only pass a few.
            // However, to be safe and minimize breaking changes, I will pass the full list BUT sorted by the shuffled IDs.
            // WAIT: If I fetch ALL products here, I defeat the purpose of "Load More".
            // The original code: var allProducts = _context.Products.OrderBy(x => Guid.NewGuid()).ToList();
            // This loads EVERYTHING.
            // I should probably just load the first batch or keep the behavior but optimize the SORT.
            // To keep behavior identical but faster:
            // 1. Get Shuffled IDs.
            // 2. Fetch All Products (still heavy, but at least DB sort is avoided).
            // 3. Sort in memory.
            
            // BETTER: The View likely uses 'Model' to render the "New Arrivals" or similar.
            // Let's assume the view renders the whole list or a part of it.
            // I will fetch all products but using the shuffled IDs to order them.
            
            var allProducts = await _context.Products
                .Where(p => shuffledIds.Contains(p.Id))
                .ToListAsync();
                
            allProducts = allProducts.OrderBy(p => shuffledIds.IndexOf(p.Id)).ToList();

            return View(allProducts);
        }

        public IActionResult Contact() => View();

        public IActionResult Privacy() => View();

        public IActionResult DebugCategories() => View();

        // Initialize loaded IDs for a category (called on page load)
        [HttpPost]
        public IActionResult InitializeLoadedIds(string category, List<int> productIds)
        {
            if (string.IsNullOrEmpty(category) || productIds == null || productIds.Count == 0)
                return Json(new { success = false });

            string loadedIdsKey = $"LoadedIds_{category}";
            var json = JsonSerializer.Serialize(productIds);
            HttpContext.Session.SetString(loadedIdsKey, json);
            
            return Json(new { success = true });
        }

        // Load thêm sản phẩm cho Infinite Scroll
        public IActionResult LoadMoreProducts(string category, int page = 1, int pageSize = 8)
        {
            System.Diagnostics.Debug.WriteLine($"LoadMoreProducts called: category={category}, page={page}");
            
            if (string.IsNullOrEmpty(category))
                return Content("");

            // Session key để lưu thứ tự random của category này
            string sessionKey = $"RandomOrder_{category}";
            string loadedIdsKey = $"LoadedIds_{category}";
            
            List<int> randomProductIds;
            HashSet<int> loadedIds;
            bool isNewRandomOrder = false;

            // Kiểm tra xem đã có random order trong Session chưa
            if (HttpContext.Session.TryGetValue(sessionKey, out byte[] data) && data != null)
            {
                randomProductIds = JsonSerializer.Deserialize<List<int>>(System.Text.Encoding.UTF8.GetString(data)) ?? new List<int>();
            }
            else
            {
                // Fallback - random lại (nếu session expire)
                // OPTIMIZED: Fetch IDs only, shuffle in memory
                var ids = _context.Products
                    .Where(p => !string.IsNullOrEmpty(p.Category) && 
                               p.Category.ToLower().Contains(category.ToLower()))
                    .Select(p => p.Id)
                    .ToList();
                    
                var random = new Random();
                randomProductIds = ids.OrderBy(x => random.Next()).ToList();

                var json = JsonSerializer.Serialize(randomProductIds);
                HttpContext.Session.SetString(sessionKey, json);
                isNewRandomOrder = true;
                
                System.Diagnostics.Debug.WriteLine($"Generated new random order for {category}, {randomProductIds.Count} products");
            }

            // Lấy danh sách IDs đã load rồi
            if (HttpContext.Session.TryGetValue(loadedIdsKey, out byte[] loadedData) && loadedData != null && !isNewRandomOrder)
            {
                var loadedList = JsonSerializer.Deserialize<List<int>>(System.Text.Encoding.UTF8.GetString(loadedData));
                loadedIds = loadedList != null ? new HashSet<int>(loadedList) : new HashSet<int>();
            }
            else
            {
                // If new random order was generated, start fresh with empty loaded list
                loadedIds = new HashSet<int>();
            }

            System.Diagnostics.Debug.WriteLine($"Total products in {category}: {randomProductIds.Count}, Already loaded: {loadedIds.Count}");

            // Lấy sản phẩm chưa load từ danh sách random
            if (randomProductIds == null || randomProductIds.Count == 0)
                return Content("");

            // Get unloaded products (no need to skip based on page, just take the next batch)
            var unloadedProductIds = randomProductIds
                .Where(id => !loadedIds.Contains(id))
                .ToList();
            
            System.Diagnostics.Debug.WriteLine($"Unloaded products available: {unloadedProductIds.Count}");

            // Take the next pageSize items from unloaded products
            var newProductIds = unloadedProductIds
                .Take(pageSize)
                .ToList();

            System.Diagnostics.Debug.WriteLine($"Returning {newProductIds.Count} new products");
            
            if (!newProductIds.Any())
                return Content("");

            // Thêm các ID mới vào loaded list
            foreach (var id in newProductIds)
            {
                loadedIds.Add(id);
            }

            // Lưu danh sách loaded IDs vào Session
            var loadedJson = JsonSerializer.Serialize(loadedIds.ToList());
            HttpContext.Session.SetString(loadedIdsKey, loadedJson);

            var products = _context.Products
                .Where(p => newProductIds.Contains(p.Id))
                .ToList();

            // Giữ lại thứ tự random
            products = products.OrderBy(p => newProductIds.IndexOf(p.Id)).ToList();

            return PartialView("_ProductList", products);
        }
    }
}
