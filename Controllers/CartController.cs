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

    public async Task<IActionResult> Index()
    {
        var cart = await _cartService.GetCartItemsAsync();
        return View(cart);
    }

    public async Task<IActionResult> Add(int id, int quantity = 1, bool buyNow = false)
    {
        var result = await _cartService.AddToCartAsync(id, quantity);
        
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

    public async Task<IActionResult> Decrease(int id)
    {
        var result = await _cartService.DecreaseQuantityAsync(id);
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

    public async Task<IActionResult> Increase(int id)
    {
        var result = await _cartService.IncreaseQuantityAsync(id);
        
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
    public async Task<IActionResult> Update(int id, int quantity)
    {
        var result = await _cartService.UpdateQuantityAsync(id, quantity);
        
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

    public async Task<IActionResult> Remove(int id)
    {
        await _cartService.RemoveFromCartAsync(id);
        TempData["Success"] = "Đã xóa sản phẩm khỏi giỏ.";
        return RedirectToAction("Index");
    }

    public async Task<IActionResult> Clear()
    {
        await _cartService.ClearCartAsync();
        TempData["Success"] = "Đã xóa toàn bộ giỏ hàng.";
        return RedirectToAction("Index");
    }
}
