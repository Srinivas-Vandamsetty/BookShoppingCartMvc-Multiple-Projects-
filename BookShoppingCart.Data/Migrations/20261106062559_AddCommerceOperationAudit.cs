using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace BookShoppingCart.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddCommerceOperationAudit : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "CommerceAuditEvents",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Kind = table.Column<string>(type: "nvarchar(30)", maxLength: 30, nullable: false),
                    EntityId = table.Column<int>(type: "int", nullable: false),
                    ActorId = table.Column<string>(type: "nvarchar(450)", maxLength: 450, nullable: false),
                    RequestHash = table.Column<string>(type: "varchar(64)", unicode: false, maxLength: 64, nullable: false),
                    Before = table.Column<string>(type: "nvarchar(64)", maxLength: 64, nullable: false),
                    After = table.Column<string>(type: "nvarchar(64)", maxLength: 64, nullable: false),
                    Reason = table.Column<string>(type: "nvarchar(300)", maxLength: 300, nullable: false),
                    CreatedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CommerceAuditEvents", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_CommerceAuditEvents_Kind_EntityId_CreatedAtUtc",
                table: "CommerceAuditEvents",
                columns: new[] { "Kind", "EntityId", "CreatedAtUtc" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "CommerceAuditEvents");
        }
    }
}
