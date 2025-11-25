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
        try
        {
            _cartService.AddToCart(id, quantity);
            TempData["Success"] = "Đã thêm sản phẩm vào giỏ hàng.";
        }
        catch (Exception ex)
        {
            TempData["Error"] = ex.Message;
            return RedirectToAction("Details", "Product", new { id });
        }

        if (buyNow)
        {
            return RedirectToAction("Index");
        }

        return RedirectToAction("Details", "Product", new { id, showMessage = 1 });
    }

    public IActionResult Decrease(int id)
    {
        _cartService.DecreaseQuantity(id);
        TempData["Info"] = "Đã cập nhật giỏ hàng.";
        return RedirectToAction("Index");
    }

    public IActionResult Increase(int id)
    {
        try
        {
            _cartService.IncreaseQuantity(id);
            TempData["Info"] = "Đã cập nhật giỏ hàng.";
        }
        catch (Exception ex)
        {
            TempData["Error"] = ex.Message;
        }
        return RedirectToAction("Index");
    }

    [HttpPost]
    public IActionResult Update(int id, int quantity)
    {
        _cartService.UpdateQuantity(id, quantity);
        TempData["Info"] = "Đã cập nhật giỏ hàng.";
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
