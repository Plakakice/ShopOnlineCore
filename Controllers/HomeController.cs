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
            // Lấy tất cả sản phẩm để hiển thị theo các tab category
            var products = _context.Products.ToList();
            return View(products);
        }

        public IActionResult Contact() => View();

        public IActionResult Privacy() => View();
    }
}
