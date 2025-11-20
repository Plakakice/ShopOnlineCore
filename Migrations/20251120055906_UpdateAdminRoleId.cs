using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ShopOnlineCore.Migrations
{
    /// <inheritdoc />
    public partial class UpdateAdminRoleId : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // Get current Admin role ID and update with constraint handling
            migrationBuilder.Sql(@"
                DECLARE @OldAdminId NVARCHAR(450);
                SELECT @OldAdminId = [Id] FROM [AspNetRoles] WHERE [Name] = 'Admin';
                
                IF @OldAdminId IS NOT NULL
                BEGIN
                    -- Disable foreign key constraint temporarily
                    ALTER TABLE [AspNetUserRoles] NOCHECK CONSTRAINT [FK_AspNetUserRoles_AspNetRoles_RoleId];
                    
                    -- Update user-role relationships first
                    UPDATE [AspNetUserRoles]
                    SET [RoleId] = 'cab19749-7cf8-4c7b-91d7-afdbb1944c0d'
                    WHERE [RoleId] = @OldAdminId;
                    
                    -- Update the Admin role ID
                    UPDATE [AspNetRoles]
                    SET [Id] = 'cab19749-7cf8-4c7b-91d7-afdbb1944c0d'
                    WHERE [Id] = @OldAdminId;
                    
                    -- Re-enable foreign key constraint
                    ALTER TABLE [AspNetUserRoles] CHECK CONSTRAINT [FK_AspNetUserRoles_AspNetRoles_RoleId];
                END
            ");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            // This migration cannot be safely rolled back as we don't know the original Admin role ID
            // Migration intentionally left empty for Down
        }
    }
}
