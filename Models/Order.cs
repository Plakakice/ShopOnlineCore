namespace ShopOnlineCore.Models;

public class Order
{
    public int Id { get; set; }
    public string CustomerName { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string Address { get; set; } = string.Empty;
    public DateTime CreatedDate { get; set; } = DateTime.Now;
    public List<CartItem> Items { get; set; } = new();
    public decimal Total => Items.Sum(x => x.Total);
}
