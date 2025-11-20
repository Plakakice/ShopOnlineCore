using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using ShopOnlineCore.Models;
using ShopOnlineCore.Models.Identity;

namespace ShopOnlineCore.Areas.Admin.Controllers;

[Area("Admin")]
[Authorize(Roles = "Admin")]
public class UsersController : Controller
{
    private readonly ApplicationDbContext _context;
    private readonly UserManager<ApplicationUser> _userManager;

    public UsersController(ApplicationDbContext context, UserManager<ApplicationUser> userManager)
    {
        _context = context;
        _userManager = userManager;
    }

    // Danh sách người dùng
    public async Task<IActionResult> Index(int page = 1, int pageSize = 20)
    {
        var totalUsers = await _context.Users.CountAsync();
        var totalOrders = await _context.Orders.CountAsync();
        var totalCustomers = await _context.Orders.Select(o => o.Email).Distinct().CountAsync();
        
        // Tính tổng doanh thu bằng cách lấy tất cả orders có items, rồi tính tổng
        var orders = await _context.Orders.Include(o => o.OrderItems).ToListAsync();
        var totalRevenue = orders.Sum(o => o.OrderItems.Sum(oi => oi.Quantity * oi.Price));

        // Lấy danh sách người dùng
        var users = await _context.Users
            .OrderByDescending(u => u.UserName)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync();

        // Tính thống kê
        ViewData["TotalUsers"] = totalUsers;
        ViewData["TotalOrders"] = totalOrders;
        ViewData["TotalCustomers"] = totalCustomers;
        ViewData["TotalRevenue"] = totalRevenue;
        ViewData["Page"] = page;
        ViewData["PageSize"] = pageSize;
        ViewData["TotalPages"] = (int)Math.Ceiling(totalUsers / (double)pageSize);

        return View(users);
    }

    // Chi tiết người dùng
    public async Task<IActionResult> Details(string id)
    {
        var user = await _userManager.FindByIdAsync(id);
        if (user == null)
            return NotFound();

        // Lấy các đơn hàng của người dùng
        var orders = await _context.Orders
            .Where(o => o.UserId == id)
            .Include(o => o.OrderItems)
            .OrderByDescending(o => o.CreatedDate)
            .ToListAsync();

        ViewData["Orders"] = orders;
        return View(user);
    }

    // Lấy danh sách khách hàng duy nhất từ đơn hàng
    public async Task<IActionResult> Customers(string? search, int page = 1, int pageSize = 20)
    {
        var query = _context.Orders
            .GroupBy(o => o.Email)
            .Select(g => new
            {
                Email = g.Key,
                Name = g.First().CustomerName,
                Phone = g.First().PhoneNumber,
                Address = g.First().Address,
                OrderCount = g.Count(),
                LastOrderDate = g.Max(o => o.CreatedDate),
                TotalSpent = g.Sum(o => o.OrderItems.Sum(oi => oi.Quantity * oi.Price))
            })
            .AsQueryable();

        // Tìm kiếm
        if (!string.IsNullOrWhiteSpace(search))
        {
            var searchLower = search.ToLower();
            query = query.Where(c => 
                c.Email.ToLower().Contains(searchLower) || 
                c.Name.ToLower().Contains(searchLower) ||
                (c.Phone != null && c.Phone.ToLower().Contains(searchLower)));
        }

        var totalCount = await query.CountAsync();
        var customers = await query
            .OrderByDescending(c => c.LastOrderDate)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync();

        ViewData["Search"] = search;
        ViewData["Page"] = page;
        ViewData["PageSize"] = pageSize;
        ViewData["TotalCount"] = totalCount;
        ViewData["TotalPages"] = (int)Math.Ceiling(totalCount / (double)pageSize);

        return View(customers);
    }

    // Chi tiết khách hàng (từ Orders)
    public async Task<IActionResult> CustomerDetails(string email)
    {
        var orders = await _context.Orders
            .Where(o => o.Email == email)
            .Include(o => o.OrderItems)
            .OrderByDescending(o => o.CreatedDate)
            .ToListAsync();

        if (orders.Count == 0)
            return NotFound();

        var customerInfo = orders.First();
        ViewData["CustomerName"] = customerInfo.CustomerName;
        ViewData["Email"] = email;
        ViewData["PhoneNumber"] = customerInfo.PhoneNumber;
        ViewData["Address"] = customerInfo.Address;
        ViewData["OrderCount"] = orders.Count;
        ViewData["TotalSpent"] = orders.Sum(o => o.OrderItems.Sum(oi => oi.Quantity * oi.Price));

        return View(orders);
    }
}
