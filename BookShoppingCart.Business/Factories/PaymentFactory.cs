using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BookShoppingCart.Business.Factories
{
    public static class PaymentFactory
    {
        public static IPaymentService Create(string paymentMethod)
        {
            return paymentMethod.ToLower() switch
            {
                "cash" => new CashPaymentService(),
                "creditcard" => new CreditCardPaymentService(),
                "paypal" => new UpiPaymentService(),
                _ => throw new ArgumentException("Invalid payment method")
            };
        }
    }
}
