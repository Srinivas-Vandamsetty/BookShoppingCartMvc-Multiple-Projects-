using BookShoppingCart.Business.Factories.AbstractFactory.Interfaces;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BookShoppingCart.Business.Factories.AbstractFactory.Upi
{
    public class UpiInvoiceService : IInvoiceService
    {
        public string GenerateInvoice(decimal amount)
        {
            return $"UPI Invoice - Amount: {amount}";
        }
    }
}
