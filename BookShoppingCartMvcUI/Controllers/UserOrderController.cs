using BookShoppingCart.Business.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;

namespace BookShoppingCartMvcUI.Controllers
{
    [Authorize]
    public class UserOrderController : Controller
    {
        private readonly IUserOrderService _userOrderService;
        private readonly ILogger<UserOrderController> _logger;

        public UserOrderController(
            IUserOrderService userOrderService,
            ILogger<UserOrderController> logger)
        {
            _userOrderService = userOrderService;
            _logger = logger;
        }

        // Display user orders
        public async Task<IActionResult> UserOrders()
        {
            var userName = User.Identity?.Name;

            _logger.LogInformation("Fetching orders for user {User}", userName);

            try
            {
                var orders = await _userOrderService.GetUserOrders();

                _logger.LogInformation(
                    "Fetched {OrderCount} orders for user {User}",
                    orders.Count(),
                    userName);

                return View(orders);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex,
                    "Error occurred while fetching orders for user {User}",
                    userName);

                TempData["errorMessage"] = "Failed to load orders.";
                return View(new List<object>());
            }
        }
    }
}