namespace BookStoreCore.Pricing;
public sealed record CartQuote(decimal Subtotal, decimal Discount, decimal Shipping, decimal Total);
public static class CartPricing
{
    public static string Fingerprint(IEnumerable<(int BookId, int Quantity, decimal Price)> lines) =>
        Convert.ToHexString(System.Security.Cryptography.SHA256.HashData(System.Text.Encoding.UTF8.GetBytes(
            string.Join("|", lines.OrderBy(l => l.BookId).Select(l =>
                FormattableString.Invariant($"{l.BookId}:{l.Quantity}:{l.Price}"))))));

    public const int MaxQuantity = 1000;
    public const decimal FreeShippingThreshold = 999m;
    public static decimal DiscountRate(string? coupon) => coupon?.Trim().ToUpperInvariant() switch
    {
        null or "" => 0m, "SAVE10" => .10m, "SAVE20" => .20m, "FLAT40" => .40m,
        _ => throw new ArgumentException("Invalid coupon code.")
    };
    public static CartQuote Quote(decimal subtotal, string? coupon, bool hasItems = true)
    {
        if (subtotal < 0) throw new ArgumentOutOfRangeException(nameof(subtotal));
        var discount = decimal.Round(subtotal * DiscountRate(coupon), 2, MidpointRounding.AwayFromZero);
        var shipping = !hasItems || subtotal - discount >= FreeShippingThreshold ? 0m : 50m;
        return new(subtotal, discount, shipping, subtotal - discount + shipping);
    }
}
