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
        public async Task<PaymentResult> ProcessPayment(decimal amount)
        {
            await Task.Delay(500);

            return new PaymentResult
            {
                IsSuccess = true,
                TransactionId = Guid.NewGuid().ToString(),
                Message = "Credit card payment processed successfully"
            };
        }
    }
}
