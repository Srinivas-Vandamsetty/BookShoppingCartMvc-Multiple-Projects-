using System.Collections.Generic;

namespace BookShoppingCart.Models.Models.DTOs;

public class OrderDetailModalDTO
{
    public string DivId { get; set; } = string.Empty;
    public IEnumerable<OrderDetail> OrderDetail { get; set; } = [];
}
