using BookShoppingCart.Models.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BookShoppingCart.Business.Factories
{
    public class CashPaymentService : IPaymentService
    {
        public async Task<PaymentResult> ProcessPayment(decimal amount)
        {
            await Task.CompletedTask;

            return new PaymentResult
            {
                IsSuccess = true,
                TransactionId = "",
                Message = "Cash on delivery selected; payment has not been collected"
            };
        }
    }
}
