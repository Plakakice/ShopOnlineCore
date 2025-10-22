using Microsoft.AspNetCore.Mvc;
using ShopOnlineCore.Models;
using System.Text.Json;

namespace ShopOnlineCore.Controllers;

public class CartController : Controller
{
    private readonly ApplicationDbContext _context;

public CartController(ApplicationDbContext context)
{
    _context = context;
}

    private const string CARTKEY = "cart";

    // Lấy giỏ hàng hiện tại từ Session
    private List<CartItem> GetCart()
    {
        var json = HttpContext.Session.GetString(CARTKEY);
        if (json != null)
            return JsonSerializer.Deserialize<List<CartItem>>(json)!;
        return new List<CartItem>();
    }

    // Lưu giỏ hàng vào Session
    private void SaveCart(List<CartItem> cart)
    {
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
        TempData["Message"] = $"Đã thêm {product.Name} vào giỏ hàng.";
        return RedirectToAction("Index");
    }

    // Giảm số lượng
    public IActionResult Decrease(int id)
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
        return RedirectToAction("Index");
    }

    // Tăng số lượng
    public IActionResult Increase(int id)
    {
        var cart = GetCart();
        var item = cart.FirstOrDefault(p => p.Id == id);
        if (item != null)
        {
            item.Quantity++;
            SaveCart(cart);
        }
        return RedirectToAction("Index");
    }

    // Cập nhật thủ công (nếu người dùng gõ số lượng)
    [HttpPost]
    public IActionResult Update(int id, int quantity)
    {
        var cart = GetCart();
        var item = cart.FirstOrDefault(p => p.Id == id);
        if (item != null && quantity > 0)
        {
            item.Quantity = quantity;
            SaveCart(cart);
        }
        return RedirectToAction("Index");
    }

    // Xóa 1 sản phẩm
    public IActionResult Remove(int id)
    {
        var cart = GetCart();
        var item = cart.FirstOrDefault(p => p.Id == id);
        if (item != null)
        {
            cart.Remove(item);
            SaveCart(cart);
        }
        return RedirectToAction("Index");
    }

    // Xóa toàn bộ
    public IActionResult Clear()
    {
        HttpContext.Session.Remove(CARTKEY);
        return RedirectToAction("Index");
    }
}
