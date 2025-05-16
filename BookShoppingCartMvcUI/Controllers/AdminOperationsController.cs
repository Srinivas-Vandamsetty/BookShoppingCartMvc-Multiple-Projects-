using BookShoppingCart.Business.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;

namespace BookShoppingCartMvcUI.Controllers;

// Only admin users can access this controller
[Authorize(Roles = nameof(Roles.Admin))]
public class AdminOperationsController : Controller
{
    private readonly IUserOrderService _userOrderService;

    // Constructor to inject the user order service
    public AdminOperationsController(IUserOrderService userOrderService)
    {
        _userOrderService = userOrderService;
    }

    // Show all orders to admin
    public async Task<IActionResult> AllOrders()
    {
        var orders = await _userOrderService.GetUserOrders(true);
        return View(orders);
    }

    // Toggle the payment status of an order
    public async Task<IActionResult> TogglePaymentStatus(int orderId)
    {
        try
        {
            await _userOrderService.TogglePaymentStatus(orderId);
        }
        catch (Exception ex)
        {
            // Handle exception (optional logging)
        }
        return RedirectToAction(nameof(AllOrders));
    }

    // Show the update order status form
    public async Task<IActionResult> UpdateOrderStatus(int orderId)
    {
        var order = await _userOrderService.GetOrderById(orderId);

        // If the order is not found, show an error
        if (order == null)
        {
            throw new InvalidOperationException($"Order with id:{orderId} does not found.");
        }

        // Create dropdown list for order statuses
        var orderStatusList = (await _userOrderService.GetOrderStatuses()).Select(orderStatus =>
        {
            return new SelectListItem
            {
                Value = orderStatus.Id.ToString(),
                Text = orderStatus.StatusName,
                Selected = order.OrderStatusId == orderStatus.Id
            };
        });

        // Prepare the data to pass to the view
        var data = new UpdateOrderStatusModel
        {
            OrderId = orderId,
            OrderStatusId = order.OrderStatusId,
            OrderStatusList = orderStatusList
        };

        return View(data);
    }

    // Handle form submission to update order status
    [HttpPost]
    public async Task<IActionResult> UpdateOrderStatus(UpdateOrderStatusModel data)
    {
        try
        {
            // If the form is not valid, reload the dropdown and return to the form
            if (!ModelState.IsValid)
            {
                data.OrderStatusList = (await _userOrderService.GetOrderStatuses()).Select(orderStatus =>
                {
                    return new SelectListItem
                    {
                        Value = orderStatus.Id.ToString(),
                        Text = orderStatus.StatusName,
                        Selected = orderStatus.Id == data.OrderStatusId
                    };
                });

                return View(data);
            }

            // Update the order status
            await _userOrderService.ChangeOrderStatus(data);
            TempData["msg"] = "Updated successfully";
        }
        catch (Exception ex)
        {
            TempData["msg"] = "Something went wrong";
        }

        // Redirect back to the update form
        return RedirectToAction(nameof(UpdateOrderStatus), new { orderId = data.OrderId });
    }

    // Show the admin dashboard page
    public IActionResult Dashboard()
    {
        return View();
    }
}
