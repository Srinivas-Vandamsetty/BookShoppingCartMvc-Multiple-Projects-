using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BookShoppingCart.Models.Models
{
    public class PaymentResult
    {
        public bool IsSuccess { get; set; }
        public string TransactionId { get; set; } = string.Empty;
        public string Message { get; set; } = string.Empty;
        [Obsolete("Use IsSuccess.")] public bool Success { get => IsSuccess; set => IsSuccess = value; }
        [Obsolete("Use Message.")] public string ErrorMessage { get => Message; set => Message = value; }
    }
}
