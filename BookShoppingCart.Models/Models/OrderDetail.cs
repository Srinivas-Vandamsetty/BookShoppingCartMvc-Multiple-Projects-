using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace BookShoppingCart.Models.Models
{
    [Table("OrderDetail")]
    public class OrderDetail
    {
        public int Id { get; set; }
        [MaxLength(40)] public string BookName { get; set; } = string.Empty;
        [MaxLength(40)] public string AuthorName { get; set; } = string.Empty;
        [MaxLength(40)] public string GenreName { get; set; } = string.Empty;
        [Required]
        public int OrderId { get; set; }
        [Required]
        public int BookId { get; set; }
        [Required]
        public int Quantity { get; set; }
        [Required]
        public decimal UnitPrice { get; set; }
        public Order Order { get; set; } = null!;
        public Book Book { get; set; } = null!;
    }
}
