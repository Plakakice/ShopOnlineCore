using System.Security.Claims;
using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using ShopOnlineCore.Models;

namespace ShopOnlineCore.Services;

public class CartService : ICartService
{
    private readonly ApplicationDbContext _context;
    private readonly IHttpContextAccessor _httpContextAccessor;
    private const string CARTKEY = "cart";

    public CartService(ApplicationDbContext context, IHttpContextAccessor httpContextAccessor)
    {
        _context = context;
        _httpContextAccessor = httpContextAccessor;
    }

    private HttpContext HttpContext => _httpContextAccessor.HttpContext!;
    private ClaimsPrincipal User => HttpContext.User;
    // private bool IsAuthenticated => User?.Identity?.IsAuthenticated == true; // Redundant
    private string? CurrentUserId => User.FindFirstValue(ClaimTypes.NameIdentifier);

    public async Task<List<CartItem>> GetCartItemsAsync()
    {
        if (CurrentUserId != null)
        {
            var lines = await _context.CartLines.Where(x => x.UserId == CurrentUserId).ToListAsync();
            return lines.Select(l => new CartItem
            {
                Id = l.ProductId,
                Name = l.ProductName,
                Price = l.Price,
                Quantity = l.Quantity,
                ImageUrl = l.ImageUrl
            }).ToList();
        }

        var json = HttpContext.Session.GetString(CARTKEY);
        if (json != null)
        {
             var items = JsonSerializer.Deserialize<List<CartItem>>(json);
             return items ?? new List<CartItem>();
        }
        return new List<CartItem>();
    }

    private async Task SaveCartAsync(List<CartItem> cart)
    {
        if (CurrentUserId != null)
        {
            var existing = _context.CartLines.Where(x => x.UserId == CurrentUserId);
            _context.CartLines.RemoveRange(existing);
            _context.CartLines.AddRange(cart.Select(c => new CartLine
            {
                UserId = CurrentUserId,
                ProductId = c.Id,
                ProductName = c.Name,
                Price = c.Price,
                Quantity = c.Quantity,
                ImageUrl = c.ImageUrl
            }));
            await _context.SaveChangesAsync();
            return;
        }
        var json = JsonSerializer.Serialize(cart);
        HttpContext.Session.SetString(CARTKEY, json);
        await Task.CompletedTask;
    }

    public async Task<ServiceResult> AddToCartAsync(int productId, int quantity)
    {
        var product = await _context.Products.FirstOrDefaultAsync(p => p.Id == productId);
        if (product == null) return ServiceResult.Fail("Sản phẩm không tồn tại");

        if (product.Stock <= 0) return ServiceResult.Fail($"{product.Name} hiện đã hết hàng.");
        if (quantity > product.Stock) quantity = product.Stock;

        if (CurrentUserId != null)
        {
            var line = await _context.CartLines.FirstOrDefaultAsync(x => x.UserId == CurrentUserId && x.ProductId == productId);
            if (line != null)
            {
                if (line.Quantity + quantity > product.Stock)
                {
                    return ServiceResult.Fail($"{product.Name} chỉ còn {product.Stock - line.Quantity} sản phẩm trong kho.");
                }
                line.Quantity += quantity;
            }
            else
            {
                _context.CartLines.Add(new CartLine
                {
                    UserId = CurrentUserId,
                    ProductId = product.Id,
                    ProductName = product.Name,
                    Price = product.Price,
                    ImageUrl = product.ImageUrl,
                    Quantity = quantity
                });
            }
            await _context.SaveChangesAsync();
        }
        else
        {
            var cart = await GetCartItemsAsync();
            var item = cart.FirstOrDefault(p => p.Id == productId);
            if (item != null)
            {
                if (item.Quantity + quantity > product.Stock)
                {
                    return ServiceResult.Fail($"{product.Name} chỉ còn {product.Stock - item.Quantity} sản phẩm trong kho.");
                }
                item.Quantity += quantity;
            }
            else
            {
                cart.Add(new CartItem
                {
                    Id = product.Id,
                    Name = product.Name,
                    Price = product.Price,
                    ImageUrl = product.ImageUrl,
                    Quantity = quantity
                });
            }
            await SaveCartAsync(cart);
        }
        return ServiceResult.Ok();
    }

