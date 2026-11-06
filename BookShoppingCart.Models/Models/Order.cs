using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace BookShoppingCart.Models.Models
{
    [Table("Order")]
    public class Order
    {
        public int Id { get; set; }
        public Guid? CheckoutKey { get; set; }
        public decimal Subtotal { get; set; }
        public decimal Discount { get; set; }
        public decimal Shipping { get; set; }
        public decimal Total { get; set; }
        [MaxLength(20)] public string? CouponCode { get; set; }
        [Required]
        public string UserId { get; set; } = string.Empty;
        public DateTime CreateDate { get; set; } = DateTime.UtcNow;
        [Required]
        public int OrderStatusId { get; set; }
        public bool IsDeleted { get; set; } = false;
        [Required]
        [MaxLength(30)]
        public string Name { get; set; } = string.Empty;

        [Required]
        [EmailAddress]
        [MaxLength(30)]
        public string Email { get; set; } = string.Empty;
        [Required]
        public string MobileNumber { get; set; } = string.Empty;
        [Required]
        [MaxLength(200)]
        public string Address { get; set; } = string.Empty;
        [Required]
        [MaxLength(30)]
        public string PaymentMethod { get; set; } = string.Empty;
        public bool IsPaid { get; set; }

        public OrderStatus OrderStatus { get; set; } = null!;
        public List<OrderDetail> OrderDetail { get; set; } = [];
    }
}
