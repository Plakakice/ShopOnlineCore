using Microsoft.AspNetCore.Mvc;
using ShopOnlineCore.Models;
using System.Text.Json;
using System.Security.Claims;

namespace ShopOnlineCore.Controllers;

public class CartController : Controller
{
    private readonly ApplicationDbContext _context;

public CartController(ApplicationDbContext context)
{
    _context = context;
}

    private const string CARTKEY = "cart";

    private bool IsAuthenticated => User?.Identity?.IsAuthenticated == true;
    private string? CurrentUserId => User.FindFirstValue(ClaimTypes.NameIdentifier);

    // Lấy giỏ hàng hiện tại (DB nếu đã đăng nhập, Session nếu ẩn danh)
    private List<CartItem> GetCart()
    {
        if (IsAuthenticated && CurrentUserId != null)
        {
            var lines = _context.CartLines.Where(x => x.UserId == CurrentUserId).ToList();
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

    // Lưu giỏ hàng (DB hoặc Session)
    private void SaveCart(List<CartItem> cart)
    {
        if (IsAuthenticated && CurrentUserId != null)
        {
            // Replace user's cart lines with provided list
            var existing = _context.CartLines.Where(x => x.UserId == CurrentUserId);
            _context.CartLines.RemoveRange(existing);
            _context.CartLines.AddRange(cart.Select(c => new CartLine
            {
                UserId = CurrentUserId,
                ProductId = c.Id,
                ProductName = c.Name,
                Price = c.Price,
                Quantity = c.Quantity,
                ImageUrl = c.ImageUrl
            }));
            _context.SaveChanges();
            return;
        }

        var json = JsonSerializer.Serialize(cart);
        HttpContext.Session.SetString(CARTKEY, json);
    }

    // Hiển thị giỏ hàng
    public IActionResult Index()
    {
        var cart = GetCart();
        return View(cart);
    }

    // Thêm sản phẩm
    public IActionResult Add(int id)
    {
        var product = _context.Products.FirstOrDefault(p => p.Id == id);
        if (product == null) return NotFound();

        if (IsAuthenticated && CurrentUserId != null)
        {
            var line = _context.CartLines.FirstOrDefault(x => x.UserId == CurrentUserId && x.ProductId == id);
            if (line != null)
                line.Quantity++;
            else
                _context.CartLines.Add(new CartLine
                {
                    UserId = CurrentUserId,
                    ProductId = product.Id,
                    ProductName = product.Name,
                    Price = product.Price,
                    ImageUrl = product.ImageUrl,
                    Quantity = 1
                });
            _context.SaveChanges();
        }
        else
        {
            var cart = GetCart();
            var item = cart.FirstOrDefault(p => p.Id == id);
            if (item != null)
                item.Quantity++;
            else
                cart.Add(new CartItem
                {
                    Id = product.Id,
                    Name = product.Name,
                    Price = product.Price,
                    ImageUrl = product.ImageUrl,
                    Quantity = 1
                });
            SaveCart(cart);
        }
        TempData["Success"] = $"Đã thêm {product.Name} vào giỏ hàng.";
        return RedirectToAction("Index");
    }

    // Giảm số lượng
    public IActionResult Decrease(int id)
    {
        if (IsAuthenticated && CurrentUserId != null)
        {
            var line = _context.CartLines.FirstOrDefault(x => x.UserId == CurrentUserId && x.ProductId == id);
            if (line != null)
            {
                line.Quantity--;
                if (line.Quantity <= 0)
                    _context.CartLines.Remove(line);
                _context.SaveChanges();
            }
        }
        else
        {
            var cart = GetCart();
            var item = cart.FirstOrDefault(p => p.Id == id);
            if (item != null)
            {
                item.Quantity--;
                if (item.Quantity <= 0)
                    cart.Remove(item);
                SaveCart(cart);
            }
        }
        TempData["Info"] = "Đã cập nhật giỏ hàng.";
        return RedirectToAction("Index");
    }

    // Tăng số lượng
    public IActionResult Increase(int id)
    {
        if (IsAuthenticated && CurrentUserId != null)
        {
            var line = _context.CartLines.FirstOrDefault(x => x.UserId == CurrentUserId && x.ProductId == id);
            if (line != null)
            {
                line.Quantity++;
                _context.SaveChanges();
            }
        }
        else
        {
            var cart = GetCart();
            var item = cart.FirstOrDefault(p => p.Id == id);
            if (item != null)
            {
                item.Quantity++;
                SaveCart(cart);
            }
        }
        TempData["Info"] = "Đã cập nhật giỏ hàng.";
        return RedirectToAction("Index");
    }

    // Cập nhật thủ công (nếu người dùng gõ số lượng)
    [HttpPost]
    public IActionResult Update(int id, int quantity)
    {
        if (IsAuthenticated && CurrentUserId != null)
        {
            var line = _context.CartLines.FirstOrDefault(x => x.UserId == CurrentUserId && x.ProductId == id);
            if (line != null && quantity > 0)
            {
                line.Quantity = quantity;
                _context.SaveChanges();
            }
        }
        else
        {
            var cart = GetCart();
            var item = cart.FirstOrDefault(p => p.Id == id);
            if (item != null && quantity > 0)
            {
                item.Quantity = quantity;
                SaveCart(cart);
            }
        }
        TempData["Info"] = "Đã cập nhật giỏ hàng.";
        return RedirectToAction("Index");
    }

    // Xóa 1 sản phẩm
    public IActionResult Remove(int id)
    {
        if (IsAuthenticated && CurrentUserId != null)
        {
            var line = _context.CartLines.FirstOrDefault(x => x.UserId == CurrentUserId && x.ProductId == id);
            if (line != null)
            {
                _context.CartLines.Remove(line);
                _context.SaveChanges();
            }
        }
        else
        {
            var cart = GetCart();
            var item = cart.FirstOrDefault(p => p.Id == id);
            if (item != null)
            {
                cart.Remove(item);
                SaveCart(cart);
            }
        }
        TempData["Success"] = "Đã xóa sản phẩm khỏi giỏ.";
        return RedirectToAction("Index");
    }

    // Xóa toàn bộ
    public IActionResult Clear()
    {
        if (IsAuthenticated && CurrentUserId != null)
        {
            var lines = _context.CartLines.Where(x => x.UserId == CurrentUserId);
            _context.CartLines.RemoveRange(lines);
            _context.SaveChanges();
        }
        else
        {
            HttpContext.Session.Remove(CARTKEY);
        }
        TempData["Success"] = "Đã xóa toàn bộ giỏ hàng.";
        return RedirectToAction("Index");
    }
}
