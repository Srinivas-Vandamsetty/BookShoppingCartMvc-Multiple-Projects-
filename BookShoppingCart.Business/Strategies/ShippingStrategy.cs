using System;

namespace BookShoppingCart.Business.Strategies
{
    public class ShippingStrategy : IShippingStrategy
    {
        private const decimal StandardCharge = 50;
        private const decimal FreeShipping = 999;

        public decimal CalculateShipping(decimal discountedTotal)
        {
            if (discountedTotal >= FreeShipping)
            {
                return 0;
            }

            return StandardCharge;
        }
    }
}
