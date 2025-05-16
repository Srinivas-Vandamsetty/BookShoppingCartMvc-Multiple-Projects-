using BookShoppingCart.Business.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;

namespace BookShoppingCartMvcUI.Controllers
{
    // Only authenticated users can access this controller
    [Authorize]
    public class UserOrderController : Controller
    {
        private readonly IUserOrderService _userOrderService;

        // Inject the user order service into the controller
        public UserOrderController(IUserOrderService userOrderService)
        {
            _userOrderService = userOrderService;
        }

        // Display the list of orders for the currently logged-in user
        public async Task<IActionResult> UserOrders()
        {
            var orders = await _userOrderService.GetUserOrders();
            return View(orders);
        }
    }
}
