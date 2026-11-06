using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace BookShoppingCart.Data.Migrations
{
    /// <inheritdoc />
    public partial class ProtectBrowserAuthenticationTokens : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "ProtectedBrowserTokens",
                table: "ApiSessions",
                type: "nvarchar(max)",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "ProtectedBrowserTokens",
                table: "ApiSessions");
        }
    }
}
