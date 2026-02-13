using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BookShoppingCart.Business.Factories
{
    public class CashPaymentService : IPaymentService
    {
        public Task<bool> ProcessPayment(decimal amount)
        {
            Console.WriteLine("Processing Cash Payment");
            return Task.FromResult(true);
        }
    }
}
