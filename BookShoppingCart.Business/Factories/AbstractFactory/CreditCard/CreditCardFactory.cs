using BookShoppingCart.Business.Factories.AbstractFactory.Interfaces;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BookShoppingCart.Business.Factories.AbstractFactory.CreditCard
{
    public class CreditCardFactory : IPaymentGatewayFactory
    {
        public IPaymentsService CreatePaymentService()
            => new CreditCardPaymentService();

        public IRefundService CreateRefundService()
            => new CreditCardRefundService();

        public IInvoiceService CreateInvoiceService()
            => new CreditCardInvoiceService();
    }
}
