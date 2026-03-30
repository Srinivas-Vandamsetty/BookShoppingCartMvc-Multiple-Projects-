using BookShoppingCart.Business.Factories.AbstractFactory.CreditCard;
using BookShoppingCart.Business.Factories.AbstractFactory.Interfaces;
using BookShoppingCart.Business.Factories.AbstractFactory.Upi;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BookShoppingCart.Business.Factories.AbstractFactory.Payments
{
    public static class PaymentGatewayFactorySelector
    {
        public static IPaymentGatewayFactory GetFactory(string method)
        {
            return method.ToLower() switch
            {
                "creditcard" => new CreditCardFactory(),
                "upi" => new UpiFactory(),
                _ => throw new ArgumentException("Invalid payment method")
            };
        }
    }
}
