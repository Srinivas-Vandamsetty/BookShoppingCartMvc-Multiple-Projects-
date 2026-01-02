using System;

namespace BookShoppingCart.Business.Strategies
{
    public class ExpressShippingStrategy : IShippingStrategy
    {
        private const decimal ExpressCharge = 100;

        public decimal CalculateShipping(decimal discountedTotal)
        {
            return ExpressCharge;
        }
    }
}
