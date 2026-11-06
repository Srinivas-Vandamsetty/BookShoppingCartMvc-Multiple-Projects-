using BookShoppingCart.Business.Services;
using BookShoppingCart.Data.Data;
using BookShoppingCart.Data.Repositories;
using BookShoppingCart.Models.Models;
using BookShoppingCart.Models.Models.DTOs;
using BookStoreCore.Pricing;
using BookStoreCore.Security;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using BookStoreCore.Shared;

namespace BookShoppingCart.Tests.Tests;

public sealed class TestUser(string id, bool admin = false) : ICurrentUser
{
    public string UserId => id;
    public bool IsAdmin => admin;
}

public sealed class CartPersistenceTests : IAsyncLifetime
{
    private readonly SqliteConnection _connection = new("Data Source=:memory:");
    private ApplicationDbContext _db = null!;
    private CartRepository _cart = null!;
    public async Task InitializeAsync()
    {
        await _connection.OpenAsync();
        _connection.CreateCollation("SQL_Latin1_General_CP1_CI_AS", (a,b) => StringComparer.OrdinalIgnoreCase.Compare(a,b));
        _db = new ApplicationDbContext(new DbContextOptionsBuilder<ApplicationDbContext>().UseSqlite(_connection).Options);
        await _db.Database.EnsureCreatedAsync();
        var genre = new Genre { GenreName = "Fiction" };
        _db.Genres.Add(genre);
        _db.OrderStatuses.Add(new OrderStatus { StatusId = 1, StatusName = "Pending" });
        _db.Books.Add(new Book { Id = 1, BookName = "Book", AuthorName = "Author", Genre = genre, Price = 100m,
            Stock = new Stock { Quantity = 5 } });
        await _db.SaveChangesAsync();
        _db.ChangeTracker.Clear();
        _cart = new CartRepository(_db, new TestUser("user-one"));
    }
    public async Task DisposeAsync() { await _db.DisposeAsync(); await _connection.DisposeAsync(); }
    private static CheckoutModel Checkout(Guid? key = null) => new()
    {
        Name = "Reader", Email = "reader@example.com", MobileNumber = "9876543210", Address = "Test address",
        PaymentMethod = "cash", CouponCode = "SAVE10", CheckoutKey = key ?? Guid.NewGuid(),
        TotalAmount = .01m // Forged value must never be used.
    };
    [Fact] public async Task FirstAdd_PersistsCart_IncrementAndLastRemove_Work()
    {
        Assert.Equal(1, await _cart.AddItem(1,1));
        Assert.Single(await _db.ShoppingCarts.ToListAsync());
        Assert.Equal(2, await _cart.AddItem(1,1));
        Assert.Equal(1, await _cart.RemoveItem(1));
        Assert.Equal(0, await _cart.RemoveItem(1));
        Assert.Empty(await _db.CartDetails.ToListAsync());
    }
    [Theory] [InlineData(0)] [InlineData(-1)] [InlineData(int.MaxValue)] [InlineData(1001)]
    public async Task InvalidQuantities_NeverPersist(int quantity)
    {
        await Assert.ThrowsAsync<ArgumentException>(() => _cart.AddItem(1,quantity));
        Assert.Empty(await _db.CartDetails.ToListAsync());
    }
    [Fact] public async Task MissingStock_AndOverstock_AreRejected()
    {
        await Assert.ThrowsAsync<InvalidOperationException>(() => _cart.AddItem(1,6));
        _db.ChangeTracker.Clear();
        await _db.Stocks.ExecuteDeleteAsync();
        await Assert.ThrowsAsync<InvalidOperationException>(() => _cart.AddItem(1,1));
    }
    [Fact] public async Task Checkout_UsesServerPriceDiscountShipping_AndReplaysOnce()
    {
        await _cart.AddItem(1,2);
        var request = Checkout();
        Assert.True(await _cart.DoCheckout(request));
        _db.ChangeTracker.Clear();
        Assert.True(await _cart.DoCheckout(request));
        var order = await _db.Orders.Include(o => o.OrderDetail).SingleAsync();
        Assert.Equal(200m, order.Subtotal);
        Assert.Equal(20m, order.Discount);
        Assert.Equal(50m, order.Shipping);
        Assert.Equal(230m, order.Total);
        Assert.False(order.IsPaid);
        Assert.Equal(100m, order.OrderDetail.Single().UnitPrice);
        Assert.Equal("Book", order.OrderDetail.Single().BookName);
        Assert.Equal(3, await _db.Stocks.Select(s => s.Quantity).SingleAsync());
        Assert.Empty(await _db.CartDetails.ToListAsync());
    }
    [Fact] public async Task FailedCheckout_RollsBackOrderStockAndCart()
    {
        await _cart.AddItem(1,2);
        await _db.Stocks.ExecuteUpdateAsync(s => s.SetProperty(x => x.Quantity, 1));
        await Assert.ThrowsAsync<InvalidOperationException>(() => _cart.DoCheckout(Checkout()));
        _db.ChangeTracker.Clear();
        Assert.Empty(await _db.Orders.ToListAsync());
        Assert.Equal(2, await _db.CartDetails.Select(d => d.Quantity).SingleAsync());
        Assert.Equal(1, await _db.Stocks.Select(d => d.Quantity).SingleAsync());
    }
    [Theory] [InlineData("creditcard")] [InlineData("upi")] [InlineData("invalid")]
    public async Task ElectronicAndUnknownPayment_NeverCreateOrder(string method)
    {
        await _cart.AddItem(1,1);
        var request = Checkout(); request.PaymentMethod = method;
        await Assert.ThrowsAsync<NotSupportedException>(() => _cart.DoCheckout(request));
        Assert.Empty(await _db.Orders.ToListAsync());
        Assert.Equal(5, await _db.Stocks.Select(s => s.Quantity).SingleAsync());
    }
    [Fact] public async Task ArchivedBook_CannotBeAdded_AndHistoryRemains()
    {
        await _cart.AddItem(1,1);
        await _cart.DoCheckout(Checkout());
        _db.ChangeTracker.Clear();
        var service = new BookService(new BookRepository(_db), Mock.Of<IFileService>(), NullLogger<BookService>.Instance);
        await service.DeleteBook(1);
        _db.ChangeTracker.Clear();
        Assert.Single(await _db.OrderDetails.ToListAsync());
        Assert.True(await _db.Books.Select(b => b.IsArchived).SingleAsync());
        await Assert.ThrowsAsync<KeyNotFoundException>(() => _cart.AddItem(1,1));
        Assert.Empty(await new HomeRepository(_db).GetBooks());
    }
    [Fact] public async Task PriceChange_UsesCurrentPriceAndHistoricalSnapshot()
    {
        await _cart.AddItem(1,1);
        await _db.Books.ExecuteUpdateAsync(b => b.SetProperty(x => x.Price, 120m));
        await _cart.DoCheckout(Checkout());
        await _db.Books.ExecuteUpdateAsync(b => b.SetProperty(x => x.Price, 150m));
        Assert.Equal(120m, await _db.OrderDetails.Select(d => d.UnitPrice).SingleAsync());
    }
    [Fact] public async Task DatabaseUniquenessAndQuantityConstraints_AreEnforced()
    {
        await _cart.AddItem(1,1);
        await Assert.ThrowsAsync<Microsoft.Data.Sqlite.SqliteException>(() =>
            _db.Database.ExecuteSqlRawAsync("UPDATE CartDetail SET Quantity=-1"));
        _db.ShoppingCarts.Add(new ShoppingCart { UserId = "user-one" });
        await Assert.ThrowsAsync<DbUpdateException>(() => _db.SaveChangesAsync());
    }
    [Fact] public async Task OwnershipBoundary_RejectsAnotherUsersCartAndOrders()
    {
        await _cart.AddItem(1,1);
        await Assert.ThrowsAsync<UnauthorizedAccessException>(() => _cart.GetCart("other-user"));
        await Assert.ThrowsAsync<UnauthorizedAccessException>(() => new UserOrderRepository(_db, new TestUser("user-one")).UserOrders(true));
    }
    [Fact] public async Task Catalog_PagesAtDatabase_AndExcludesArchives()
    {
        var genreId = await _db.Genres.Select(g => g.Id).SingleAsync();
        _db.Books.AddRange(Enumerable.Range(2,24).Select(i => new Book { Id=i, BookName="Book "+i, AuthorName="Author", GenreId=genreId, Price=10m }));
        await _db.SaveChangesAsync();
        var result = await new HomeRepository(_db).SearchBooks(page:2, pageSize:12);
        Assert.Equal(25,result.TotalCount);
        Assert.Equal(12,result.Items.Count);
        Assert.Equal(13,result.Items[0].Id);
        Assert.Equal(genreId,result.Items[0].GenreId);
        Assert.All(await new HomeRepository(_db).GetBooks(), b=>Assert.Equal(genreId,b.GenreId));
    }
    [Theory] [InlineData(999,0)] [InlineData(998,50)] [InlineData(1000,0)]
    public void ShippingThreshold_IsConsistent(int total, int shipping) =>
        Assert.Equal((decimal)shipping, CartPricing.Quote(total, null).Shipping);
    [Fact] public void Discount_IsRoundedAndInvalidCouponsRejected()
    {
        Assert.Equal(.33m, CartPricing.Quote(3.33m,"save10").Discount);
        Assert.Equal(0, CartPricing.Quote(0,null,false).Total);
        Assert.Throws<ArgumentException>(()=>CartPricing.Quote(100,"UNKNOWN"));
    }

    [Fact] public async Task PriceChangedAfterSignedReview_DoesNotPlaceAnUnreviewedOrder()
    {
        await _cart.AddItem(1,1);
        var request = Checkout();
        request.ExpectedCartFingerprint = CartPricing.Fingerprint([(1,1,100m)]);
        await _db.Books.ExecuteUpdateAsync(b=>b.SetProperty(x=>x.Price,120m));
        await Assert.ThrowsAsync<InvalidOperationException>(()=>_cart.DoCheckout(request));
        Assert.Empty(await _db.Orders.ToListAsync());
        Assert.Equal(5,await _db.Stocks.Select(s=>s.Quantity).SingleAsync());
        Assert.Single(await _db.CartDetails.ToListAsync());
    }
}
