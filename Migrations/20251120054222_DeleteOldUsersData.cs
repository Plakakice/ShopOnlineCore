using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ShopOnlineCore.Migrations
{
    /// <inheritdoc />
    public partial class DeleteOldUsersData : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // Delete all old users to ensure clean state
            migrationBuilder.Sql("DELETE FROM [AspNetUsers];");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            // No rollback needed for data cleanup
        }
    }
}
