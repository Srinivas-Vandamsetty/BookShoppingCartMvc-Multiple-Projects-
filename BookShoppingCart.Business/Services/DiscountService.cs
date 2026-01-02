using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BookShoppingCart.Business.Services
{
    public class DiscountService : IDiscountService
    {
        private readonly Dictionary<string, decimal> _validCoupons = new()
    {
        { "SAVE10", 0.10m },
        { "SAVE20", 0.20m },
        { "FLAT50", 0.50m }
    };

        public decimal GetDiscountRate(string couponCode)
        {
            if (string.IsNullOrEmpty(couponCode))
                return 0;

            _validCoupons.TryGetValue(couponCode.ToUpper(), out var rate);
            return rate;
        }

        public decimal CalculateDiscountAmount(decimal total, string couponCode)
        {
            var rate = GetDiscountRate(couponCode);
            return total * rate;
        }
    }

}
