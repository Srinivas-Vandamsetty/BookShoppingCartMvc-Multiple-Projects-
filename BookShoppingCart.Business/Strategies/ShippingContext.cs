namespace BookShoppingCart.Business.Strategies;
public class ShippingContext(IShippingStrategy strategy)
{
    public decimal CalculateShipping(decimal total) => strategy.CalculateShipping(total);
}
