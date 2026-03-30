using BookShoppingCart.Business.Factories;
using BookShoppingCart.Models.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BookShoppingCart.Business.Decorators
{
    public class LoggingPaymentDecorator : PaymentDecorator
    {
        public LoggingPaymentDecorator(IPaymentService paymentService)
            : base(paymentService)
        {
        }

        public override async Task<PaymentResult> ProcessPayment(decimal amount)
        {
            Console.WriteLine($"[LOG] Payment started for {amount}");

            var result = await base.ProcessPayment(amount);

            Console.WriteLine($"[LOG] Payment completed: {result.IsSuccess}");

            return result;
        }
    }
}
