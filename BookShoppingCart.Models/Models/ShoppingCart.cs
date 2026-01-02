using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace BookShoppingCart.Models.Models
{
    [Table("ShoppingCart")]
    public class ShoppingCart
    {
        public decimal DiscountAmount;
        public decimal FinalAmount;

        public int Id { get; set; }

        [Required]
        public string UserId { get; set; }

        public bool IsDeleted { get; set; } = false;

        public ICollection<CartDetail> CartDetails { get; set; }

        [NotMapped]
        public decimal TotalAmount { get; set; }

        [NotMapped]
        public decimal DiscountedAmount { get; set; }
    }
}