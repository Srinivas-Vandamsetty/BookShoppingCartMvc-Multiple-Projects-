using BookStoreCore.Pricing;
namespace BookShoppingCart.Business.Services;
public class DiscountService : IDiscountService
{
    public decimal GetDiscountRate(string? couponCode)
    {
        try { return CartPricing.DiscountRate(couponCode); }
        catch (ArgumentException) { return 0; }
    }
    public decimal CalculateDiscountAmount(decimal total, string? couponCode) =>
        decimal.Round(total * GetDiscountRate(couponCode), 2, MidpointRounding.AwayFromZero);
}
