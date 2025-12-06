using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using ShopOnlineCore.Models;
using ShopOnlineCore.Models.Identity;
using ShopOnlineCore.Repositories;

namespace ShopOnlineCore.Services;

public class OrderService : IOrderService
{
    private readonly ApplicationDbContext _context;
    private readonly IOrderRepository _orderRepository;
    private readonly ILogger<OrderService> _logger;

    public OrderService(ApplicationDbContext context, IOrderRepository orderRepository, ILogger<OrderService> logger)
    {
        _context = context;
        _orderRepository = orderRepository;
        _logger = logger;
    }

    public async Task<ServiceResult> QuickCheckoutAsync(ApplicationUser user, List<CartItem> cartItems)
    {
        // 1. Validate User Info
        var customerName = user.FullName ?? user.UserName;
        var email = user.Email;
        var address = user.Address;
        var phoneNumber = user.PhoneNumber;

        // Fallback to last order if profile is incomplete
        if (string.IsNullOrWhiteSpace(address) || string.IsNullOrWhiteSpace(phoneNumber))
        {
            var lastOrder = await _context.Orders
                .Where(o => o.UserId == user.Id)
                .OrderByDescending(o => o.CreatedDate)
                .FirstOrDefaultAsync();

            if (lastOrder != null)
            {
                if (string.IsNullOrWhiteSpace(customerName)) customerName = lastOrder.CustomerName;
                if (string.IsNullOrWhiteSpace(address)) address = lastOrder.Address;
                if (string.IsNullOrWhiteSpace(phoneNumber)) phoneNumber = lastOrder.PhoneNumber;
            }
        }

        if (string.IsNullOrWhiteSpace(customerName) || 
            string.IsNullOrWhiteSpace(email) || 
            string.IsNullOrWhiteSpace(address) || 
            string.IsNullOrWhiteSpace(phoneNumber))
        {
            return ServiceResult.Fail("MissingUserInfo");
        }

        // 2. Create Order Object
        var order = new Order
        {
            UserId = user.Id,
            CustomerName = customerName,
            Email = email,
            Address = address,
            PhoneNumber = phoneNumber,
            CreatedDate = DateTime.Now,
            Status = "Pending"
        };

        // 3. Place Order
        return await PlaceOrderAsync(user, order, cartItems);
    }

    public async Task<ServiceResult> PlaceOrderAsync(ApplicationUser user, Order orderDetails, List<CartItem> cartItems)
    {
        if (!cartItems.Any())
            return ServiceResult.Fail("Giỏ hàng trống");

        // Sử dụng IsolationLevel.ReadCommitted (mặc định) kết hợp với UPDLOCK hint
        using var transaction = await _context.Database.BeginTransactionAsync();
        try
        {
            // 1. Fetch Products with Row-Level Lock (UPDLOCK, ROWLOCK)
            // Thay vì batch fetch, ta fetch từng cái để lock chính xác.
            // Performance: Với số lượng item nhỏ (<20), điều này không ảnh hưởng nhiều.
            var products = new List<Product>();
            var outOfStockItems = new List<string>();

            // ID của các sản phẩm trong giỏ để kiểm tra duplicate nếu cần
            foreach (var item in cartItems)
            {
                // Raw SQL để Lock Row: Tránh Race Condition khi nhiều người mua cùng lúc
                // WITH (UPDLOCK, ROWLOCK): Khóa dòng cho đến khi transaction kết thúc (commit/rollback)
                var product = await _context.Products
                    .FromSqlRaw("SELECT * FROM Products WITH (UPDLOCK, ROWLOCK) WHERE Id = {0}", item.Id)
                    .FirstOrDefaultAsync();

                if (product == null)
                {
                    outOfStockItems.Add($"{item.Name} - Sản phẩm không tồn tại");
                    continue; // Skip check stock
                }
                
                products.Add(product); // Add to local list to use later

                if (product.Stock < item.Quantity)
                {
                    outOfStockItems.Add($"{item.Name} - Chỉ còn {product.Stock} sản phẩm (bạn đặt {item.Quantity})");
                }
            }

            if (outOfStockItems.Any())
            {
                await transaction.RollbackAsync();
                return ServiceResult.Fail("Một số sản phẩm không đủ hàng:<br>" + string.Join("<br>", outOfStockItems));
            }

            // 2. Prepare Order Items
            orderDetails.OrderItems = cartItems.Select(item => new OrderItem
            {
                ProductId = item.Id,
                ProductName = item.Name,
                Price = item.Price,
                Quantity = item.Quantity,
                ImageUrl = item.ImageUrl
            }).ToList();

            if (string.IsNullOrEmpty(orderDetails.UserId))
            {
                orderDetails.UserId = user.Id;
            }

            // 3. Save Order
            await _orderRepository.AddAsync(orderDetails);

            // 4. Deduct Stock
            foreach (var item in cartItems)
            {
                // Products đã được load và track bởi EF Core (và đang bị lock)
                var product = products.First(p => p.Id == item.Id);
                product.Stock -= item.Quantity;
                // Không cần gọi Update explicit vì EF Core Change Tracking sẽ lo
            }
            await _context.SaveChangesAsync();

            await transaction.CommitAsync();
            _logger.LogInformation("Order placed successfully. OrderID: {OrderId}, User: {UserId}", orderDetails.Id, user.Id);
            
            return ServiceResult.Ok("Đặt hàng thành công", orderDetails.Id);
        }
        catch (Exception ex)
        {
            await transaction.RollbackAsync();
            _logger.LogError(ex, "Error placing order for user {UserId}", user.Id);
            return ServiceResult.Fail("Đã có lỗi xảy ra khi xử lý đơn hàng. Vui lòng thử lại sau.");
        }
    }
}
