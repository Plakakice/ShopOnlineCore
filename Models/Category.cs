using System.ComponentModel.DataAnnotations;

namespace ShopOnlineCore.Models
{
    public class Category
    {
        public int Id { get; set; }

        [Required(ErrorMessage = "Tên danh mục không được để trống")]
        public string Name { get; set; } = string.Empty;

        public string? Description { get; set; }
    }
}
