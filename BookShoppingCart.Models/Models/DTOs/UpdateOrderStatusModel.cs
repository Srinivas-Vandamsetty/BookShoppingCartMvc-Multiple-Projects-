using Microsoft.AspNetCore.Mvc.Rendering;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace BookShoppingCart.Models.Models.DTOs;

public class UpdateOrderStatusModel
{
    public int OrderId { get; set; }
    public System.Guid OperationId { get; set; }
    [Range(1,int.MaxValue)] public int ExpectedStatusId { get; set; }
    [Required, StringLength(300)] public string Reason { get; set; } = "";

    [Required]
    public int OrderStatusId { get; set; }

    public IEnumerable<SelectListItem>? OrderStatusList { get; set; }
}
