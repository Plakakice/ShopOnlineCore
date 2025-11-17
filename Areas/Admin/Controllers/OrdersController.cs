using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using ShopOnlineCore.Models;

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
    private readonly OrderRepository _repository;

    public OrdersController(ApplicationDbContext context, OrderRepository repository)
    {
        _context = context;
        _repository = repository;
    }

    [HttpGet]
    public async Task<IActionResult> Index(string? status, string? q, DateTime? from, DateTime? to, int page = 1, int pageSize = 20)
    {
        var query = _context.Orders
            .AsNoTracking()
            .Include(o => o.OrderItems)
            .OrderByDescending(o => o.CreatedDate)
            .AsQueryable();

        if (!string.IsNullOrWhiteSpace(status))
        {
            query = query.Where(o => o.Status == status);
        }
        if (!string.IsNullOrWhiteSpace(q))
        {
            var term = q.Trim().ToLower();
            query = query.Where(o =>
                o.CustomerName.ToLower().Contains(term) ||
                o.Email.ToLower().Contains(term) ||
                o.Id.ToString().Contains(term));
        }
        if (from.HasValue)
        {
            var f = from.Value.Date;
            query = query.Where(o => o.CreatedDate >= f);
        }
        if (to.HasValue)
        {
            var t = to.Value.Date.AddDays(1).AddTicks(-1);
            query = query.Where(o => o.CreatedDate <= t);
        }

        var totalCount = await query.CountAsync();
        var orders = await query
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync();

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
        var order = await _repository.GetByIdAsync(id);
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
