using BookShoppingCart.Models.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BookShoppingCart.Business.Factories.AbstractFactory.Interfaces
{
    public interface IPaymentsService
    {
        Task<PaymentResult> ProcessPayment(decimal amount);
    }
}
