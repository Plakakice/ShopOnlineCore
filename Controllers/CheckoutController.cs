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

        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (string.IsNullOrEmpty(userId))
            return Challenge();

        // Kiểm tra xem user đã có đầy đủ thông tin không
        var lastOrder = await _context.Orders
            .Where(o => o.UserId == userId)
            .OrderByDescending(o => o.CreatedDate)
            .FirstOrDefaultAsync();

        // Nếu có đơn hàng trước đó và có đầy đủ thông tin, tự động điền vào
        var order = new Order();
        if (lastOrder != null && 
            !string.IsNullOrWhiteSpace(lastOrder.CustomerName) &&
            !string.IsNullOrWhiteSpace(lastOrder.Email) &&
            !string.IsNullOrWhiteSpace(lastOrder.Address) &&
            !string.IsNullOrWhiteSpace(lastOrder.PhoneNumber))
        {
            order.CustomerName = lastOrder.CustomerName;
            order.Email = lastOrder.Email;
            order.Address = lastOrder.Address;
            order.PhoneNumber = lastOrder.PhoneNumber;
        }

        ViewBag.Total = cart.Sum(x => x.Total);
        ViewBag.HasCompleteInfo = order.CustomerName != null;
        return View(order);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> QuickCheckout()
    {
        var cart = await GetCartAsync();
        if (!cart.Any())
            return RedirectToAction("Index", "Cart");

        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (string.IsNullOrEmpty(userId))
            return Challenge();

        // Lấy thông tin đơn hàng cuối cùng
        var lastOrder = await _context.Orders
            .Where(o => o.UserId == userId)
            .OrderByDescending(o => o.CreatedDate)
            .FirstOrDefaultAsync();

        if (lastOrder == null || 
            string.IsNullOrWhiteSpace(lastOrder.CustomerName) ||
            string.IsNullOrWhiteSpace(lastOrder.Email) ||
            string.IsNullOrWhiteSpace(lastOrder.Address) ||
            string.IsNullOrWhiteSpace(lastOrder.PhoneNumber))
        {
            // Không có đầy đủ thông tin, quay lại checkout form
            return RedirectToAction("Index");
        }

        // Kiểm tra tồn kho
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
            return RedirectToAction("Index", "Cart");
        }

        // Tạo đơn hàng mới từ thông tin cũ
        var newOrder = new Order
        {
            UserId = userId,
            CustomerName = lastOrder.CustomerName,
            Email = lastOrder.Email,
            Address = lastOrder.Address,
            PhoneNumber = lastOrder.PhoneNumber,
            CreatedDate = DateTime.Now,
            Status = "Pending",
            OrderItems = cart.Select(item => new OrderItem
            {
                ProductId = item.Id,
                ProductName = item.Name,
                Price = item.Price,
                Quantity = item.Quantity,
                ImageUrl = item.ImageUrl
            }).ToList()
        };

        // Lưu đơn hàng
        await _orderRepository.AddAsync(newOrder);

        // Trừ tồn kho
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
