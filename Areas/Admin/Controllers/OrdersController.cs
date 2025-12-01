using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using ShopOnlineCore.Models;
using ShopOnlineCore.Repositories;

namespace ShopOnlineCore.Areas.Admin.Controllers;

[Area("Admin")]
[Authorize(Roles = "Admin")]
[Route("Admin/[controller]/[action]")]
public class OrdersController : Controller
{
    private static readonly string[] AllowedStatuses = new[]
    {
        "Pending", "Processing", "Shipped", "Delivered", "Cancelled"
    };

    private readonly ApplicationDbContext _context;
    private readonly IOrderRepository _repository;

    public OrdersController(ApplicationDbContext context, IOrderRepository repository)
    {
        _context = context;
        _repository = repository;
    }

    [HttpGet]
    public async Task<IActionResult> Index(string? status, string? q, DateTime? from, DateTime? to, int page = 1, int pageSize = 20)
    {
        var (orders, totalCount) = await _repository.GetOrdersAsync(status, q, from, to, page, pageSize);

        ViewData["StatusFilter"] = status;
        ViewData["Query"] = q;
        ViewData["From"] = from?.ToString("yyyy-MM-dd");
        ViewData["To"] = to?.ToString("yyyy-MM-dd");
        ViewData["Page"] = page;
        ViewData["PageSize"] = pageSize;
        ViewData["TotalCount"] = totalCount;
        ViewData["AllowedStatuses"] = AllowedStatuses;

        return View(orders);
    }

    [HttpGet]
    public async Task<IActionResult> Details(int id)
    {
        var order = await _repository.GetOrderByIdAsync(id);
        if (order == null) return NotFound();
        ViewData["AllowedStatuses"] = AllowedStatuses;
        return View(order);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> UpdateStatus(int id, string status, string? returnUrl = null)
    {
        if (!AllowedStatuses.Contains(status))
        {
            TempData["Error"] = "Trạng thái không hợp lệ.";
            return RedirectToAction(nameof(Details), new { id });
        }

        await _repository.UpdateStatusAsync(id, status);
        TempData["Success"] = "Cập nhật trạng thái đơn hàng thành công.";

        if (!string.IsNullOrEmpty(returnUrl) && Url.IsLocalUrl(returnUrl))
        {
            return Redirect(returnUrl);
        }
        return RedirectToAction(nameof(Details), new { id });
    }
}
