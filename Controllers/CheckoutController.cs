using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using ShopOnlineCore.Models;
using System.Text.Json;
using System.Security.Claims;
using Microsoft.EntityFrameworkCore;

namespace ShopOnlineCore.Controllers;

[Authorize]
public class CheckoutController : Controller
{
    private readonly OrderRepository _orderRepository;
    private readonly ApplicationDbContext _context;
    private const string CARTKEY = "cart";

    public CheckoutController(OrderRepository orderRepository, ApplicationDbContext context)
    {
        _orderRepository = orderRepository;
        _context = context;
    }

    private async Task<List<CartItem>> GetCartAsync()
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (!string.IsNullOrEmpty(userId))
        {
            var lines = await _context.CartLines.Where(x => x.UserId == userId).ToListAsync();
            return lines.Select(l => new CartItem
            {
                Id = l.ProductId,
                Name = l.ProductName,
                Price = l.Price,
                Quantity = l.Quantity,
                ImageUrl = l.ImageUrl
            }).ToList();
        }

        var json = HttpContext.Session.GetString(CARTKEY);
        if (json != null)
            return JsonSerializer.Deserialize<List<CartItem>>(json)!;
        return new List<CartItem>();
    }

    private async Task ClearCartAsync()
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (!string.IsNullOrEmpty(userId))
        {
            var lines = _context.CartLines.Where(x => x.UserId == userId);
            _context.CartLines.RemoveRange(lines);
            await _context.SaveChangesAsync();
        }
        HttpContext.Session.Remove(CARTKEY);
    }

    [HttpGet]
    public async Task<IActionResult> Index()
    {
        var cart = await GetCartAsync();
        if (!cart.Any())
            return RedirectToAction("Index", "Cart");

        ViewBag.Total = cart.Sum(x => x.Total);
        return View();
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Index(Order model)
    {
        var cart = await GetCartAsync();
        if (!cart.Any())
            return RedirectToAction("Index", "Cart");

        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (string.IsNullOrEmpty(userId))
        {
            return Challenge(); // yêu cầu đăng nhập
        }

        if (!ModelState.IsValid)
        {
            ViewBag.Total = cart.Sum(x => x.Total);
            return View(model);
        }

        // Kiểm tra tồn kho trước khi đặt hàng
        var outOfStockItems = new List<string>();
        foreach (var item in cart)
        {
            var product = await _context.Products.FindAsync(item.Id);
            if (product == null)
            {
                outOfStockItems.Add($"{item.Name} - Sản phẩm không tồn tại");
            }
            else if (product.Stock < item.Quantity)
            {
                outOfStockItems.Add($"{item.Name} - Chỉ còn {product.Stock} sản phẩm (bạn đặt {item.Quantity})");
            }
        }

        if (outOfStockItems.Any())
        {
            TempData["Error"] = "Một số sản phẩm không đủ hàng:<br>" + string.Join("<br>", outOfStockItems);
            ViewBag.Total = cart.Sum(x => x.Total);
            return View(model);
        }

        // Convert CartItem to OrderItem
        model.OrderItems = cart.Select(item => new OrderItem
        {
            ProductId = item.Id,
            ProductName = item.Name,
            Price = item.Price,
            Quantity = item.Quantity,
            ImageUrl = item.ImageUrl
        }).ToList();

        model.UserId = userId;

        // Lưu đơn hàng
        await _orderRepository.AddAsync(model);

        // Trừ tồn kho sau khi đặt hàng thành công
        foreach (var item in cart)
        {
            var product = await _context.Products.FindAsync(item.Id);
            if (product != null)
            {
                product.Stock -= item.Quantity;
            }
        }
        await _context.SaveChangesAsync();

        // Xóa giỏ hàng
        await ClearCartAsync();
        
        TempData["Success"] = "Đặt hàng thành công! Cảm ơn bạn đã mua sắm.";
        return RedirectToAction("Success");
    }

    public IActionResult Success()
    {
        return View();
    }

    // Xem danh sách đơn hàng đã tạo
    public async Task<IActionResult> Orders()
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        var orders = string.IsNullOrEmpty(userId)
            ? new List<Order>()
            : await _orderRepository.GetByUserAsync(userId);
        return View(orders);
    }
}
