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
            // Lấy tất cả sản phẩm random 1 lần để hiển thị theo các tab category
            var products = _context.Products.OrderBy(x => Guid.NewGuid()).ToList();
            return View(products);
        }

        public IActionResult Contact() => View();

        public IActionResult Privacy() => View();

        public IActionResult DebugCategories() => View();

        // Load thêm sản phẩm cho Infinite Scroll
        public IActionResult LoadMoreProducts(string category, int page = 1, int pageSize = 8)
        {
            if (string.IsNullOrEmpty(category))
                return Content("");

            // Session key để lưu thứ tự random của category này
            string sessionKey = $"RandomOrder_{category}";
            List<int> randomProductIds;

            // Kiểm tra xem đã có random order trong Session chưa
            if (HttpContext.Session.TryGetValue(sessionKey, out byte[] data))
            {
                // Lấy từ Session
                randomProductIds = JsonSerializer.Deserialize<List<int>>(System.Text.Encoding.UTF8.GetString(data));
            }
            else
            {
                // Lần đầu tiên - random và lưu vào Session
                randomProductIds = _context.Products
                    .Where(p => !string.IsNullOrEmpty(p.Category) && 
                               p.Category.ToLower().Contains(category.ToLower()))
                    .OrderBy(x => Guid.NewGuid())
                    .Select(p => p.Id)
                    .ToList();

                // Lưu vào Session
                var json = JsonSerializer.Serialize(randomProductIds);
                HttpContext.Session.SetString(sessionKey, json);
            }

            // Lấy sản phẩm từ danh sách random đã lưu
            var productIds = randomProductIds
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .ToList();

            if (!productIds.Any())
                return Content("");

            var products = _context.Products
                .Where(p => productIds.Contains(p.Id))
                .ToList();

            // Giữ lại thứ tự random
            products = products.OrderBy(p => productIds.IndexOf(p.Id)).ToList();

            // Tạo HTML cho sản phẩm
            var html = "";
            foreach (var product in products)
            {
                html += $@"
                    <div class=""col-md-3 product-men"" style=""margin-bottom: 30px; display: flex;"">>
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
