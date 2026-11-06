using BookShoppingCart.Data.Data;
using BookShoppingCart.Data.Repositories;
using BookShoppingCart.Models.Models;
using BookShoppingCart.Models.Models.DTOs;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
namespace BookShoppingCart.Tests.Tests;
public sealed class SqlServerFactAttribute : FactAttribute
{
    public SqlServerFactAttribute()
    {
        if (string.IsNullOrEmpty(Environment.GetEnvironmentVariable("BOOKSTORE_TEST_SQL")))
            Skip = "Set BOOKSTORE_TEST_SQL to an isolated SQL Server test-server connection to run SQL-specific migration/concurrency tests.";
    }
}
public class SqlServerIntegrationTests
{
    [Fact] public void SqlServerModel_MatchesMigrationSnapshot()
    {
        using var db = new DesignTimeDbContextFactory().CreateDbContext([]);
        Assert.False(db.Database.HasPendingModelChanges());
    }
    [SqlServerFact]
    public async Task MigrationsAndConcurrentLastUnitCheckout_PreserveInventory()
    {
        var connection = new SqlConnectionStringBuilder(Environment.GetEnvironmentVariable("BOOKSTORE_TEST_SQL")!)
        { InitialCatalog = "BookShoppingCartTests_" + Guid.NewGuid().ToString("N") };
        var options = new DbContextOptionsBuilder<ApplicationDbContext>().UseSqlServer(connection.ConnectionString).Options;
        await using var setup = new ApplicationDbContext(options);
        var created = false;
        try
        {
            await setup.Database.MigrateAsync();
            created = true;
            Assert.True(await setup.OrderStatuses.AnyAsync(s=>s.StatusName=="Pending"));
            var genre=new Genre { GenreName="Concurrency test" };
            setup.Books.Add(new Book { BookName="Final unit", AuthorName="Test", Price=100, Genre=genre, Stock=new Stock { Quantity=1 } });
            await setup.SaveChangesAsync();
            var bookId=await setup.Books.Select(b=>b.Id).SingleAsync();
            foreach (var owner in new[] { "buyer-one","buyer-two" })
            {
                await using var db=new ApplicationDbContext(options);
                await new CartRepository(db,new TestUser(owner)).AddItem(bookId,1);
            }
            async Task<bool> Buy(string owner)
            {
                await using var db=new ApplicationDbContext(options);
                try
                {
                    return await new CartRepository(db,new TestUser(owner)).DoCheckout(new CheckoutModel
                    { Name="Buyer", Email="buyer@example.com", Address="Address", MobileNumber="9876543210", PaymentMethod="cash", CheckoutKey=Guid.NewGuid() });
                }
                catch (InvalidOperationException) { return false; }
            }
            var results=await Task.WhenAll(Buy("buyer-one"),Buy("buyer-two"));
            Assert.Single(results, x=>x);
            Assert.Equal(1,await setup.Orders.CountAsync());
            Assert.Equal(0,await setup.Stocks.Select(s=>s.Quantity).SingleAsync());
            setup.Books.Add(new Book { BookName="Concurrent cart", AuthorName="Test", GenreId=genre.Id,
                Price=10, Stock=new Stock { Quantity=5 } });
            await setup.SaveChangesAsync();
            var nextBookId=await setup.Books.MaxAsync(b=>b.Id);
            async Task Add()
            {
                await using var db=new ApplicationDbContext(options);
                await new CartRepository(db,new TestUser("same-buyer")).AddItem(nextBookId,1);
            }
            await Task.WhenAll(Add(),Add());
            Assert.Equal(1,await setup.ShoppingCarts.CountAsync(c=>c.UserId=="same-buyer"));
            Assert.Equal(2,await setup.CartDetails.Where(d=>d.ShoppingCart.UserId=="same-buyer").Select(d=>d.Quantity).SingleAsync());
        }
        finally
        {
            // Only the new, randomly named test database created above may be removed.
            if (created && connection.InitialCatalog.StartsWith("BookShoppingCartTests_",StringComparison.Ordinal))
                await setup.Database.EnsureDeletedAsync();
        }
    }

