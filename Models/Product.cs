using System.ComponentModel.DataAnnotations;

namespace ShopOnlineCore.Models
{
    public class Product
    {
        public int Id { get; set; }
        public string Name { get; set; } = string.Empty;
        
        // Old Category string (will be removed later)
        public string? Category { get; set; } 

        // New Category Relation
        public int? CategoryId { get; set; }
        public Category? CategoryObj { get; set; }

        public decimal Price { get; set; }
        public decimal? OldPrice { get; set; } // Giá cũ (khi giảm giá)
        public string Description { get; set; } = string.Empty;

        // Ảnh chính
        public string ImageUrl { get; set; } = string.Empty;

        // Danh sách ảnh khác
        public List<string> ImageGallery { get; set; } = new();

        // Tồn kho
        [Range(0, int.MaxValue, ErrorMessage = "Số lượng phải >= 0")]
        public int Stock { get; set; } = 0;

        // Trạng thái còn hàng
        public bool IsAvailable => Stock > 0;
    }
}
