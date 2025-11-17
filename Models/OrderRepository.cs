using Microsoft.EntityFrameworkCore;

namespace ShopOnlineCore.Models;

public class OrderRepository
{
    private readonly ApplicationDbContext _context;

    public OrderRepository(ApplicationDbContext context)
    {
        _context = context;
    }

    public void Add(Order order)
    {
        _context.Orders.Add(order);
        _context.SaveChanges();
    }

    public async Task AddAsync(Order order, CancellationToken ct = default)
    {
        await _context.Orders.AddAsync(order, ct);
        await _context.SaveChangesAsync(ct);
    }

    public List<Order> GetAll()
    {
        return _context.Orders
            .Include(o => o.OrderItems)
            .OrderByDescending(o => o.CreatedDate)
            .ToList();
    }

    public async Task<List<Order>> GetAllAsync(CancellationToken ct = default)
    {
        return await _context.Orders
            .Include(o => o.OrderItems)
            .OrderByDescending(o => o.CreatedDate)
            .ToListAsync(ct);
    }

    public List<Order> GetByUser(string userId)
    {
        return _context.Orders
            .Where(o => o.UserId == userId)
            .Include(o => o.OrderItems)
            .OrderByDescending(o => o.CreatedDate)
            .ToList();
    }

    public async Task<List<Order>> GetByUserAsync(string userId, CancellationToken ct = default)
    {
        return await _context.Orders
            .Where(o => o.UserId == userId)
            .Include(o => o.OrderItems)
            .OrderByDescending(o => o.CreatedDate)
            .ToListAsync(ct);
    }

    public Order? GetById(int id)
    {
        return _context.Orders
            .Include(o => o.OrderItems)
            .FirstOrDefault(o => o.Id == id);
    }

    public async Task<Order?> GetByIdAsync(int id, CancellationToken ct = default)
    {
        return await _context.Orders
            .Include(o => o.OrderItems)
            .FirstOrDefaultAsync(o => o.Id == id, ct);
    }

    public void UpdateStatus(int orderId, string status)
    {
        var order = _context.Orders.Find(orderId);
        if (order != null)
        {
            order.Status = status;
            _context.SaveChanges();
        }
    }

    public async Task UpdateStatusAsync(int orderId, string status, CancellationToken ct = default)
    {
        var order = await _context.Orders.FindAsync(new object?[] { orderId }, ct);
        if (order != null)
        {
            order.Status = status;
            await _context.SaveChangesAsync(ct);
        }
    }
}
