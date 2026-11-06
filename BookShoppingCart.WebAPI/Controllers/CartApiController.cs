using System.Security.Cryptography;
using System.Text.Json;
using BookShoppingCart.Business.Services;
using BookShoppingCart.Data.Repositories;
using BookShoppingCart.Models.Models;
using BookShoppingCart.Models.Models.DTOs;
using BookStoreCore.Pricing;
using BookStoreCore.Security;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.AspNetCore.Mvc;

namespace BookShoppingCart.WebAPI.Controllers;

[ApiController, Authorize, Route("api/cart"), ResponseCache(NoStore = true, Location = ResponseCacheLocation.None)]
public sealed class CartApiController(ICartService cart, CartRepository repository, CommerceOperations operations,
    ICurrentUser user, IDataProtectionProvider protection) : ControllerBase
{
    private readonly ITimeLimitedDataProtector _quotes = protection.CreateProtector("BookVerse.ApiCheckoutQuote.v1").ToTimeLimitedDataProtector();
    private sealed record QuoteData(string Owner, Guid Key, string Coupon, string Fingerprint);

    [HttpGet]
    public async Task<CartResponse> Get([FromQuery, System.ComponentModel.DataAnnotations.StringLength(20)] string? coupon = null)
    {
        var current = await cart.GetUserCart();
        var quote = Price(current, coupon);
        return new(current.CartDetails.Select(d => new CartLineResponse(d.BookId, d.Quantity, d.Book.ToResponse())).ToList(),
            current.CartDetails.Sum(d => d.Quantity), quote.Subtotal, quote.Discount, quote.Shipping, quote.Total);
    }

    [HttpPost("items")]
    public Task<int> Add(CartItemRequest request) => cart.AddItem(request.BookId, request.Quantity);
    [HttpPut("items/{bookId:int}")]
    public Task<int> Set(int bookId, CartQuantityRequest request) => repository.SetQuantity(bookId, request.Quantity);
    [HttpDelete("items/{bookId:int}")]
    public async Task<IActionResult> Remove(int bookId) { await repository.SetQuantity(bookId, 0); return NoContent(); }
    [HttpPost("items/{bookId:int}/decrement")]
    public Task<int> Decrement(int bookId) => cart.RemoveItem(bookId);

    [HttpPost("quote")]
    public async Task<CheckoutQuoteResponse> Quote(QuoteRequest request)
    {
        var current = await cart.GetUserCart();
        if (current.CartDetails.Count == 0) throw new InvalidOperationException("Your cart is empty.");
        if (current.CartDetails.Any(d => d.Book.IsArchived || d.Quantity > (d.Book.Stock?.Quantity ?? 0)))
            throw new InvalidOperationException("Some cart quantities are no longer available. Review your cart.");
        var price = Price(current, request.CouponCode);
        var key = Guid.NewGuid();
        var expires = DateTimeOffset.UtcNow.AddMinutes(15);
        var token = _quotes.Protect(JsonSerializer.Serialize(new QuoteData(user.UserId, key, Coupon(request.CouponCode),
            CartPricing.Fingerprint(current.CartDetails.Select(d => (d.BookId, d.Quantity, d.Book.Price))))), expires);
        return new(key, token, expires.UtcDateTime, price.Subtotal, price.Discount, price.Shipping, price.Total);
    }

    [HttpPost("checkout"), RequestSizeLimit(16384)]
    public async Task<ActionResult<CheckoutResponse>> Checkout(CheckoutModel request, CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(request.QuoteToken) || request.QuoteToken.Length > 4096)
            throw new ArgumentException("A valid checkout quote is required.");
        QuoteData? quote;
        try { quote = JsonSerializer.Deserialize<QuoteData>(_quotes.Unprotect(request.QuoteToken)); }
        catch (Exception ex) when (ex is CryptographicException or JsonException or FormatException)
        { throw new ArgumentException("Your checkout quote expired or is invalid. Review your cart again."); }
        if (quote is null || quote.Owner != user.UserId || quote.Key != request.CheckoutKey || quote.Coupon != Coupon(request.CouponCode))
            throw new ArgumentException("Your checkout quote does not match this request.");
        request.ExpectedCartFingerprint = quote.Fingerprint;
        await cart.DoCheckout(request);
        var order = await operations.FindCheckout(request.CheckoutKey, ct);
        return CreatedAtAction(nameof(OrdersController.Get), "Orders", new { orderId = order.Id }, new CheckoutResponse(order.Id, order.Total));
    }

    private static string Coupon(string? code) => code?.Trim().ToUpperInvariant() ?? "";
    private static CartQuote Price(ShoppingCart current, string? coupon) => CartPricing.Quote(
        current.CartDetails.Sum(d => d.Book.Price * d.Quantity), coupon, current.CartDetails.Count > 0);
}
