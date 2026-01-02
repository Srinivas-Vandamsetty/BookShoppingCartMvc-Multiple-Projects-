using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BookShoppingCart.Business.Strategies
{
    public interface IShippingStrategy
    {
        decimal CalculateShipping(decimal discountedTotal);
    }
}
