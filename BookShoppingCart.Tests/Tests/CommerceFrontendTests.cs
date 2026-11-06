using System.Net;
using System.Text.RegularExpressions;
using BookShoppingCart.Data.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace BookShoppingCart.Tests.Tests;

public sealed class CommerceFrontendTests
{
    private static string Field(string html, string name)
    {
        var match = Regex.Match(html, $"name=\"{Regex.Escape(name)}\"[^>]*value=\"([^\"]*)\"");
        Assert.True(match.Success, $"Missing form field: {name}");
        return WebUtility.HtmlDecode(match.Groups[1].Value);
    }
    private static Task<HttpResponseMessage> Post(HttpClient client, string path, string html, Dictionary<string,string> values)
    {
        values["__RequestVerificationToken"] = FrontendAuthHarness.Antiforgery(html);
        return client.PostAsync(path, new FormUrlEncodedContent(values));
    }
    private static async Task Redirect(HttpResponseMessage response)
    {
        Assert.True(response.StatusCode == HttpStatusCode.Redirect,
            $"Expected redirect, got {response.StatusCode}: {await response.Content.ReadAsStringAsync()}");
    }

    [SqlServerFact]
    public async Task Buyer_CanEditCart_Checkout_ViewOrder_AndUpdateProfileThroughBearerApi()
    {
        await using var app = new FrontendAuthHarness(); await app.InitializeAsync();
        var (_, book) = await CommerceApiTests.SeedBook(app.Api.Services);
        using var client = app.Client(); using var login = await FrontendAuthHarness.Login(client); await Redirect(login);
        var cart = await client.GetStringAsync("/Cart/GetUserCart");
        using var added = await Post(client, "/Cart/AddItem", cart, new() { ["bookId"] = book.ToString(), ["qty"] = "2" });
        Assert.Equal(HttpStatusCode.OK, added.StatusCode);
        cart = await client.GetStringAsync("/Cart/GetUserCart");
        Assert.Contains("Paper Lanterns", cart);
        using var removed = await Post(client, "/Cart/RemoveLine", cart, new() { ["bookId"] = book.ToString() });
        await Redirect(removed);
        cart = await client.GetStringAsync("/Cart/GetUserCart");
        Assert.Contains("Your cart is empty", cart);
        using var readded = await Post(client, "/Cart/AddItem", cart, new() { ["bookId"] = book.ToString(), ["qty"] = "2" });
        readded.EnsureSuccessStatusCode();
        cart = await client.GetStringAsync("/Cart/GetUserCart");
        using var exact = await Post(client, "/Cart/SetQuantity", cart, new() { ["bookId"] = book.ToString(), ["quantity"] = "3" });
        await Redirect(exact);
        cart = await client.GetStringAsync("/Cart/GetUserCart");
        using var minus = await Post(client, "/Cart/RemoveItem", cart, new() { ["bookId"] = book.ToString() });
        await Redirect(minus);
        var review = await client.GetStringAsync("/Cart/Checkout?coupon=SAVE10");
        Assert.Contains("Cash on delivery", review);
        Assert.Contains("230.00", review);
        using var placed = await Post(client, "/Cart/Checkout", review, new()
        {
            ["Name"] = "UI Reader", ["Email"] = FrontendAuthHarness.Email, ["MobileNumber"] = "9876543210",
            ["Address"] = "42 Example Road", ["PaymentMethod"] = "cash", ["CouponCode"] = "SAVE10",
            ["CheckoutKey"] = Field(review, "CheckoutKey"), ["QuoteToken"] = Field(review, "QuoteToken")
        });
        await Redirect(placed);
        Assert.Contains("OrderSuccess", placed.Headers.Location!.ToString());
        using var scope = app.Api.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        var order = await db.Orders.SingleAsync();
        Assert.Equal(230m, order.Total);
        Assert.Contains("Paper Lanterns", await client.GetStringAsync("/UserOrder/UserOrders"));
        Assert.Contains("Order #" + order.Id, await client.GetStringAsync($"/UserOrder/Details/{order.Id}"));
        var profile = await client.GetStringAsync("/Identity/Account/Manage");
        using var saved = await Post(client, "/Identity/Account/Manage", profile, new() { ["Input.PhoneNumber"] = "9876543210" });
        await Redirect(saved);
        Assert.Contains("9876543210", await client.GetStringAsync("/Identity/Account/Manage"));
        foreach (var path in new[] { "/api/cart", "/api/cart/items", $"/api/cart/items/{book}", $"/api/cart/items/{book}/decrement",
            "/api/cart/quote", "/api/cart/checkout", "/api/payment-methods", "/api/orders", $"/api/orders/{order.Id}", "/api/customers/me" })
            Assert.Contains(app.Calls, call => call == (path, "Bearer"));
    }

