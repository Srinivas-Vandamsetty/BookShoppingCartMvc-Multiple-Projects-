using System.ComponentModel.DataAnnotations;

namespace BookShoppingCart.Models.Models.DTOs
{
    public class StockDTO
    {
        public int BookId { get; set; }
        public System.Guid OperationId { get; set; }
        [Range(0, int.MaxValue)] public int ExpectedQuantity { get; set; }
        [StringLength(300)] public string Reason { get; set; } = "";

        [Range(0, int.MaxValue, ErrorMessage = "Quantity must be a non-negative value.")]
        public int Quantity { get; set; }
    }
}
