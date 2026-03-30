using BookShoppingCart.Business.Factories.AbstractFactory.Interfaces;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BookShoppingCart.Business.Factories.AbstractFactory.CreditCard
{
    public class CreditCardInvoiceService : IInvoiceService
    {
        public string GenerateInvoice(decimal amount)
        {
            return $"Credit Card Invoice - Amount: {amount}";
        }
    }
}
