using Microsoft.AspNetCore.Mvc;
using System.Text.Json;
using ShopOnlineCore.Models;
using System.Security.Claims;
using Microsoft.EntityFrameworkCore;

namespace ShopOnlineCore.ViewComponents;

public class CartSummaryViewComponent : ViewComponent
{
    private const string CARTKEY = "cart";
    private readonly ApplicationDbContext _context;

    public CartSummaryViewComponent(ApplicationDbContext context)
    {
        _context = context;
    }

    public IViewComponentResult Invoke()
    {
        var session = HttpContext.Session;
        int count = 0;

        // Kiểm tra xem user có đăng nhập không
        var claimsPrincipal = User as ClaimsPrincipal;
        var userId = claimsPrincipal?.FindFirstValue(ClaimTypes.NameIdentifier);
        
        if (!string.IsNullOrEmpty(userId))
        {
            // User đã đăng nhập - lấy từ Database
            var cartLines = _context.CartLines.Where(x => x.UserId == userId).ToList();
            count = cartLines.Count;
        }
        else
        {
            // User ẩn danh - lấy từ Session
            var json = session.GetString(CARTKEY);
            if (json != null)
            {
                var cart = JsonSerializer.Deserialize<List<CartItem>>(json)!;
                count = cart.Count; // Đếm số loại sản phẩm khác nhau
            }
        }

        return View(count);
    }
}