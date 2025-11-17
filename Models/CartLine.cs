using ShopOnlineCore.Models.Identity;

namespace ShopOnlineCore.Models;

public class CartLine
{
    public int Id { get; set; }
    public string UserId { get; set; } = string.Empty;

    public int ProductId { get; set; }
    public string ProductName { get; set; } = string.Empty;
    public decimal Price { get; set; }
    public int Quantity { get; set; } = 1;
    public string ImageUrl { get; set; } = string.Empty;

    public DateTime CreatedDate { get; set; } = DateTime.Now;
}
