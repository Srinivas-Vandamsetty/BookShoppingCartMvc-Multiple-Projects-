using System;

namespace BookShoppingCart.Business.Strategies
{
    public class StandardShippingStrategy : IShippingStrategy
    {
        private const decimal StandardCharge = 50;
        private const decimal FreeShippingThreshold = 999;

        public decimal CalculateShipping(decimal discountedTotal)
        {
            if (discountedTotal >= FreeShippingThreshold)
            {
                return 0;
            }

            return StandardCharge;
        }
    }
}
