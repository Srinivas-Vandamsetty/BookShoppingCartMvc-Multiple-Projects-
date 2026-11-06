using BookShoppingCart.Business.Factories.AbstractFactory.Interfaces;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BookShoppingCart.Business.Factories.AbstractFactory.Upi
{
    public class UpiRefundService : IRefundService
    {
        public Task<bool> Refund(string transactionId) =>
            throw new NotSupportedException("A verified refund provider is not configured.");
    }
}
