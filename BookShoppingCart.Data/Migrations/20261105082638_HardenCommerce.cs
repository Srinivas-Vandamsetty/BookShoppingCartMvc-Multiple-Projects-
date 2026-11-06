using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace BookShoppingCart.Data.Migrations
{
    /// <inheritdoc />
    public partial class HardenCommerce : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // Stop for explicit data reconciliation rather than silently deleting/merging user records.
            migrationBuilder.Sql("""
IF EXISTS (SELECT 1 FROM [ShoppingCart] WHERE [UserId] IS NULL OR LEN([UserId]) = 0 OR LEN([UserId]) > 450)
    THROW 51001, 'Resolve missing or oversized cart owners before migration.', 1;
IF EXISTS (SELECT [UserId] FROM [ShoppingCart] GROUP BY [UserId] HAVING COUNT(*) > 1)
    THROW 51002, 'Resolve duplicate carts before migration.', 1;
IF EXISTS (SELECT [ShoppingCartId], [BookId] FROM [CartDetail] GROUP BY [ShoppingCartId], [BookId] HAVING COUNT(*) > 1)
    THROW 51003, 'Resolve duplicate cart lines before migration.', 1;
IF EXISTS (SELECT [NormalizedEmail] FROM [AspNetUsers] WHERE [NormalizedEmail] IS NOT NULL GROUP BY [NormalizedEmail] HAVING COUNT(*) > 1)
    THROW 51004, 'Resolve duplicate normalized account emails before migration.', 1;
IF EXISTS (SELECT UPPER(LTRIM(RTRIM([GenreName]))) COLLATE SQL_Latin1_General_CP1_CI_AS FROM [Genre]
    GROUP BY UPPER(LTRIM(RTRIM([GenreName]))) COLLATE SQL_Latin1_General_CP1_CI_AS HAVING COUNT(*) > 1)
    THROW 51005, 'Resolve duplicate genre names before migration.', 1;
IF EXISTS (SELECT 1 FROM [Stock] WHERE [Quantity] < 0)
 OR EXISTS (SELECT 1 FROM [CartDetail] WHERE [Quantity] NOT BETWEEN 1 AND 1000)
 OR EXISTS (SELECT 1 FROM [OrderDetail] WHERE [Quantity] NOT BETWEEN 1 AND 1000)
    THROW 51006, 'Resolve invalid stock/cart/order quantities before migration.', 1;
IF EXISTS (SELECT 1 FROM [Book] WHERE TRY_CONVERT(decimal(18,2),[Price]) IS NULL OR TRY_CONVERT(decimal(18,2),[Price]) <= 0)
 OR EXISTS (SELECT 1 FROM [Order] WHERE LEN([UserId]) > 450)
    THROW 51007, 'Resolve invalid currency values or oversized order owners before migration.', 1;
IF EXISTS (SELECT [StatusName] FROM [OrderStatus] GROUP BY [StatusName] HAVING COUNT(*) > 1)
    THROW 51008, 'Resolve duplicate order status names before migration.', 1;
UPDATE [Genre] SET [GenreName] = LTRIM(RTRIM([GenreName]));
""");

            migrationBuilder.DropForeignKey(
                name: "FK_Book_Genre_GenreId",
                table: "Book");

            migrationBuilder.DropForeignKey(
                name: "FK_OrderDetail_Book_BookId",
                table: "OrderDetail");

            migrationBuilder.DropIndex(
                name: "IX_CartDetail_ShoppingCartId",
                table: "CartDetail");

            migrationBuilder.DropIndex(
                name: "EmailIndex",
                table: "AspNetUsers");

            migrationBuilder.AlterColumn<string>(
                name: "UserId",
                table: "ShoppingCart",
                type: "nvarchar(450)",
                maxLength: 450,
                nullable: false,
                defaultValue: "",
                oldClrType: typeof(string),
                oldType: "nvarchar(max)",
                oldNullable: true);

            migrationBuilder.AlterColumn<decimal>(
                name: "UnitPrice",
                table: "OrderDetail",
                type: "decimal(18,2)",
                precision: 18,
                scale: 2,
                nullable: false,
                oldClrType: typeof(double),
                oldType: "float");

            migrationBuilder.AddColumn<string>(
                name: "AuthorName",
                table: "OrderDetail",
                type: "nvarchar(40)",
                maxLength: 40,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "BookName",
                table: "OrderDetail",
                type: "nvarchar(40)",
                maxLength: 40,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "GenreName",
                table: "OrderDetail",
                type: "nvarchar(40)",
                maxLength: 40,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AlterColumn<string>(
                name: "UserId",
                table: "Order",
                type: "nvarchar(450)",
                maxLength: 450,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "nvarchar(max)");

            migrationBuilder.AddColumn<Guid>(
                name: "CheckoutKey",
                table: "Order",
                type: "uniqueidentifier",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "CouponCode",
                table: "Order",
                type: "nvarchar(20)",
                maxLength: 20,
                nullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "Discount",
                table: "Order",
                type: "decimal(18,2)",
                precision: 18,
                scale: 2,
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<decimal>(
                name: "Shipping",
                table: "Order",
                type: "decimal(18,2)",
                precision: 18,
                scale: 2,
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<decimal>(
                name: "Subtotal",
                table: "Order",
                type: "decimal(18,2)",
                precision: 18,
                scale: 2,
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<decimal>(
                name: "Total",
                table: "Order",
                type: "decimal(18,2)",
                precision: 18,
                scale: 2,
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AlterColumn<string>(
                name: "GenreName",
                table: "Genre",
                type: "nvarchar(40)",
                maxLength: 40,
                nullable: false,
                collation: "SQL_Latin1_General_CP1_CI_AS",
                oldClrType: typeof(string),
                oldType: "nvarchar(40)",
                oldMaxLength: 40);

            migrationBuilder.AlterColumn<decimal>(
                name: "UnitPrice",
                table: "CartDetail",
                type: "decimal(18,2)",
                precision: 18,
                scale: 2,
                nullable: false,
                oldClrType: typeof(double),
                oldType: "float");

            migrationBuilder.AlterColumn<decimal>(
                name: "Price",
                table: "Book",
                type: "decimal(18,2)",
                precision: 18,
                scale: 2,
                nullable: false,
                oldClrType: typeof(double),
                oldType: "float");

            migrationBuilder.AlterColumn<string>(
                name: "Image",
                table: "Book",
                type: "nvarchar(max)",
                nullable: false,
                defaultValue: "",
                oldClrType: typeof(string),
                oldType: "nvarchar(max)",
                oldNullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "IsArchived",
                table: "Book",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddCheckConstraint(
                name: "CK_Stock_Quantity",
                table: "Stock",
                sql: "[Quantity] >= 0");

            migrationBuilder.CreateIndex(
                name: "IX_ShoppingCart_UserId",
                table: "ShoppingCart",
                column: "UserId",
                unique: true);

            migrationBuilder.AddCheckConstraint(
                name: "CK_OrderDetail_Quantity",
                table: "OrderDetail",
                sql: "[Quantity] BETWEEN 1 AND 1000");

            migrationBuilder.CreateIndex(
                name: "IX_Order_UserId_CheckoutKey",
                table: "Order",
                columns: new[] { "UserId", "CheckoutKey" },
                unique: true,
                filter: "[CheckoutKey] IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "IX_Order_UserId_CreateDate",
                table: "Order",
                columns: new[] { "UserId", "CreateDate" });

            migrationBuilder.CreateIndex(
                name: "IX_Genre_GenreName",
                table: "Genre",
                column: "GenreName",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_CartDetail_ShoppingCartId_BookId",
                table: "CartDetail",
                columns: new[] { "ShoppingCartId", "BookId" },
                unique: true);

            migrationBuilder.AddCheckConstraint(
                name: "CK_CartDetail_Quantity",
                table: "CartDetail",
                sql: "[Quantity] BETWEEN 1 AND 1000");

            migrationBuilder.AddCheckConstraint(
                name: "CK_Book_Price",
                table: "Book",
                sql: "[Price] > 0");

            migrationBuilder.CreateIndex(
                name: "EmailIndex",
                table: "AspNetUsers",
                column: "NormalizedEmail",
                unique: true,
                filter: "[NormalizedEmail] IS NOT NULL");

            migrationBuilder.AddForeignKey(
                name: "FK_Book_Genre_GenreId",
                table: "Book",
                column: "GenreId",
                principalTable: "Genre",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_OrderDetail_Book_BookId",
                table: "OrderDetail",
                column: "BookId",
                principalTable: "Book",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            // Preserve existing order history using the best data available at upgrade time.
            // Historical discounts/shipping were never stored and cannot be reconstructed.
            migrationBuilder.Sql("""
EXEC(N'UPDATE d SET d.[BookName]=b.[BookName], d.[AuthorName]=b.[AuthorName], d.[GenreName]=g.[GenreName]
FROM [OrderDetail] d JOIN [Book] b ON d.[BookId]=b.[Id] JOIN [Genre] g ON b.[GenreId]=g.[Id];');
EXEC(N'UPDATE o SET o.[Subtotal]=COALESCE(t.[Amount],0), o.[Total]=COALESCE(t.[Amount],0)
FROM [Order] o LEFT JOIN
    (SELECT [OrderId], SUM([UnitPrice]*[Quantity]) AS [Amount] FROM [OrderDetail] GROUP BY [OrderId]) t
    ON o.[Id]=t.[OrderId];');
IF NOT EXISTS (SELECT 1 FROM [OrderStatus] WHERE [StatusName]='Pending')
    INSERT INTO [OrderStatus] ([StatusId],[StatusName]) VALUES (1,'Pending');
IF NOT EXISTS (SELECT 1 FROM [OrderStatus] WHERE [StatusName]='Shipped')
    INSERT INTO [OrderStatus] ([StatusId],[StatusName]) VALUES (2,'Shipped');
IF NOT EXISTS (SELECT 1 FROM [OrderStatus] WHERE [StatusName]='Delivered')
    INSERT INTO [OrderStatus] ([StatusId],[StatusName]) VALUES (3,'Delivered');
IF NOT EXISTS (SELECT 1 FROM [OrderStatus] WHERE [StatusName]='Cancelled')
    INSERT INTO [OrderStatus] ([StatusId],[StatusName]) VALUES (4,'Cancelled');
IF NOT EXISTS (SELECT 1 FROM [OrderStatus] WHERE [StatusName]='Returned')
    INSERT INTO [OrderStatus] ([StatusId],[StatusName]) VALUES (5,'Returned');
IF NOT EXISTS (SELECT 1 FROM [OrderStatus] WHERE [StatusName]='Refund')
    INSERT INTO [OrderStatus] ([StatusId],[StatusName]) VALUES (6,'Refund');
""");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Book_Genre_GenreId",
                table: "Book");

            migrationBuilder.DropForeignKey(
                name: "FK_OrderDetail_Book_BookId",
                table: "OrderDetail");

            migrationBuilder.DropCheckConstraint(
                name: "CK_Stock_Quantity",
                table: "Stock");

            migrationBuilder.DropIndex(
                name: "IX_ShoppingCart_UserId",
                table: "ShoppingCart");

            migrationBuilder.DropCheckConstraint(
                name: "CK_OrderDetail_Quantity",
                table: "OrderDetail");

            migrationBuilder.DropIndex(
                name: "IX_Order_UserId_CheckoutKey",
                table: "Order");

            migrationBuilder.DropIndex(
                name: "IX_Order_UserId_CreateDate",
                table: "Order");

            migrationBuilder.DropIndex(
                name: "IX_Genre_GenreName",
                table: "Genre");

            migrationBuilder.DropIndex(
                name: "IX_CartDetail_ShoppingCartId_BookId",
                table: "CartDetail");

            migrationBuilder.DropCheckConstraint(
                name: "CK_CartDetail_Quantity",
                table: "CartDetail");

            migrationBuilder.DropCheckConstraint(
                name: "CK_Book_Price",
                table: "Book");

            migrationBuilder.DropIndex(
                name: "EmailIndex",
                table: "AspNetUsers");

            migrationBuilder.DropColumn(
                name: "AuthorName",
                table: "OrderDetail");

            migrationBuilder.DropColumn(
                name: "BookName",
                table: "OrderDetail");

            migrationBuilder.DropColumn(
                name: "GenreName",
                table: "OrderDetail");

            migrationBuilder.DropColumn(
                name: "CheckoutKey",
                table: "Order");

            migrationBuilder.DropColumn(
                name: "CouponCode",
                table: "Order");

            migrationBuilder.DropColumn(
                name: "Discount",
                table: "Order");

            migrationBuilder.DropColumn(
                name: "Shipping",
                table: "Order");

            migrationBuilder.DropColumn(
                name: "Subtotal",
                table: "Order");

            migrationBuilder.DropColumn(
                name: "Total",
                table: "Order");

            migrationBuilder.DropColumn(
                name: "IsArchived",
                table: "Book");

            migrationBuilder.AlterColumn<string>(
                name: "UserId",
                table: "ShoppingCart",
                type: "nvarchar(max)",
                nullable: true,
                oldClrType: typeof(string),
                oldType: "nvarchar(450)",
                oldMaxLength: 450);

            migrationBuilder.AlterColumn<double>(
                name: "UnitPrice",
                table: "OrderDetail",
                type: "float",
                nullable: false,
                oldClrType: typeof(decimal),
                oldType: "decimal(18,2)",
                oldPrecision: 18,
                oldScale: 2);

            migrationBuilder.AlterColumn<string>(
                name: "UserId",
                table: "Order",
                type: "nvarchar(max)",
                nullable: false,
                oldClrType: typeof(string),
                oldType: "nvarchar(450)",
                oldMaxLength: 450);

            migrationBuilder.AlterColumn<string>(
                name: "GenreName",
                table: "Genre",
                type: "nvarchar(40)",
                maxLength: 40,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "nvarchar(40)",
                oldMaxLength: 40,
                oldCollation: "SQL_Latin1_General_CP1_CI_AS");

            migrationBuilder.AlterColumn<double>(
                name: "UnitPrice",
                table: "CartDetail",
                type: "float",
                nullable: false,
                oldClrType: typeof(decimal),
                oldType: "decimal(18,2)",
                oldPrecision: 18,
                oldScale: 2);

            migrationBuilder.AlterColumn<double>(
                name: "Price",
                table: "Book",
                type: "float",
                nullable: false,
                oldClrType: typeof(decimal),
                oldType: "decimal(18,2)",
                oldPrecision: 18,
                oldScale: 2);

            migrationBuilder.AlterColumn<string>(
                name: "Image",
                table: "Book",
                type: "nvarchar(max)",
                nullable: true,
                oldClrType: typeof(string),
                oldType: "nvarchar(max)");

            migrationBuilder.CreateIndex(
                name: "IX_CartDetail_ShoppingCartId",
                table: "CartDetail",
                column: "ShoppingCartId");

            migrationBuilder.CreateIndex(
                name: "EmailIndex",
                table: "AspNetUsers",
                column: "NormalizedEmail");

            migrationBuilder.AddForeignKey(
                name: "FK_Book_Genre_GenreId",
                table: "Book",
                column: "GenreId",
                principalTable: "Genre",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_OrderDetail_Book_BookId",
                table: "OrderDetail",
                column: "BookId",
                principalTable: "Book",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }
    }
}
