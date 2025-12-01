using Microsoft.EntityFrameworkCore;
using ShopOnlineCore.Models;
using ShopOnlineCore.Models.Identity;
using ShopOnlineCore.Repositories;

namespace ShopOnlineCore.Services;

public class OrderService : IOrderService
{
    private readonly ApplicationDbContext _context;
    private readonly IOrderRepository _orderRepository;

    public OrderService(ApplicationDbContext context, IOrderRepository orderRepository)
    {
        _context = context;
        _orderRepository = orderRepository;
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

        using var transaction = await _context.Database.BeginTransactionAsync();
        try
        {
            // 1. Batch Fetch Products (Performance Optimization)
            var productIds = cartItems.Select(c => c.Id).ToList();
            var products = await _context.Products
                .Where(p => productIds.Contains(p.Id))
                .ToListAsync();

            // 2. Check Stock & Validate
            var outOfStockItems = new List<string>();
            foreach (var item in cartItems)
            {
                var product = products.FirstOrDefault(p => p.Id == item.Id);
                if (product == null)
                {
                    outOfStockItems.Add($"{item.Name} - Sản phẩm không tồn tại");
                }
                else if (product.Stock < item.Quantity)
                {
                    outOfStockItems.Add($"{item.Name} - Chỉ còn {product.Stock} sản phẩm (bạn đặt {item.Quantity})");
                }
            }

            if (outOfStockItems.Any())
            {
                await transaction.RollbackAsync();
                return ServiceResult.Fail("Một số sản phẩm không đủ hàng:<br>" + string.Join("<br>", outOfStockItems));
            }

            // 3. Prepare Order Items
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

            // 4. Save Order
            // Note: We use _context directly here to ensure it's part of the same transaction scope easily,
            // or ensure OrderRepository uses the same context instance (which it does as Scoped).
            // However, OrderRepository.AddAsync calls SaveChangesAsync immediately.
            // To keep it in transaction, we just need to ensure SaveChanges is called.
            // Let's use _orderRepository.AddAsync. Since it shares _context (Scoped), it participates in the transaction.
            await _orderRepository.AddAsync(orderDetails);

            // 5. Deduct Stock
            foreach (var item in cartItems)
            {
                var product = products.First(p => p.Id == item.Id);
                product.Stock -= item.Quantity;
            }
            await _context.SaveChangesAsync();

            await transaction.CommitAsync();
            return ServiceResult.Ok("Đặt hàng thành công");
        }
        catch (Exception ex)
        {
            await transaction.RollbackAsync();
            // Log error here if logger is available
            return ServiceResult.Fail("Đã có lỗi xảy ra khi xử lý đơn hàng: " + ex.Message);
        }
    }
}
