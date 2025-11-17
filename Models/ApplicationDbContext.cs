using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using ShopOnlineCore.Models.Identity;

namespace ShopOnlineCore.Models
{
    public class ApplicationDbContext : IdentityDbContext<ApplicationUser>
    {
        public DbSet<Product> Products { get; set; }
        public DbSet<Order> Orders { get; set; }
        public DbSet<OrderItem> OrderItems { get; set; }
        public DbSet<CartLine> CartLines { get; set; }

        public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options)
            : base(options)
        {
        }

        // Cấu hình thêm để tránh lỗi decimal và set chính xác schema
        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            // Cấu hình cột Price cho SQLite (decimal không hỗ trợ trực tiếp)
            modelBuilder.Entity<Product>()
                .Property(p => p.Price)
                .HasConversion<double>(); // dùng double thay cho decimal cho SQLite

            modelBuilder.Entity<OrderItem>()
                .Property(oi => oi.Price)
                .HasConversion<double>();

            modelBuilder.Entity<CartLine>()
                .Property(c => c.Price)
                .HasConversion<double>();

            // Cấu hình relationship Order - OrderItem
            modelBuilder.Entity<Order>()
                .HasMany(o => o.OrderItems)
                .WithOne(oi => oi.Order)
                .HasForeignKey(oi => oi.OrderId)
                .OnDelete(DeleteBehavior.Cascade);
        }
    }
}
