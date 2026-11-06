using BookShoppingCart.Business.Services;
using BookShoppingCartMvcUI.Services;
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
        private readonly CommerceApiClient _api;

        public UserOrderController(
            IUserOrderService userOrderService,
            ILogger<UserOrderController> logger, CommerceApiClient api)
        {
            _userOrderService = userOrderService;
            _logger = logger;
            _api = api;
        }

        public async Task<IActionResult> Details(int id)
        {
            var order = await _userOrderService.GetOrderById(id);
            return order is null ? NotFound() : View(order);
        }

        // Display user orders
        public async Task<IActionResult> UserOrders(int page = 1)
        {
            var userName = User.Identity?.Name;

            _logger.LogInformation("Fetching orders for user {User}", userName);

            try
            {
                var result = await _api.Get<CommercePage<OrderResponse>>($"orders?page={page}");
                var orders = result.Items;
                ViewBag.Page = page;
                ViewBag.HasNext = result.HasNext;

                _logger.LogInformation(
                    "Fetched {OrderCount} orders for user {User}",
                    orders.Count(),
                    userName);

                return View(orders.Select(o => o.ToModel()));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex,
                    "Error occurred while fetching orders for user {User}",
                    userName);

                TempData["errorMessage"] = "Failed to load orders.";
                return View(new List<Order>());
            }
        }
    }
}
