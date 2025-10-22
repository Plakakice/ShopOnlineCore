namespace ShopOnlineCore.Models;

public class CartItem
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public decimal Price { get; set; }
    public int Quantity { get; set; } = 1;
    public string ImageUrl { get; set; } = string.Empty;

    public decimal Total => Price * Quantity;
}
