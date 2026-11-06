using BookShoppingCart.Business.Factories;
using BookShoppingCart.Models.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BookShoppingCart.Business.Factories
{
    public class UpiPaymentService : IPaymentService
    {
        public Task<PaymentResult> ProcessPayment(decimal amount) =>
            throw new NotSupportedException("A verified electronic payment provider is not configured.");
    }
}
