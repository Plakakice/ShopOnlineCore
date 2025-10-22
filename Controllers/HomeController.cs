using Microsoft.AspNetCore.Mvc;
using ShopOnlineCore.Models;

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
            // Lấy 5 sản phẩm đầu tiên trong database để hiển thị ở trang chủ
            var topProducts = _context.Products.Take(5).ToList();
            return View(topProducts);
        }

        public IActionResult Contact() => View();

        public IActionResult Privacy() => View();
    }
}
