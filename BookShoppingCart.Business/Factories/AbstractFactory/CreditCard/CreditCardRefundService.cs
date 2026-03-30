using BookShoppingCart.Business.Factories.AbstractFactory.Interfaces;
using BookShoppingCart.Models.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BookShoppingCart.Business.Factories.AbstractFactory.CreditCard
{
    public class CreditCardRefundService : IRefundService
    {
        public async Task<bool> Refund(string transactionId)
        {
            await Task.Delay(300);
            return true;
        }
    }
}
