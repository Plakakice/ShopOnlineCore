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
    private bool IsAuthenticated => User?.Identity?.IsAuthenticated == true;
    private string? CurrentUserId => User.FindFirstValue(ClaimTypes.NameIdentifier);

    public List<CartItem> GetCart()
    {
        if (IsAuthenticated && CurrentUserId != null)
        {
            var lines = _context.CartLines.Where(x => x.UserId == CurrentUserId).ToList();
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
            return JsonSerializer.Deserialize<List<CartItem>>(json)!;
        return new List<CartItem>();
    }

    private void SaveCart(List<CartItem> cart)
    {
        if (IsAuthenticated && CurrentUserId != null)
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
            _context.SaveChanges();
            return;
        }
        var json = JsonSerializer.Serialize(cart);
        HttpContext.Session.SetString(CARTKEY, json);
    }

    public void AddToCart(int productId, int quantity)
    {
        var product = _context.Products.FirstOrDefault(p => p.Id == productId);
        if (product == null) throw new Exception("Sản phẩm không tồn tại");

        if (product.Stock <= 0) throw new Exception($"{product.Name} hiện đã hết hàng.");
        if (quantity > product.Stock) quantity = product.Stock;

        if (IsAuthenticated && CurrentUserId != null)
        {
            var line = _context.CartLines.FirstOrDefault(x => x.UserId == CurrentUserId && x.ProductId == productId);
            if (line != null)
            {
                if (line.Quantity + quantity > product.Stock)
                {
                    throw new Exception($"{product.Name} chỉ còn {product.Stock - line.Quantity} sản phẩm trong kho.");
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
            _context.SaveChanges();
        }
        else
        {
            var cart = GetCart();
            var item = cart.FirstOrDefault(p => p.Id == productId);
            if (item != null)
            {
                if (item.Quantity + quantity > product.Stock)
                {
                    throw new Exception($"{product.Name} chỉ còn {product.Stock - item.Quantity} sản phẩm trong kho.");
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
            SaveCart(cart);
        }
    }

    public void DecreaseQuantity(int productId)
    {
        if (IsAuthenticated && CurrentUserId != null)
        {
            var line = _context.CartLines.FirstOrDefault(x => x.UserId == CurrentUserId && x.ProductId == productId);
            if (line != null)
            {
                line.Quantity--;
                if (line.Quantity <= 0)
                    _context.CartLines.Remove(line);
                _context.SaveChanges();
            }
        }
        else
        {
            var cart = GetCart();
            var item = cart.FirstOrDefault(p => p.Id == productId);
            if (item != null)
            {
                item.Quantity--;
                if (item.Quantity <= 0)
                    cart.Remove(item);
                SaveCart(cart);
            }
        }
    }

    public void IncreaseQuantity(int productId)
    {
        var product = _context.Products.FirstOrDefault(p => p.Id == productId);
        if (product == null) return;

        if (IsAuthenticated && CurrentUserId != null)
        {
            var line = _context.CartLines.FirstOrDefault(x => x.UserId == CurrentUserId && x.ProductId == productId);
            if (line != null)
            {
                if (line.Quantity >= product.Stock)
                    throw new Exception($"{product.Name} chỉ còn {product.Stock} sản phẩm trong kho.");
                
                line.Quantity++;
                _context.SaveChanges();
            }
        }
        else
        {
            var cart = GetCart();
            var item = cart.FirstOrDefault(p => p.Id == productId);
            if (item != null)
            {
                if (item.Quantity >= product.Stock)
                    throw new Exception($"{product.Name} chỉ còn {product.Stock} sản phẩm trong kho.");

                item.Quantity++;
                SaveCart(cart);
            }
        }
    }

    public void UpdateQuantity(int productId, int quantity)
    {
        if (quantity <= 0) return;

        if (IsAuthenticated && CurrentUserId != null)
        {
            var line = _context.CartLines.FirstOrDefault(x => x.UserId == CurrentUserId && x.ProductId == productId);
            if (line != null)
            {
                line.Quantity = quantity;
                _context.SaveChanges();
            }
        }
        else
        {
            var cart = GetCart();
            var item = cart.FirstOrDefault(p => p.Id == productId);
            if (item != null)
            {
                item.Quantity = quantity;
                SaveCart(cart);
            }
        }
    }

    public void RemoveFromCart(int productId)
    {
        if (IsAuthenticated && CurrentUserId != null)
        {
            var line = _context.CartLines.FirstOrDefault(x => x.UserId == CurrentUserId && x.ProductId == productId);
            if (line != null)
            {
                _context.CartLines.Remove(line);
                _context.SaveChanges();
            }
        }
        else
        {
            var cart = GetCart();
            var item = cart.FirstOrDefault(p => p.Id == productId);
            if (item != null)
            {
                cart.Remove(item);
                SaveCart(cart);
            }
        }
    }

    public void ClearCart()
    {
        if (IsAuthenticated && CurrentUserId != null)
        {
            var lines = _context.CartLines.Where(x => x.UserId == CurrentUserId);
            _context.CartLines.RemoveRange(lines);
            _context.SaveChanges();
        }
        else
        {
            HttpContext.Session.Remove(CARTKEY);
        }
    }
}
