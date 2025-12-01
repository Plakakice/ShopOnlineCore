using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ShopOnlineCore.Migrations
{
    /// <inheritdoc />
    public partial class FixImageGallery : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // Add new column
            migrationBuilder.AddColumn<decimal>(
                name: "SalePrice",
                table: "Products",
                type: "decimal(18,2)",
                precision: 18,
                scale: 2,
                nullable: true);

            // Copy data if OldPrice exists (using raw SQL for safety, assuming SQL Server)
            migrationBuilder.Sql("UPDATE Products SET SalePrice = OldPrice");

            // Drop old column
            migrationBuilder.DropColumn(
                name: "OldPrice",
                table: "Products");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<decimal>(
                name: "OldPrice",
                table: "Products",
                type: "decimal(18,2)",
                precision: 18,
                scale: 2,
                nullable: true);

            migrationBuilder.Sql("UPDATE Products SET OldPrice = SalePrice");

            migrationBuilder.DropColumn(
                name: "SalePrice",
                table: "Products");
        }
    }
}
