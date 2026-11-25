using BookShoppingCart.Models.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BookShoppingCart.Business.Factories
{
    public class CreditCardPaymentService : IPaymentService
    {
        public Task<PaymentResult> ProcessPayment(decimal amount) =>
            Task.FromResult(new PaymentResult { IsSuccess = true, TransactionId = $"CARD-{Guid.NewGuid():N}".ToUpperInvariant(),
                Message = "Card sandbox payment authorized." });
    }
}