    [SqlServerFact]
    public async Task UpgradeExistingDatabase_BackfillsHistoryAndSeedsMissingStatuses()
    {
        var connection = new SqlConnectionStringBuilder(Environment.GetEnvironmentVariable("BOOKSTORE_TEST_SQL")!)
        { InitialCatalog = "BookShoppingCartTests_" + Guid.NewGuid().ToString("N") };
        await using var db = new ApplicationDbContext(new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseSqlServer(connection.ConnectionString).Options);
        var created = false;
        try
        {
            await db.GetService<IMigrator>().MigrateAsync("20250331111632_InitialCreate");
            created = true;
            await db.Database.ExecuteSqlRawAsync("""
INSERT INTO [Genre] ([GenreName]) VALUES ('History');
INSERT INTO [Book] ([BookName],[AuthorName],[Price],[GenreId]) VALUES ('Legacy Book','Legacy Author',19.99,1);
INSERT INTO [OrderStatus] ([StatusId],[StatusName]) VALUES (1,'Pending');
INSERT INTO [Order] ([UserId],[CreateDate],[OrderStatusId],[IsDeleted],[Name],[Email],[MobileNumber],[Address],[PaymentMethod],[IsPaid])
    VALUES ('legacy-owner',GETUTCDATE(),1,0,'Reader','reader@example.com','9876543210','Address','cash',0);
INSERT INTO [OrderDetail] ([OrderId],[BookId],[Quantity],[UnitPrice]) VALUES (1,1,2,19.99);
""");
            await db.Database.MigrateAsync();
            var order = await db.Orders.Include(o=>o.OrderDetail).SingleAsync();
            Assert.Equal(39.98m,order.Total);
            Assert.Equal("Legacy Book",order.OrderDetail.Single().BookName);
            Assert.Equal(19.99m,order.OrderDetail.Single().UnitPrice);
            Assert.Equal(6,await db.OrderStatuses.CountAsync());
            Assert.Null(order.CheckoutKey);
        }
        finally
        {
            if (created && connection.InitialCatalog.StartsWith("BookShoppingCartTests_",StringComparison.Ordinal))
                await db.Database.EnsureDeletedAsync();
        }
    }

    [SqlServerFact]
    public async Task GeneratedIdempotentScript_CreatesAndUpgradesDatabase_AndCanBeReplayed()
    {
        var connection = new SqlConnectionStringBuilder(Environment.GetEnvironmentVariable("BOOKSTORE_TEST_SQL")!)
        { InitialCatalog = "BookShoppingCartTests_" + Guid.NewGuid().ToString("N") };
        await using var db = new ApplicationDbContext(new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseSqlServer(connection.ConnectionString).Options);
        var created = false;
        try
        {
            await db.GetService<Microsoft.EntityFrameworkCore.Storage.IRelationalDatabaseCreator>().CreateAsync();
            created = true;
            await db.Database.OpenConnectionAsync();
            var script = db.GetService<IMigrator>().GenerateScript(options: MigrationsSqlGenerationOptions.Idempotent);
            var batches = System.Text.RegularExpressions.Regex.Split(script, @"^\s*GO\s*$",
                System.Text.RegularExpressions.RegexOptions.Multiline | System.Text.RegularExpressions.RegexOptions.IgnoreCase);
            for (var replay = 0; replay < 2; replay++)
                foreach (var batch in batches.Where(b => !string.IsNullOrWhiteSpace(b)))
                    await db.Database.ExecuteSqlRawAsync(batch);
            Assert.Equal(6, await db.OrderStatuses.CountAsync());
            Assert.Contains("20261105082638_HardenCommerce", await db.Database.GetAppliedMigrationsAsync());
            Assert.Empty(await db.Books.Where(b => b.IsArchived).ToListAsync());
            Assert.Contains("20261105102022_AddApiAuthenticationSessions", await db.Database.GetAppliedMigrationsAsync());
            Assert.Empty(await db.ApiSessions.ToListAsync());
            Assert.Empty(await db.ApiRefreshTokens.ToListAsync());
            Assert.Contains("20261106062559_AddCommerceOperationAudit", await db.Database.GetAppliedMigrationsAsync());
            Assert.Empty(await db.CommerceAuditEvents.ToListAsync());
        }
        finally
        {
            await db.Database.CloseConnectionAsync();
            if (created && connection.InitialCatalog.StartsWith("BookShoppingCartTests_", StringComparison.Ordinal))
                await db.Database.EnsureDeletedAsync();
        }
    }
}
