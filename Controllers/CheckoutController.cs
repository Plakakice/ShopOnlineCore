using Microsoft.AspNetCore.Mvc;
using ShopOnlineCore.Models;
using System.Text.Json;

namespace ShopOnlineCore.Controllers;

public class CheckoutController : Controller
{
    private const string CARTKEY = "cart";

    private List<CartItem> GetCart()
    {
        var json = HttpContext.Session.GetString(CARTKEY);
        if (json != null)
            return JsonSerializer.Deserialize<List<CartItem>>(json)!;
        return new List<CartItem>();
    }

    private void ClearCart()
    {
        HttpContext.Session.Remove(CARTKEY);
    }

    [HttpGet]
    public IActionResult Index()
    {
        var cart = GetCart();
        if (!cart.Any())
            return RedirectToAction("Index", "Cart");

        ViewBag.Total = cart.Sum(x => x.Total);
        return View();
    }

    [HttpPost]
    public IActionResult Index(Order model)
    {
        var cart = GetCart();
        if (!cart.Any())
            return RedirectToAction("Index", "Cart");

        model.Items = cart;
        OrderRepository.Add(model);
        ClearCart();

        return RedirectToAction("Success");
    }

    public IActionResult Success()
    {
        return View();
    }

    // (tùy chọn) Xem danh sách đơn hàng đã tạo
    public IActionResult Orders()
    {
        var orders = OrderRepository.GetAll();
        return View(orders);
    }
}