    [SqlServerFact]
    public async Task Admin_CanManageBooksStockAndOrderFulfillmentThroughUi()
    {
        await using var app = new FrontendAuthHarness(); await app.InitializeAsync("Admin");
        var (genre, book) = await CommerceApiTests.SeedBook(app.Api.Services);
        using var client = app.Client(); using var login = await FrontendAuthHarness.Login(client); await Redirect(login);
        var create = await client.GetStringAsync("/Book/AddBook");
        using var added = await Post(client, "/Book/AddBook", create, new()
        { ["BookName"] = "UI Book", ["AuthorName"] = "A. Writer", ["GenreId"] = genre.ToString(), ["Price"] = "49.99" });
        await Redirect(added);
        using var scope = app.Api.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        var created = await db.Books.AsNoTracking().SingleAsync(b => b.BookName == "UI Book");
        var edit = await client.GetStringAsync($"/Book/UpdateBook/{created.Id}");
        using var changed = await Post(client, "/Book/UpdateBook", edit, new()
        { ["Id"] = created.Id.ToString(), ["BookName"] = "UI Book Revised", ["AuthorName"] = "A. Writer", ["GenreId"] = genre.ToString(), ["Price"] = "55.00" });
        await Redirect(changed);
        Assert.Contains("UI Book Revised", await client.GetStringAsync($"/Book/BookDetails/{created.Id}"));
        var books = await client.GetStringAsync("/Book/Index");
        using var archived = await Post(client, "/Book/DeleteBook", books, new() { ["id"] = created.Id.ToString() });
        await Redirect(archived);
        Assert.True(await db.Books.Where(b => b.Id == created.Id).Select(b => b.IsArchived).SingleAsync());
        var stock = await client.GetStringAsync($"/Stock/ManangeStock?bookId={book}");
        using var adjusted = await Post(client, "/Stock/ManangeStock", stock, new()
        {
            ["BookId"] = book.ToString(), ["Quantity"] = "12", ["ExpectedQuantity"] = Field(stock, "ExpectedQuantity"),
            ["OperationId"] = Field(stock, "OperationId"), ["Reason"] = "Received two copies"
        });
        await Redirect(adjusted);
        Assert.Equal(12, await db.Stocks.Where(s => s.BookId == book).Select(s => s.Quantity).SingleAsync());
        Assert.Contains("Paper Lanterns", await client.GetStringAsync("/Stock/Index"));
        var owner = await db.Users.Select(u => u.Id).SingleAsync();
        var pending = await db.OrderStatuses.SingleAsync(s => s.StatusName == "Pending");
        var shipped = await db.OrderStatuses.SingleAsync(s => s.StatusName == "Shipped");
        var order = new BookShoppingCart.Models.Models.Order { UserId = owner, Name = "UI Reader", Email = FrontendAuthHarness.Email,
            MobileNumber = "9876543210", Address = "42 Example Road", PaymentMethod = "cash", OrderStatusId = pending.Id, Total = 150m };
        db.Orders.Add(order); await db.SaveChangesAsync();
        var fulfillment = await client.GetStringAsync($"/AdminOperations/UpdateOrderStatus?orderId={order.Id}");
        using var dispatch = await Post(client, "/AdminOperations/UpdateOrderStatus", fulfillment, new()
        {
            ["OrderId"] = order.Id.ToString(), ["OrderStatusId"] = shipped.Id.ToString(), ["ExpectedStatusId"] = Field(fulfillment, "ExpectedStatusId"),
            ["OperationId"] = Field(fulfillment, "OperationId"), ["Reason"] = "Dispatched to carrier"
        });
        await Redirect(dispatch);
        var allOrders = await client.GetStringAsync("/AdminOperations/AllOrders?status=Shipped");
        Assert.Contains("UI Reader", allOrders);
        using var paid = await Post(client, "/AdminOperations/RecordCashCollection", allOrders, new()
        {
            ["orderId"] = order.Id.ToString(), ["OperationId"] = Field(allOrders, "OperationId"), ["Collected"] = Field(allOrders, "Collected"),
            ["ExpectedCollected"] = Field(allOrders, "ExpectedCollected"), ["Reason"] = "Cash receipt 12"
        });
        await Redirect(paid);
        var saved = await db.Orders.AsNoTracking().SingleAsync(o => o.Id == order.Id);
        Assert.True(saved.IsPaid); Assert.Equal(shipped.Id, saved.OrderStatusId);
        Assert.Equal(3, await db.CommerceAuditEvents.CountAsync());
        foreach (var path in new[] { "/api/books", "/api/admin/books", $"/api/books/{created.Id}", "/api/admin/inventory", $"/api/books/{book}/availability",
            $"/api/admin/inventory/{book}/adjustments", "/api/admin/orders", $"/api/admin/orders/{order.Id}/status", $"/api/admin/orders/{order.Id}/cash-collection" })
            Assert.Contains(app.Calls, call => call == (path, "Bearer"));
    }
}
