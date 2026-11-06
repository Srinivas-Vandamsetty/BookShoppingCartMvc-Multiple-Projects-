using BookShoppingCart.Business.Factories.AbstractFactory.Interfaces;
using BookShoppingCart.Models.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BookShoppingCart.Business.Factories.AbstractFactory.Upi
{
    public class UpiPaymentService : IPaymentsService
    {
        public Task<PaymentResult> ProcessPayment(decimal amount) =>
            throw new NotSupportedException("A verified electronic payment provider is not configured.");
    }
}
