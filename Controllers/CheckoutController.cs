using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using ShopOnlineCore.Models;
using System.Text.Json;
using System.Security.Claims;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Identity;
using ShopOnlineCore.Models.Identity;

namespace ShopOnlineCore.Controllers;

[Authorize]
public class CheckoutController : Controller
{
    private readonly OrderRepository _orderRepository;
    private readonly ApplicationDbContext _context;
    private readonly UserManager<ApplicationUser> _userManager;
    private const string CARTKEY = "cart";

    public CheckoutController(OrderRepository orderRepository, ApplicationDbContext context, UserManager<ApplicationUser> userManager)
    {
        _orderRepository = orderRepository;
        _context = context;
        _userManager = userManager;
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

        var user = await _userManager.GetUserAsync(User);
        if (user == null)
            return Challenge();

        // Tự động điền thông tin từ Profile người dùng
        var order = new Order
        {
            CustomerName = user.FullName ?? user.UserName, // Ưu tiên FullName, nếu không có thì dùng UserName
            Email = user.Email,
            Address = user.Address,
            PhoneNumber = user.PhoneNumber
        };

        // Nếu chưa có thông tin trong Profile, thử lấy từ đơn hàng cuối cùng (fallback)
        if (string.IsNullOrWhiteSpace(order.Address) || string.IsNullOrWhiteSpace(order.PhoneNumber))
        {
            var lastOrder = await _context.Orders
                .Where(o => o.UserId == user.Id)
                .OrderByDescending(o => o.CreatedDate)
                .FirstOrDefaultAsync();

            if (lastOrder != null)
            {
                if (string.IsNullOrWhiteSpace(order.Address)) order.Address = lastOrder.Address;
                if (string.IsNullOrWhiteSpace(order.PhoneNumber)) order.PhoneNumber = lastOrder.PhoneNumber;
                if (string.IsNullOrWhiteSpace(order.CustomerName)) order.CustomerName = lastOrder.CustomerName;
            }
        }

        ViewBag.Total = cart.Sum(x => x.Total);
        ViewBag.HasCompleteInfo = !string.IsNullOrWhiteSpace(order.Address) && !string.IsNullOrWhiteSpace(order.PhoneNumber);
        return View(order);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> QuickCheckout()
    {
        var cart = await GetCartAsync();
        if (!cart.Any())
            return RedirectToAction("Index", "Cart");

        var user = await _userManager.GetUserAsync(User);
        if (user == null)
            return Challenge();

        // Ưu tiên lấy thông tin từ Profile
        var customerName = user.FullName ?? user.UserName;
        var email = user.Email;
        var address = user.Address;
        var phoneNumber = user.PhoneNumber;

        // Nếu Profile thiếu, thử lấy từ đơn hàng cuối cùng
        if (string.IsNullOrWhiteSpace(address) || string.IsNullOrWhiteSpace(phoneNumber))
        {
            var lastOrder = await _context.Orders
                .Where(o => o.UserId == user.Id)
                .OrderByDescending(o => o.CreatedDate)
                .FirstOrDefaultAsync();

            if (lastOrder != null)
            {
                if (string.IsNullOrWhiteSpace(customerName)) customerName = lastOrder.CustomerName;
                if (string.IsNullOrWhiteSpace(address)) address = lastOrder.Address;
                if (string.IsNullOrWhiteSpace(phoneNumber)) phoneNumber = lastOrder.PhoneNumber;
            }
        }

        if (string.IsNullOrWhiteSpace(customerName) || 
            string.IsNullOrWhiteSpace(email) || 
            string.IsNullOrWhiteSpace(address) || 
            string.IsNullOrWhiteSpace(phoneNumber))
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
            UserId = user.Id,
            CustomerName = customerName,
            Email = email,
            Address = address,
            PhoneNumber = phoneNumber,
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

    // Xem chi tiết đơn hàng
    public async Task<IActionResult> Details(int id)
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (string.IsNullOrEmpty(userId))
        {
            return Challenge();
        }

        var order = await _context.Orders
            .Include(o => o.OrderItems)
            .FirstOrDefaultAsync(o => o.Id == id);

        if (order == null)
        {
            return NotFound();
        }

        // Chỉ cho phép xem đơn hàng của chính mình (trừ khi là Admin)
        if (order.UserId != userId && !User.IsInRole("Admin"))
        {
            return Forbid();
        }

        return View(order);
    }
}
