using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace BookShoppingCart.Models.Models
{
    [Table("ShoppingCart")]
    public class ShoppingCart
    {
        [NotMapped] public decimal DiscountAmount { get; set; }
        [NotMapped] public decimal FinalAmount { get; set; }

        public int Id { get; set; }

        [Required]
        public string UserId { get; set; } = string.Empty;

        public bool IsDeleted { get; set; } = false;

        public ICollection<CartDetail> CartDetails { get; set; } = [];

        [NotMapped]
        public decimal TotalAmount { get; set; }

        [NotMapped]
        public decimal DiscountedAmount { get; set; }
    }
}
