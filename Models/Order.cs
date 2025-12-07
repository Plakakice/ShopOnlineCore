using System.ComponentModel.DataAnnotations;
using ShopOnlineCore.Models.Identity;

namespace ShopOnlineCore.Models;

public class Order
{
    public int Id { get; set; }
    public string? UserId { get; set; }
    public ApplicationUser? User { get; set; }
    [Required(ErrorMessage = "Vui lòng nhập họ và tên")]
    [StringLength(100)]
    public string CustomerName { get; set; } = string.Empty;

    [Required(ErrorMessage = "Vui lòng nhập email")]
    [EmailAddress(ErrorMessage = "Email không hợp lệ")]
    public string Email { get; set; } = string.Empty;

    [Required(ErrorMessage = "Vui lòng nhập địa chỉ giao hàng")]
    [StringLength(255)]
    public string Address { get; set; } = string.Empty;

    [Phone(ErrorMessage = "Số điện thoại không hợp lệ")]
    public string? PhoneNumber { get; set; }
    public DateTime CreatedDate { get; set; } = DateTime.UtcNow;
    public string Status { get; set; } = "Pending"; // Pending, Processing, Shipped, Delivered, Cancelled
    
    // Navigation property for EF Core
    public List<OrderItem> OrderItems { get; set; } = new();
    
    // Calculated total from OrderItems
    public decimal Total => OrderItems.Sum(x => x.Quantity * x.Price);
}
