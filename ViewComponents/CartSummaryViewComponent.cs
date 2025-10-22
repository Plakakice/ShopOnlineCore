using Microsoft.AspNetCore.Mvc;
using System.Text.Json;
using ShopOnlineCore.Models;

namespace ShopOnlineCore.ViewComponents;

public class CartSummaryViewComponent : ViewComponent
{
    private const string CARTKEY = "cart";

    public IViewComponentResult Invoke()
    {
        var session = HttpContext.Session;
        var json = session.GetString(CARTKEY);
        int count = 0;

        if (json != null)
        {
            var cart = JsonSerializer.Deserialize<List<CartItem>>(json)!;
            count = cart.Sum(x => x.Quantity);
        }

        return View(count);
    }
}