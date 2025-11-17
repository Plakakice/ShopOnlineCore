using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ShopOnlineCore.Migrations
{
    /// <inheritdoc />
    public partial class UpdateCategoriesToVietnamese : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // Update Men to Chuột
            migrationBuilder.Sql("UPDATE Products SET Category = 'Chuột' WHERE Category = 'Men' OR Category = 'Men''s' OR Category LIKE '%men%'");
            
            // Update Bags to Bàn phím
            migrationBuilder.Sql("UPDATE Products SET Category = 'Bàn phím' WHERE Category = 'Bags' OR Category LIKE '%bag%'");
            
            // Update Footwear to Âm thanh
            migrationBuilder.Sql("UPDATE Products SET Category = 'Âm thanh' WHERE Category = 'Footwear' OR Category LIKE '%footwear%' OR Category LIKE '%shoe%'");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            // Rollback: Âm thanh back to Footwear
            migrationBuilder.Sql("UPDATE Products SET Category = 'Footwear' WHERE Category = 'Âm thanh'");
            
            // Rollback: Bàn phím back to Bags
            migrationBuilder.Sql("UPDATE Products SET Category = 'Bags' WHERE Category = 'Bàn phím'");
            
            // Rollback: Chuột back to Men
            migrationBuilder.Sql("UPDATE Products SET Category = 'Men' WHERE Category = 'Chuột'");
        }
    }
}
