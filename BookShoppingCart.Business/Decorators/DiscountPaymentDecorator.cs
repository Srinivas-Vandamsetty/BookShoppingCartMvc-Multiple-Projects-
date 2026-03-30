using BookShoppingCart.Business.Factories;
using BookShoppingCart.Models.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BookShoppingCart.Business.Decorators
{
    public class DiscountPaymentDecorator : PaymentDecorator
    {
        private const decimal Discount = 50;

        public DiscountPaymentDecorator(IPaymentService paymentService)
            : base(paymentService)
        {
        }

        public override async Task<PaymentResult> ProcessPayment(decimal amount)
        {
            amount -= Discount;
            return await base.ProcessPayment(amount);
        }
    }
}
