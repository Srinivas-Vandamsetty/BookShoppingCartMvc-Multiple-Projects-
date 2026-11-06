using System.ComponentModel.DataAnnotations;
using BookShoppingCart.Business.Services;
using BookShoppingCart.Data.Repositories;
using BookShoppingCart.Models.Models;
using BookShoppingCart.Models.Models.DTOs;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace BookShoppingCart.WebAPI.Controllers;

[ApiController, Authorize, Route("api/orders"), ResponseCache(NoStore = true, Location = ResponseCacheLocation.None)]
public sealed class OrdersController(CommerceOperations operations, IUserOrderService orders) : ControllerBase
{
    [HttpGet]
    public async Task<CommercePage<OrderResponse>> List([FromQuery, Range(1,100000)] int page = 1, CancellationToken ct = default) =>
        CommercePage<OrderResponse>.From((await operations.SearchOrders(page, false, null, null, ct)).Select(o => o.ToResponse()), page);
    [HttpGet("{orderId:int}")]
    public async Task<OrderResponse> Get(int orderId, CancellationToken ct) =>
        (await operations.FindOrder(orderId, ct) ?? throw new KeyNotFoundException("Order not found.")).ToResponse();
    [HttpGet("/api/admin/orders"), Authorize(Roles = "Admin")]
    public async Task<CommercePage<OrderResponse>> AdminList([FromQuery, Range(1,100000)] int page = 1,
        [FromQuery, StringLength(30)] string? status = null, [FromQuery, StringLength(80)] string? search = null, CancellationToken ct = default) =>
        CommercePage<OrderResponse>.From((await operations.SearchOrders(page, true, status, search, ct)).Select(o => o.ToResponse()), page);
    [HttpGet("/api/admin/order-statuses"), Authorize(Roles = "Admin")]
    public Task<IEnumerable<OrderStatus>> Statuses() => orders.GetOrderStatuses();
    [HttpPatch("/api/admin/orders/{orderId:int}/status"), Authorize(Roles = "Admin")]
    public async Task<IActionResult> UpdateStatus(int orderId, OrderStatusRequest request, CancellationToken ct)
    { await operations.UpdateStatus(orderId, request, ct); return NoContent(); }
    [HttpPut("/api/admin/orders/{orderId:int}/cash-collection"), Authorize(Roles = "Admin")]
    public async Task<IActionResult> RecordCash(int orderId, CashCollectionRequest request, CancellationToken ct)
    { await operations.RecordCash(orderId, request, ct); return NoContent(); }
}