    public async Task<ServiceResult> DecreaseQuantityAsync(int productId)
    {
        if (CurrentUserId != null)
        {
            var line = await _context.CartLines.FirstOrDefaultAsync(x => x.UserId == CurrentUserId && x.ProductId == productId);
            if (line != null)
            {
                line.Quantity--;
                if (line.Quantity <= 0)
                    _context.CartLines.Remove(line);
                await _context.SaveChangesAsync();
            }
        }
        else
        {
            var cart = await GetCartItemsAsync();
            var item = cart.FirstOrDefault(p => p.Id == productId);
            if (item != null)
            {
                item.Quantity--;
                if (item.Quantity <= 0)
                    cart.Remove(item);
                await SaveCartAsync(cart);
            }
        }
        return ServiceResult.Ok();
    }

    public async Task<ServiceResult> IncreaseQuantityAsync(int productId)
    {
        var product = await _context.Products.FirstOrDefaultAsync(p => p.Id == productId);
        if (product == null) return ServiceResult.Fail("Sản phẩm không tồn tại");

        if (CurrentUserId != null)
        {
            var line = await _context.CartLines.FirstOrDefaultAsync(x => x.UserId == CurrentUserId && x.ProductId == productId);
            if (line != null)
            {
                if (line.Quantity >= product.Stock)
                    return ServiceResult.Fail($"{product.Name} chỉ còn {product.Stock} sản phẩm trong kho.");
                
                line.Quantity++;
                await _context.SaveChangesAsync();
            }
        }
        else
        {
            var cart = await GetCartItemsAsync();
            var item = cart.FirstOrDefault(p => p.Id == productId);
            if (item != null)
            {
                if (item.Quantity >= product.Stock)
                    return ServiceResult.Fail($"{product.Name} chỉ còn {product.Stock} sản phẩm trong kho.");

                item.Quantity++;
                await SaveCartAsync(cart);
            }
        }
        return ServiceResult.Ok();
    }

    public async Task<ServiceResult> UpdateQuantityAsync(int productId, int quantity)
    {
        if (quantity <= 0) return ServiceResult.Ok();

        var product = await _context.Products.FirstOrDefaultAsync(p => p.Id == productId);
        if (product != null && quantity > product.Stock)
             return ServiceResult.Fail($"{product.Name} chỉ còn {product.Stock} sản phẩm.");

        if (CurrentUserId != null)
        {
            var line = await _context.CartLines.FirstOrDefaultAsync(x => x.UserId == CurrentUserId && x.ProductId == productId);
            if (line != null)
            {
                line.Quantity = quantity;
                await _context.SaveChangesAsync();
            }
        }
        else
        {
            var cart = await GetCartItemsAsync();
            var item = cart.FirstOrDefault(p => p.Id == productId);
            if (item != null)
            {
                item.Quantity = quantity;
                await SaveCartAsync(cart);
            }
        }
        return ServiceResult.Ok();
    }

    public async Task RemoveFromCartAsync(int productId)
    {
        if (CurrentUserId != null)
        {
            var line = await _context.CartLines.FirstOrDefaultAsync(x => x.UserId == CurrentUserId && x.ProductId == productId);
            if (line != null)
            {
                _context.CartLines.Remove(line);
                await _context.SaveChangesAsync();
            }
        }
        else
        {
            var cart = await GetCartItemsAsync();
            var item = cart.FirstOrDefault(p => p.Id == productId);
            if (item != null)
            {
                cart.Remove(item);
                await SaveCartAsync(cart);
            }
        }
    }

    public async Task ClearCartAsync()
    {
        if (CurrentUserId != null)
        {
            var lines = _context.CartLines.Where(x => x.UserId == CurrentUserId);
            _context.CartLines.RemoveRange(lines);
            await _context.SaveChangesAsync();
        }
        else
        {
            HttpContext.Session.Remove(CARTKEY);
            await Task.CompletedTask;
        }
    }
}
