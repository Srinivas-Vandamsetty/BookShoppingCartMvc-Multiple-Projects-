using BookShoppingCart.Models.Models.DTOs;

namespace BookShoppingCart.Models.Models;

/// <summary>
/// Payment methods supported by the checkout boundary. Card and UPI are sandbox authorizations:
/// no card number, CVV, PIN, or UPI credential is collected or stored by this application.
/// </summary>
public static class CheckoutPaymentMethods
{
    public const string Cash = "cash";
    public const string CreditCard = "creditcard";
    public const string Upi = "upi";

    public static readonly PaymentMethodResponse[] Available =
    [
        new(Cash, "Cash on Delivery (COD)"),
        new(CreditCard, "Credit / Debit Card"),
        new(Upi, "UPI")
    ];

    public static string Normalize(string? paymentMethod) => paymentMethod?.Trim().ToLowerInvariant() switch
    {
        Cash => Cash,
        CreditCard => CreditCard,
        Upi => Upi,
        _ => throw new NotSupportedException("Choose Cash on Delivery, Credit / Debit Card, or UPI.")
    };

    public static PaymentResult AuthorizeSandbox(CheckoutModel checkout, decimal amount)
    {
        if (amount <= 0) throw new ArgumentOutOfRangeException(nameof(amount));
        var method = Normalize(checkout.PaymentMethod);
        if (method == Cash)
            return new PaymentResult { IsSuccess = true, Message = "Cash on delivery selected; payment will be collected on delivery." };

        if (method == CreditCard)
        {
            var card = new string((checkout.CardNumber ?? "").Where(char.IsDigit).ToArray());
            if (string.IsNullOrWhiteSpace(checkout.CardholderName) || !System.Text.RegularExpressions.Regex.IsMatch(checkout.CardExpiry ?? "", @"^(0[1-9]|1[0-2])\/\d{2}$") || !System.Text.RegularExpressions.Regex.IsMatch(checkout.CardCvv ?? "", @"^\d{3,4}$"))
                throw new ArgumentException("Enter a cardholder name, expiry in MM/YY format, and a 3 or 4 digit CVV.");
            return card switch
            {
                "4111111111111111" => new PaymentResult { IsSuccess = true, TransactionId = $"CARD-{checkout.CheckoutKey:N}".ToUpperInvariant(), Message = "Card sandbox payment authorized." },
                "4000000000000002" => new PaymentResult { IsSuccess = false, Message = "Card payment was declined by the sandbox. Try the success test card." },
                _ => throw new ArgumentException("Use test card 4111 1111 1111 1111 for success or 4000 0000 0000 0002 to test a decline.")
            };
        }
        var upi = checkout.UpiId?.Trim().ToLowerInvariant();
        if (string.IsNullOrWhiteSpace(upi) || !System.Text.RegularExpressions.Regex.IsMatch(upi, @"^[a-z0-9._-]{2,}@[a-z][a-z0-9.-]{1,}$")) throw new ArgumentException("Enter a valid test UPI ID, for example success@upi.");
        if (upi == "failure@upi") return new PaymentResult { IsSuccess = false, Message = "UPI sandbox payment failed. Try success@upi." };
        if (upi != "success@upi") throw new ArgumentException("Use success@upi for payment success or failure@upi to test a failed payment.");
        return new PaymentResult
        {
            IsSuccess = true,
            TransactionId = $"UPI-{checkout.CheckoutKey:N}".ToUpperInvariant(), Message = "UPI sandbox payment authorized."
        };
    }
}
