using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BookShoppingCart.Business.Strategies
{
    namespace BookShoppingCart.Business.Strategies
    {
        public class ShippingContext
        {
            private readonly IShippingStrategy _shippingStrategy;

            public ShippingContext(IShippingStrategy shippingStrategy)
            {
                _shippingStrategy = shippingStrategy;
            }

            public decimal CalculateShipping(decimal total)
            {
                return _shippingStrategy.CalculateShipping(total);
            }
        }
    }
}
