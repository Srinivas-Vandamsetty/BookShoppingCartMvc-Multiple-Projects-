using BookShoppingCart.Business.Factories;
using BookShoppingCart.Models.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BookShoppingCart.Business.Decorators
{
    public abstract class PaymentDecorator : IPaymentService
    {
        protected readonly IPaymentService _paymentService;

        protected PaymentDecorator(IPaymentService paymentService)
        {
            _paymentService = paymentService;
        }

        public virtual async Task<PaymentResult> ProcessPayment(decimal amount)
        {
            return await _paymentService.ProcessPayment(amount);
        }
    }
}
