using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BookShoppingCart.Business.Services
{
    public interface IDiscountService
    {
        decimal GetDiscountRate(string couponCode);
        decimal CalculateDiscountAmount(decimal total, string couponCode);
    }

}
