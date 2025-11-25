using Microsoft.AspNetCore.Mvc;
using ShopOnlineCore.Models;
using System.Text.Json;

namespace ShopOnlineCore.Controllers
{
    public class HomeController : Controller
    {
        private readonly ApplicationDbContext _context;

        public HomeController(ApplicationDbContext context)
        {
            _context = context;
        }

        public IActionResult Index()
        {
            // Lấy tất cả sản phẩm và random 1 lần duy nhất
            var allProducts = _context.Products.OrderBy(x => Guid.NewGuid()).ToList();
            
            // Lưu toàn bộ sản phẩm random vào Session để dùng chung cho tất cả tab
            var json = JsonSerializer.Serialize(allProducts.Select(p => p.Id).ToList());
            HttpContext.Session.SetString("RandomOrder_All", json);
            
            // Lưu thứ tự random cho từng category riêng (từ danh sách random chung)
            var categories = new[] { "chuột", "laptop", "bàn phím", "âm thanh", "bags" };
            
            foreach (var category in categories)
            {
                string sessionKey = $"RandomOrder_{category}";
                
                // Lấy IDs của category này từ danh sách random chung
                var categoryProductIds = allProducts
                    .Where(p => !string.IsNullOrEmpty(p.Category) && 
                               p.Category.ToLower().Contains(category.ToLower()))
                    .Select(p => p.Id)
                    .ToList();

                // Lưu vào Session
                var categoryJson = JsonSerializer.Serialize(categoryProductIds);
                HttpContext.Session.SetString(sessionKey, categoryJson);
            }

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

            // Kiểm tra xem đã có random order trong Session chưa
            if (HttpContext.Session.TryGetValue(sessionKey, out byte[] data) && data != null)
            {
                randomProductIds = JsonSerializer.Deserialize<List<int>>(System.Text.Encoding.UTF8.GetString(data)) ?? new List<int>();
            }
            else
            {
                // Fallback - random lại (nếu session expire)
                randomProductIds = _context.Products
                    .Where(p => !string.IsNullOrEmpty(p.Category) && 
                               p.Category.ToLower().Contains(category.ToLower()))
                    .OrderBy(x => Guid.NewGuid())
                    .Select(p => p.Id)
                    .ToList();

                var json = JsonSerializer.Serialize(randomProductIds);
                HttpContext.Session.SetString(sessionKey, json);
            }

            // Lấy danh sách IDs đã load rồi
            if (HttpContext.Session.TryGetValue(loadedIdsKey, out byte[] loadedData) && loadedData != null)
            {
                var loadedList = JsonSerializer.Deserialize<List<int>>(System.Text.Encoding.UTF8.GetString(loadedData));
                loadedIds = loadedList != null ? new HashSet<int>(loadedList) : new HashSet<int>();
            }
            else
            {
                loadedIds = new HashSet<int>();
            }

            System.Diagnostics.Debug.WriteLine($"Total products in {category}: {randomProductIds.Count}, Already loaded: {loadedIds.Count}");

            // Lấy sản phẩm chưa load từ danh sách random
            if (randomProductIds == null || randomProductIds.Count == 0)
                return Content("");

            var newProductIds = randomProductIds
                .Where(id => !loadedIds.Contains(id))
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .ToList();

            System.Diagnostics.Debug.WriteLine($"Returning {newProductIds.Count} new products for page {page}");
            
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

            // Tạo HTML cho sản phẩm
            var html = "";
            foreach (var product in products)
            {
                html += $@"
                    <div class=""col-md-3 product-men"" data-product-id=""{product.Id}"" style=""margin-bottom: 30px; display: flex;"">
                        <div class=""men-pro-item simpleCart_shelfItem"" style=""border: 1px solid #e8e8e8; border-radius: 8px; overflow: hidden; display: flex; flex-direction: column; width: 100%;"">
                            <div class=""men-thumb-item"" style=""position: relative; overflow: hidden; background: #f8f8f8; flex-shrink: 0;"">
                                <img src=""{Url.Content(product.ImageUrl)}"" class=""pro-image-front img-responsive"" alt=""{product.Name}"" style=""width: 100%; height: 250px; object-fit: cover;"" />
                                <img src=""{Url.Content(product.ImageUrl)}"" class=""pro-image-back img-responsive"" alt=""{product.Name}"" style=""width: 100%; height: 250px; object-fit: cover;"" />
                                <div class=""men-cart-pro"">
                                    <div class=""inner-men-cart-pro"">
                                        <a href=""{Url.Action("Details", "Product", new { id = product.Id })}"" class=""link-product-add-cart"">Xem chi tiết</a>
                                    </div>
                                </div>
                                <span class=""product-new-top"">New</span>
                            </div>
                            <div class=""item-info-product text-center"" style=""padding: 20px 15px; flex: 1; display: flex; flex-direction: column; justify-content: space-between;"">
                                <div style=""flex: 1; display: flex; flex-direction: column; justify-content: center;"">
                                    <h4 style=""font-size: 1.05em; margin-bottom: 12px; height: 48px; display: -webkit-box; -webkit-line-clamp: 2; -webkit-box-orient: vertical; overflow: hidden; text-overflow: ellipsis; line-height: 1.4em;"">{product.Name}</h4>
                                    <div class=""info-product-price"" style=""margin-bottom: 15px;"">
                                        <span class=""item_price"" style=""font-size: 1.3em; font-weight: 700; color: #e74c3c;"">{product.Price.ToString("N0")} ₫</span>
                                    </div>
                                </div>
                                <a href=""{Url.Action("Details", "Product", new { id = product.Id })}"" class=""btn btn-primary hvr-outline-out"" style=""width: 100%; max-width: 150px; margin: 0 auto;"">Chi tiết</a>
                            </div>
                        </div>
                    </div>
                ";
            }
            return Content(html);
        }
    }
}
