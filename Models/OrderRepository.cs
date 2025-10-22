namespace ShopOnlineCore.Models;

public static class OrderRepository
{
    private static readonly List<Order> Orders = new();

    public static void Add(Order order)
    {
        order.Id = Orders.Count + 1;
        Orders.Add(order);
    }

    public static List<Order> GetAll() => Orders;
}
