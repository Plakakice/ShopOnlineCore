using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using ShopOnlineCore.Models;

namespace ShopOnlineCore.Areas.Admin.Controllers;

[Area("Admin")]
[Authorize(Roles = "Admin")]
public class ProductsController : Controller
{
    private readonly ApplicationDbContext _context;

    public ProductsController(ApplicationDbContext context)
    {
        _context = context;
    }

    // Danh sách sản phẩm với bảng quản lý
    public async Task<IActionResult> Index(string? search, string? category, int page = 1, int pageSize = 20)
    {
        var products = _context.Products.AsQueryable();

        // Tìm kiếm
        if (!string.IsNullOrWhiteSpace(search))
        {
            products = products.Where(p => 
                p.Name.Contains(search) || 
                p.Category.Contains(search) ||
                p.Description.Contains(search));
            ViewData["Search"] = search;
        }

        // Lọc theo category
        if (!string.IsNullOrWhiteSpace(category))
        {
            products = products.Where(p => p.Category == category);
            ViewData["Category"] = category;
        }

        var totalCount = await products.CountAsync();
        var productList = await products
            .OrderByDescending(p => p.Id)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync();

        ViewData["Page"] = page;
        ViewData["PageSize"] = pageSize;
        ViewData["TotalCount"] = totalCount;
        ViewData["TotalPages"] = (int)Math.Ceiling(totalCount / (double)pageSize);

        // Thống kê
        ViewData["TotalProducts"] = await _context.Products.CountAsync();
        ViewData["TotalInStock"] = await _context.Products.Where(p => p.Stock > 0).CountAsync();
        ViewData["TotalOutOfStock"] = await _context.Products.Where(p => p.Stock == 0).CountAsync();
        ViewData["TotalValue"] = await _context.Products.SumAsync(p => p.Price * p.Stock);

        return View(productList);
    }

    // Cập nhật nhanh Stock
    [HttpPost]
    public async Task<IActionResult> UpdateStock(int id, int stock)
    {
        var product = await _context.Products.FindAsync(id);
        if (product == null)
            return Json(new { success = false, message = "Không tìm thấy sản phẩm" });

        product.Stock = stock;
        await _context.SaveChangesAsync();

        return Json(new { success = true, message = "Cập nhật thành công", isAvailable = product.IsAvailable });
    }

    // Xóa nhiều sản phẩm
    [HttpPost]
    public async Task<IActionResult> BulkDelete(int[] ids)
    {
        if (ids == null || ids.Length == 0)
            return Json(new { success = false, message = "Chưa chọn sản phẩm nào" });

        var products = await _context.Products.Where(p => ids.Contains(p.Id)).ToListAsync();
        _context.Products.RemoveRange(products);
        await _context.SaveChangesAsync();

        return Json(new { success = true, message = $"Đã xóa {products.Count} sản phẩm" });
    }

    // Cập nhật giá hàng loạt
    [HttpPost]
    public async Task<IActionResult> BulkUpdatePrice(int[] ids, decimal percentage, string action)
    {
        if (ids == null || ids.Length == 0)
            return Json(new { success = false, message = "Chưa chọn sản phẩm nào" });

        var products = await _context.Products.Where(p => ids.Contains(p.Id)).ToListAsync();
        
        foreach (var product in products)
        {
            if (action == "increase")
                product.Price = product.Price * (1 + percentage / 100);
            else if (action == "decrease")
                product.Price = product.Price * (1 - percentage / 100);
        }

        await _context.SaveChangesAsync();

        return Json(new { success = true, message = $"Đã cập nhật giá cho {products.Count} sản phẩm" });
    }
}
