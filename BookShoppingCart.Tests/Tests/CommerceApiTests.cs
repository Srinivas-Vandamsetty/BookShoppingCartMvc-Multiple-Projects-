using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using BookShoppingCart.Data.Data;
using BookShoppingCart.Models.Models;
using BookShoppingCart.Models.Models.DTOs;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.Extensions.DependencyInjection;

namespace BookShoppingCart.Tests.Tests;

public sealed class CommerceApiTests
{
    internal static async Task Authorize(HttpClient client, string email = "reader@example.test", string password = AuthApiFactory.Password)
    {
        using var login = await AuthApiFactory.Login(client, email, password);
        login.EnsureSuccessStatusCode();
        var pair = (await login.Content.ReadFromJsonAsync<AuthTokens>())!;
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", pair.AccessToken);
    }

    internal static async Task<(int Genre, int Book)> SeedBook(IServiceProvider services, int quantity = 10)
    {
        using var scope = services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        var genre = new Genre { GenreName = "Test fiction" };
        var book = new Book { Genre = genre, BookName = "Paper Lanterns", AuthorName = "J. Writer", Price = 100,
            Stock = new Stock { Quantity = quantity } };
        db.Books.Add(book);
        if (!await db.OrderStatuses.AnyAsync())
            db.OrderStatuses.AddRange(new[] { "Pending", "Shipped", "Delivered", "Cancelled" }.Select((name, i) =>
                new OrderStatus { StatusId = i + 1, StatusName = name }));
        await db.SaveChangesAsync();
        return (genre.Id, book.Id);
    }

    internal static CheckoutModel Checkout(CheckoutQuoteResponse quote, string? coupon = null) => new()
    {
        Name = "Test Reader", Email = "reader@example.test", MobileNumber = "9876543210", Address = "42 Example Road",
        PaymentMethod = "cash", CouponCode = coupon, CheckoutKey = quote.CheckoutKey, QuoteToken = quote.QuoteToken,
        TotalAmount = 0.01m, ExpectedCartFingerprint = "forged"
    };

    internal static async Task<T> Read<T>(HttpResponseMessage response)
    {
        using (response)
        {
            Assert.True(response.IsSuccessStatusCode, $"{response.StatusCode}: {await response.Content.ReadAsStringAsync()}");
            return (await response.Content.ReadFromJsonAsync<T>())!;
        }
    }

