using BookShoppingCart.Models.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BookShoppingCart.Business.Factories
{
    public interface IPaymentService
    {
        Task<PaymentResult> ProcessPayment(decimal amount);
    }

}
