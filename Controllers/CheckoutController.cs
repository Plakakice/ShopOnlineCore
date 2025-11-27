using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using ShopOnlineCore.Models;
using System.Security.Claims;
using Microsoft.AspNetCore.Identity;
using ShopOnlineCore.Models.Identity;
using ShopOnlineCore.Services;

namespace ShopOnlineCore.Controllers;

[Authorize]
public class CheckoutController : Controller
{
    private readonly IOrderService _orderService;
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly OrderRepository _orderRepository;
    private readonly ICartService _cartService;

    public CheckoutController(IOrderService orderService, UserManager<ApplicationUser> userManager, OrderRepository orderRepository, ICartService cartService)
    {
        _orderService = orderService;
        _userManager = userManager;
        _orderRepository = orderRepository;
        _cartService = cartService;
    }

    private List<CartItem> GetCart()
    {
        return _cartService.GetCart();
    }

    private void ClearCart()
    {
        _cartService.ClearCart();
    }

    [HttpGet]
    public async Task<IActionResult> Index()
    {
        var cart = GetCart();
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
            var lastOrder = (await _orderRepository.GetByUserAsync(user.Id))
                .OrderByDescending(o => o.CreatedDate)
                .FirstOrDefault();

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
        var cart = GetCart();
        if (!cart.Any())
            return RedirectToAction("Index", "Cart");

        var user = await _userManager.GetUserAsync(User);
        if (user == null)
            return Challenge();

        var result = await _orderService.QuickCheckoutAsync(user, cart);

        if (!result.Success)
        {
            if (result.Message == "MissingUserInfo")
            {
                return RedirectToAction("Index");
            }
            
            TempData["Error"] = result.Message;
            return RedirectToAction("Index", "Cart");
        }

        // Xóa giỏ hàng
        ClearCart();

        TempData["Success"] = "Đặt hàng thành công! Cảm ơn bạn đã mua sắm.";
        return RedirectToAction("Success");
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Index(Order model)
    {
        var cart = GetCart();
        if (!cart.Any())
            return RedirectToAction("Index", "Cart");

        var user = await _userManager.GetUserAsync(User);
        if (user == null)
        {
            return Challenge(); // yêu cầu đăng nhập
        }

        if (!ModelState.IsValid)
        {
            ViewBag.Total = cart.Sum(x => x.Total);
            return View(model);
        }

        var result = await _orderService.PlaceOrderAsync(user, model, cart);

        if (!result.Success)
        {
            TempData["Error"] = result.Message;
            ViewBag.Total = cart.Sum(x => x.Total);
            return View(model);
        }

        // Xóa giỏ hàng
        ClearCart();
        
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

        var order = await _orderRepository.GetByIdAsync(id);

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
