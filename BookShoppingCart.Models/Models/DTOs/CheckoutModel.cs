using System.ComponentModel.DataAnnotations;

namespace BookShoppingCart.Models.Models.DTOs;

public class CheckoutModel
{
    [Required]
    [MaxLength(30)]
    public string Name { get; set; } = string.Empty;

    [Required]
    [EmailAddress]
    [MaxLength(30)]
    public string Email { get; set; } = string.Empty;
    [Required]
    [Phone, MaxLength(30)]
    public string MobileNumber { get; set; } = string.Empty;
    [Required]
    [MaxLength(200)]
    public string Address { get; set; } = string.Empty;

    [Required]
    public string PaymentMethod { get; set; } = string.Empty;

    // Display only; posted totals are never used to place an order.
    [Microsoft.AspNetCore.Mvc.ModelBinding.BindNever]
    public decimal TotalAmount { get; set; }
    [MaxLength(20)] public string? CouponCode { get; set; }
    public Guid CheckoutKey { get; set; }
    public string QuoteToken { get; set; } = "";
    [Microsoft.AspNetCore.Mvc.ModelBinding.BindNever]
    public string? ExpectedCartFingerprint { get; set; }
}
