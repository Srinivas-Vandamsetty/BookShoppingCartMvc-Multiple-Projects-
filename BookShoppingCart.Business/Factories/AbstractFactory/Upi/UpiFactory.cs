using BookShoppingCart.Business.Factories.AbstractFactory.Interfaces;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BookShoppingCart.Business.Factories.AbstractFactory.Upi
{
    public class UpiFactory : IPaymentGatewayFactory
    {
        public IPaymentsService CreatePaymentService()
            => new UpiPaymentService();

        public IRefundService CreateRefundService()
            => new UpiRefundService();

        public IInvoiceService CreateInvoiceService()
            => new UpiInvoiceService();
    }
}
