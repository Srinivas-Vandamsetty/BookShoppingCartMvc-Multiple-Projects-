using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BookShoppingCart.Models.Models.DTOs
{
    public class OrderListDTO
    {
        public int OrderId { get; set; }
        public DateTime CreateDate { get; set; }
        public string OrderStatus { get; set; } = string.Empty;
        public bool IsPaid { get; set; }
        public string BookName { get; set; } = string.Empty;
        public string GenreName { get; set; } = string.Empty;
        public int Quantity { get; set; }
    }
}
