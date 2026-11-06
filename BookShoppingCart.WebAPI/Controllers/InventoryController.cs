using System.ComponentModel.DataAnnotations;
using BookShoppingCart.Data.Repositories;
using BookShoppingCart.Models.Models.DTOs;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
namespace BookShoppingCart.WebAPI.Controllers;

[ApiController, Authorize(Roles = "Admin"), Route("api/admin/inventory"), ResponseCache(NoStore = true, Location = ResponseCacheLocation.None)]
public sealed class InventoryController(CommerceOperations operations) : ControllerBase
{
    [HttpGet]
    public async Task<CommercePage<StockDisplayModel>> List([FromQuery, Range(1,100000)] int page = 1,
        [FromQuery, StringLength(100)] string? search = null, [FromQuery, Range(0,int.MaxValue)] int? lowStock = null,
        CancellationToken ct = default) => CommercePage<StockDisplayModel>.From(await operations.Inventory(page, search, lowStock, ct), page);
    [HttpPost("{bookId:int}/adjustments")]
    public async Task<IActionResult> Adjust(int bookId, InventoryAdjustmentRequest request, CancellationToken ct)
    { await operations.AdjustStock(bookId, request, ct); return NoContent(); }
}
