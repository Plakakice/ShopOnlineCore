using Microsoft.AspNetCore.Mvc;
using ShopOnlineCore.Models;
using ShopOnlineCore.Services;

namespace ShopOnlineCore.Controllers;

public class CartController : Controller
{
    private readonly ICartService _cartService;

    public CartController(ICartService cartService)
    {
        _cartService = cartService;
    }

    public IActionResult Index()
    {
        var cart = _cartService.GetCart();
        return View(cart);
    }

    public IActionResult Add(int id, int quantity = 1, bool buyNow = false)
    {
        var result = _cartService.AddToCart(id, quantity);
        
        if (!result.Success)
        {
            TempData["Error"] = result.Message;
            return RedirectToAction("Details", "Product", new { id });
        }

        TempData["Success"] = "Đã thêm sản phẩm vào giỏ hàng.";

        if (buyNow)
        {
            return RedirectToAction("Index");
        }

        return RedirectToAction("Details", "Product", new { id, showMessage = 1 });
    }

    public IActionResult Decrease(int id)
    {
        var result = _cartService.DecreaseQuantity(id);
        if (!result.Success)
        {
            TempData["Error"] = result.Message;
        }
        else
        {
            TempData["Info"] = "Đã cập nhật giỏ hàng.";
        }
        return RedirectToAction("Index");
    }

    public IActionResult Increase(int id)
    {
        var result = _cartService.IncreaseQuantity(id);
        
        if (!result.Success)
        {
            TempData["Error"] = result.Message;
        }
        else
        {
            TempData["Info"] = "Đã cập nhật giỏ hàng.";
        }
        
        return RedirectToAction("Index");
    }

    [HttpPost]
    public IActionResult Update(int id, int quantity)
    {
        var result = _cartService.UpdateQuantity(id, quantity);
        
        if (!result.Success)
        {
            TempData["Error"] = result.Message;
        }
        else
        {
            TempData["Info"] = "Đã cập nhật giỏ hàng.";
        }
        return RedirectToAction("Index");
    }

    public IActionResult Remove(int id)
    {
        _cartService.RemoveFromCart(id);
        TempData["Success"] = "Đã xóa sản phẩm khỏi giỏ.";
        return RedirectToAction("Index");
    }

    public IActionResult Clear()
    {
        _cartService.ClearCart();
        TempData["Success"] = "Đã xóa toàn bộ giỏ hàng.";
        return RedirectToAction("Index");
    }
}