    [Theory]
    [InlineData("/api/cart")]
    [InlineData("/api/orders")]
    [InlineData("/api/customers/me")]
    [InlineData("/api/admin/inventory")]
    [InlineData("/api/admin/orders")]
    public async Task CommerceRoutes_RequireBearer(string path)
    {
        await using var app = new AuthApiFactory();
        using var client = app.Client(); using var response = await client.GetAsync(path);
        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task Books_AdminCrud_Availability_AndArchiving()
    {
        await using var app = new AuthApiFactory(); await app.SeedUser(role: "Admin");
        var (genre, original) = await SeedBook(app.Services);
        using var anonymous = app.Client();
        Assert.Equal(10, (await anonymous.GetFromJsonAsync<AvailabilityResponse>($"/api/books/{original}/availability"))!.AvailableQuantity);
        Assert.Equal("Paper Lanterns", (await anonymous.GetFromJsonAsync<BookResponse>($"/api/books/{original}"))!.BookName);
        using var admin = app.Client(); await Authorize(admin);
        var request = new BookWriteRequest { BookName = "Another Book", AuthorName = "A. Author", Price = 29.99m, GenreId = genre };
        var created = await Read<BookResponse>(await admin.PostAsJsonAsync("/api/books", request));
        Assert.Equal(29.99m, created.Price);
        request.Price = 33.50m;
        Assert.Equal(33.50m, (await Read<BookResponse>(await admin.PutAsJsonAsync($"/api/books/{created.Id}", request))).Price);
        request.Image = "../unsafe.png";
        using var rejected = await admin.PutAsJsonAsync($"/api/books/{created.Id}", request);
        Assert.Equal(HttpStatusCode.BadRequest, rejected.StatusCode);
        using var archived = await admin.DeleteAsync($"/api/books/{created.Id}");
        Assert.Equal(HttpStatusCode.NoContent, archived.StatusCode);
        using var hidden = await anonymous.GetAsync($"/api/books/{created.Id}");
        Assert.Equal(HttpStatusCode.NotFound, hidden.StatusCode);
        var page = (await admin.GetFromJsonAsync<CommercePage<BookResponse>>("/api/admin/books"))!;
        Assert.Single(page.Items); Assert.False(page.HasNext);
        var methods = (await anonymous.GetFromJsonAsync<List<PaymentMethodResponse>>("/api/payment-methods"))!;
        Assert.Equal("cash", Assert.Single(methods).Code);
    }

    [Fact]
    public async Task Checkout_UsesSignedOwnerBoundQuote_RechecksCart_AndReplaysWithoutDuplicateOrder()
    {
        await using var app = new AuthApiFactory(); await app.SeedUser(); await app.SeedUser("other@example.test");
        var (_, book) = await SeedBook(app.Services);
        using var buyer = app.Client(); await Authorize(buyer);
        using var other = app.Client(); await Authorize(other, "other@example.test");
        using var forbidden = await buyer.GetAsync("/api/admin/inventory");
        Assert.Equal(HttpStatusCode.Forbidden, forbidden.StatusCode);
        Assert.Equal(2, await Read<int>(await buyer.PostAsJsonAsync("/api/cart/items", new CartItemRequest { BookId = book, Quantity = 2 })));
        var quote = await Read<CheckoutQuoteResponse>(await buyer.PostAsJsonAsync("/api/cart/quote", new QuoteRequest { CouponCode = "SAVE10" }));
        Assert.Equal(230m, quote.Total);
        Assert.InRange(quote.ExpiresAtUtc, DateTime.UtcNow.AddMinutes(14), DateTime.UtcNow.AddMinutes(16));
        using var stolen = await other.PostAsJsonAsync("/api/cart/checkout", Checkout(quote, "SAVE10"));
        Assert.Equal(HttpStatusCode.BadRequest, stolen.StatusCode);
        Assert.Empty((await other.GetFromJsonAsync<CartResponse>("/api/cart"))!.Items);
        Assert.Equal(3, await Read<int>(await buyer.PutAsJsonAsync($"/api/cart/items/{book}", new CartQuantityRequest { Quantity = 3 })));
        using var stale = await buyer.PostAsJsonAsync("/api/cart/checkout", Checkout(quote, "SAVE10"));
        Assert.Equal(HttpStatusCode.Conflict, stale.StatusCode);
        using var deleted = await buyer.DeleteAsync($"/api/cart/items/{book}");
        Assert.Equal(HttpStatusCode.NoContent, deleted.StatusCode);
        Assert.Empty((await buyer.GetFromJsonAsync<CartResponse>("/api/cart"))!.Items);
        await Read<int>(await buyer.PostAsJsonAsync("/api/cart/items", new CartItemRequest { BookId = book, Quantity = 2 }));
        quote = await Read<CheckoutQuoteResponse>(await buyer.PostAsJsonAsync("/api/cart/quote", new QuoteRequest { CouponCode = "SAVE10" }));
        var first = await Read<CheckoutResponse>(await buyer.PostAsJsonAsync("/api/cart/checkout", Checkout(quote, "SAVE10")));
        var replay = await Read<CheckoutResponse>(await buyer.PostAsJsonAsync("/api/cart/checkout", Checkout(quote, "SAVE10")));
        Assert.Equal(first, replay); Assert.Equal(230m, first.Total);
        var orders = (await buyer.GetFromJsonAsync<CommercePage<OrderResponse>>("/api/orders"))!;
        Assert.Equal(first.OrderId, Assert.Single(orders.Items).Id);
        Assert.Equal(2, Assert.Single((await buyer.GetFromJsonAsync<OrderResponse>($"/api/orders/{first.OrderId}"))!.Items).Quantity);
        using var denied = await other.GetAsync($"/api/orders/{first.OrderId}");
        Assert.Equal(HttpStatusCode.NotFound, denied.StatusCode);
        Assert.Equal(8, (await buyer.GetFromJsonAsync<AvailabilityResponse>($"/api/books/{book}/availability"))!.AvailableQuantity);
    }

    [Fact]
    public async Task AuditedOperations_RejectStaleWrites_ReplayAndRestoreStockOnCancellation()
    {
        await using var app = new AuthApiFactory(); var actor = await app.SeedUser(role: "Admin");
        var (_, book) = await SeedBook(app.Services, 4);
        using var client = app.Client(); await Authorize(client);
        var adjustment = new InventoryAdjustmentRequest { OperationId = Guid.NewGuid(), ExpectedQuantity = 4, Delta = 2, Reason = "Received stock" };
        var path = $"/api/admin/inventory/{book}/adjustments";
        using var added = await client.PostAsJsonAsync(path, adjustment); Assert.Equal(HttpStatusCode.NoContent, added.StatusCode);
        using var replayed = await client.PostAsJsonAsync(path, adjustment); Assert.Equal(HttpStatusCode.NoContent, replayed.StatusCode);
        adjustment.OperationId = Guid.NewGuid();
        using var stale = await client.PostAsJsonAsync(path, adjustment); Assert.Equal(HttpStatusCode.Conflict, stale.StatusCode);
        Assert.Empty((await client.GetFromJsonAsync<CommercePage<StockDisplayModel>>("/api/admin/inventory?lowStock=5"))!.Items);
        var inventory = (await client.GetFromJsonAsync<CommercePage<StockDisplayModel>>("/api/admin/inventory?search=Lantern"))!;
        Assert.Equal(6, Assert.Single(inventory.Items).Quantity);
        await Read<int>(await client.PostAsJsonAsync("/api/cart/items", new CartItemRequest { BookId = book, Quantity = 2 }));
        var quote = await Read<CheckoutQuoteResponse>(await client.PostAsJsonAsync("/api/cart/quote", new QuoteRequest()));
        var order = await Read<CheckoutResponse>(await client.PostAsJsonAsync("/api/cart/checkout", Checkout(quote)));
        var status = $"/api/admin/orders/{order.OrderId}/status";
        var statuses = (await client.GetFromJsonAsync<List<OrderStatus>>("/api/admin/order-statuses"))!;
        int Status(string name) => statuses.Single(s => s.StatusName == name).Id;
        var request = new OrderStatusRequest { OperationId = Guid.NewGuid(), StatusId = Status("Delivered"),
            ExpectedStatusId = Status("Pending"), Reason = "Fulfillment" };
        using var jump = await client.PatchAsJsonAsync(status, request); Assert.Equal(HttpStatusCode.Conflict, jump.StatusCode);
        var cash = new CashCollectionRequest { OperationId = Guid.NewGuid(), Collected = true, ExpectedCollected = false, Reason = "Receipt 12" };
        var cashPath = $"/api/admin/orders/{order.OrderId}/cash-collection";
        using var paid = await client.PutAsJsonAsync(cashPath, cash); Assert.Equal(HttpStatusCode.NoContent, paid.StatusCode);
        using var paidAgain = await client.PutAsJsonAsync(cashPath, cash); Assert.Equal(HttpStatusCode.NoContent, paidAgain.StatusCode);
        request.StatusId = Status("Cancelled");
        using var paidCancel = await client.PatchAsJsonAsync(status, request); Assert.Equal(HttpStatusCode.Conflict, paidCancel.StatusCode);
        cash = new CashCollectionRequest { OperationId = Guid.NewGuid(), ExpectedCollected = true, Reason = "Corrected receipt" };
        using var corrected = await client.PutAsJsonAsync(cashPath, cash); Assert.Equal(HttpStatusCode.NoContent, corrected.StatusCode);
        using var cancelled = await client.PatchAsJsonAsync(status, request); Assert.Equal(HttpStatusCode.NoContent, cancelled.StatusCode);
        using var cancelledAgain = await client.PatchAsJsonAsync(status, request); Assert.Equal(HttpStatusCode.NoContent, cancelledAgain.StatusCode);
        Assert.Equal(6, (await client.GetFromJsonAsync<AvailabilityResponse>($"/api/books/{book}/availability"))!.AvailableQuantity);
        var adminOrders = (await client.GetFromJsonAsync<CommercePage<OrderResponse>>("/api/admin/orders?status=Cancelled&search=Reader"))!;
        Assert.Equal(order.OrderId, Assert.Single(adminOrders.Items).Id);
        using var scope = app.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        var audit = await db.CommerceAuditEvents.ToListAsync();
        Assert.Equal(4, audit.Count); Assert.All(audit, e => Assert.Equal(actor, e.ActorId));
        Assert.Contains(audit, e => e.Kind == "inventory" && e.Before == "4" && e.After == "6");
    }

    [Fact]
    public async Task InvalidOrExpiredCheckoutQuote_IsRejectedBeforePlacingOrder()
    {
        await using var app = new AuthApiFactory(); var owner = await app.SeedUser();
        using var client = app.Client(); await Authorize(client);
        var key = Guid.NewGuid();
        var protector = app.Services.GetRequiredService<IDataProtectionProvider>()
            .CreateProtector("BookVerse.ApiCheckoutQuote.v1").ToTimeLimitedDataProtector();
        var expired = protector.Protect(System.Text.Json.JsonSerializer.Serialize(new
        { Owner = owner, Key = key, Coupon = "", Fingerprint = "old" }), DateTimeOffset.UtcNow.AddMinutes(-1));
        foreach (var token in new[] { "not-valid-base64!", expired })
        {
            using var rejected = await client.PostAsJsonAsync("/api/cart/checkout", Checkout(new CheckoutQuoteResponse(
                key, token, DateTime.UtcNow.AddMinutes(-1), 0m, 0m, 0m, 0m)));
            Assert.Equal(HttpStatusCode.BadRequest, rejected.StatusCode);
        }
        Assert.Empty((await client.GetFromJsonAsync<CommercePage<OrderResponse>>("/api/orders"))!.Items);
    }

    [Fact]
    public async Task CustomerProfile_UpdatesOnlyContactAndKeepsSessionActive()
    {
        await using var app = new AuthApiFactory(); await app.SeedUser();
        using var client = app.Client(); await Authorize(client);
        using var invalid = await client.PatchAsJsonAsync("/api/customers/me", new CustomerProfileRequest { PhoneNumber = "bad" });
        Assert.Equal(HttpStatusCode.BadRequest, invalid.StatusCode);
        var updated = await Read<CustomerProfileResponse>(await client.PatchAsJsonAsync("/api/customers/me",
            new { phoneNumber = "9876543210", email = "forged@example.test", emailConfirmed = false }));
        Assert.Equal("reader@example.test", updated.Email);
        Assert.True(updated.EmailConfirmed);
        Assert.Equal("9876543210", (await client.GetFromJsonAsync<CustomerProfileResponse>("/api/customers/me"))!.PhoneNumber);
    }
}
