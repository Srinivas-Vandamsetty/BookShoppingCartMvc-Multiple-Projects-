using System.ComponentModel.DataAnnotations;
namespace BookShoppingCart.Models.Models.DTOs;

public sealed record BookResponse(int Id, string BookName, string AuthorName, decimal Price, int GenreId,
    string GenreName, string Image, int Quantity, bool IsArchived);
public sealed class BookWriteRequest
{
    [Required, StringLength(40)] public string BookName { get; set; } = "";
    [Required, StringLength(40)] public string AuthorName { get; set; } = "";
    [Range(typeof(decimal), "0.01", "999999999.99")] public decimal Price { get; set; }
    [Range(1, int.MaxValue)] public int GenreId { get; set; }
    [StringLength(200)] public string Image { get; set; } = "";
}
public sealed record CartLineResponse(int BookId, int Quantity, BookResponse Book);
public sealed record CartResponse(List<CartLineResponse> Items, int ItemCount, decimal Subtotal, decimal Discount, decimal Shipping, decimal Total);
public sealed class CartItemRequest
{
    [Range(1, int.MaxValue)] public int BookId { get; set; }
    [Range(1,1000)] public int Quantity { get; set; } = 1;
}
public sealed class CartQuantityRequest { [Range(1,1000)] public int Quantity { get; set; } }
public sealed class QuoteRequest { [StringLength(20)] public string? CouponCode { get; set; } }
public sealed record CheckoutQuoteResponse(Guid CheckoutKey, string QuoteToken, DateTime ExpiresAtUtc,
    decimal Subtotal, decimal Discount, decimal Shipping, decimal Total);
public sealed record CheckoutResponse(int OrderId, decimal Total);
public sealed record OrderLineResponse(int BookId, string BookName, string AuthorName, string GenreName, int Quantity, decimal UnitPrice, string Image);
public sealed record OrderResponse(int Id, DateTime CreateDate, int OrderStatusId, string Status,
    decimal Subtotal, decimal Discount, decimal Shipping, decimal Total, string? CouponCode,
    string Name, string Email, string MobileNumber, string Address, string PaymentMethod, bool IsPaid, List<OrderLineResponse> Items);
public sealed class InventoryAdjustmentRequest
{
    public Guid OperationId { get; set; }
    [Range(0,int.MaxValue)] public int ExpectedQuantity { get; set; }
    public int Delta { get; set; }
    [Required, StringLength(300)] public string Reason { get; set; } = "";
}
public sealed class CashCollectionRequest
{
    public Guid OperationId { get; set; }
    public bool Collected { get; set; }
    public bool ExpectedCollected { get; set; }
    [Required, StringLength(300)] public string Reason { get; set; } = "";
}
public sealed class OrderStatusRequest
{
    public Guid OperationId { get; set; }
    [Range(1,int.MaxValue)] public int StatusId { get; set; }
    [Range(1,int.MaxValue)] public int ExpectedStatusId { get; set; }
    [Required, StringLength(300)] public string Reason { get; set; } = "";
}
public sealed record CustomerProfileResponse(string Id, string UserName, string Email, string? PhoneNumber, bool EmailConfirmed);
public sealed class CustomerProfileRequest { [Phone, StringLength(30)] public string? PhoneNumber { get; set; } }
public sealed record AvailabilityResponse(int BookId, int AvailableQuantity, int MaxPurchaseQuantity, bool InStock);
public sealed record PaymentMethodResponse(string Code, string Name);

public sealed record CommercePage<T>(List<T> Items, int Page, bool HasNext)
{
    public static CommercePage<T> From(IEnumerable<T> source, int page)
    {
        var rows = source.Take(51).ToList();
        return new(rows.Take(50).ToList(), page, rows.Count > 50);
    }
}
