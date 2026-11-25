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
            Task.FromResult(new PaymentResult { IsSuccess = true, TransactionId = $"UPI-{Guid.NewGuid():N}".ToUpperInvariant(),
                Message = "UPI sandbox payment authorized." });
    }
}
