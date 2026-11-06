using BookShoppingCart.Business.Services;
using BookShoppingCart.Data.Repositories;
using BookShoppingCart.Models.Models;
using BookShoppingCart.Models.Models.DTOs;
using BookShoppingCartMvcUI.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.AspNetCore.WebUtilities;

namespace BookShoppingCartMvcUI.Controllers;

[Authorize(Roles = nameof(Roles.Admin))]
public class AdminOperationsController(IUserOrderService orders, CommerceApiClient api) : Controller
{
    public async Task<IActionResult> AllOrders(int page = 1, string? status = null, string? search = null)
    {
        var path = QueryHelpers.AddQueryString($"admin/orders?page={page}", new Dictionary<string,string?> { ["status"] = status, ["search"] = search });
        var items = await api.Get<CommercePage<OrderResponse>>(path);
        ViewBag.Page = page;
        ViewBag.HasNext = items.HasNext;
        ViewBag.StatusFilter = status;
        ViewBag.Search = search;
        return View(items.Items.Select(o => o.ToModel()));
    }

    [HttpPost]
    public async Task<IActionResult> RecordCashCollection(int orderId, CashCollectionRequest request)
    {
        if (!ModelState.IsValid) TempData["errorMessage"] = "Enter a reason for the cash collection change.";
        else
        {
            try
            {
                await api.Send(HttpMethod.Put, $"admin/orders/{orderId}/cash-collection", request);
                TempData["successMessage"] = "Cash collection recorded.";
            }
            catch (CommerceApiException ex) { TempData["errorMessage"] = ex.Message; }
        }
        return RedirectToAction(nameof(AllOrders));
    }

    // Keep the original form route compatible while moving the current UI to explicit updates.
    [HttpPost]
    public async Task<IActionResult> TogglePaymentStatus(int orderId)
    {
        try { await orders.TogglePaymentStatus(orderId); }
        catch (CommerceApiException ex) { TempData["errorMessage"] = ex.Message; }
        return RedirectToAction(nameof(AllOrders));
    }

    private async Task Populate(UpdateOrderStatusModel data, Order order)
    {
        data.OrderStatusList = (await orders.GetOrderStatuses())
            .Where(s => OrderTransitions.Allows(order.OrderStatus.StatusName, s.StatusName))
            .Where(s => s.StatusName != "Cancelled" || !order.IsPaid || s.Id == order.OrderStatusId)
            .Select(s => new SelectListItem(s.StatusName, s.Id.ToString(), s.Id == data.OrderStatusId));
    }

    public async Task<IActionResult> UpdateOrderStatus(int orderId)
    {
        var order = await orders.GetOrderById(orderId);
        if (order is null) return NotFound();
        var data = new UpdateOrderStatusModel { OrderId = orderId, OrderStatusId = order.OrderStatusId,
            ExpectedStatusId = order.OrderStatusId, OperationId = Guid.NewGuid() };
        await Populate(data, order);
        return View(data);
    }

    [HttpPost]
    public async Task<IActionResult> UpdateOrderStatus(UpdateOrderStatusModel data)
    {
        if (ModelState.IsValid)
        {
            try
            {
                await orders.ChangeOrderStatus(data);
                TempData["msg"] = "Order status updated.";
                return RedirectToAction(nameof(UpdateOrderStatus), new { orderId = data.OrderId });
            }
            catch (CommerceApiException ex) { ModelState.AddModelError("", ex.Message); }
        }
        var order = await orders.GetOrderById(data.OrderId);
        if (order is null) return NotFound();
        await Populate(data, order);
        return View(data);
    }

    public IActionResult Dashboard() => View();
}
