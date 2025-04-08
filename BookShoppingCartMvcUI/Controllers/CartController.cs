using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using BookShoppingCart.Business.Services;

namespace BookShoppingCartMvcUI.Controllers
{
    // Controller for handling shopping cart operations
    [Authorize]
    public class CartController : Controller
    {
        private readonly ICartService _cartService;

        // Constructor to inject cart service
        public CartController(ICartService cartService)
        {
            _cartService = cartService;
        }

        // Adds an item to the cart
        public async Task<IActionResult> AddItem(int bookId, int qty = 1, int redirect = 0)
        {
            var cartCount = await _cartService.AddItem(bookId, qty);

            // If redirect flag is 0, return the cart count as a response
            if (redirect == 0)
                return Ok(cartCount);

            // Otherwise, redirect to the user cart view
            return RedirectToAction(nameof(GetUserCart));
        }

        // Removes an item from the cart
        public async Task<IActionResult> RemoveItem(int bookId)
        {
            var cartCount = await _cartService.RemoveItem(bookId);

            // Redirect to the user cart view after removal
            return RedirectToAction(nameof(GetUserCart));
        }

        // Displays the user's cart
        public async Task<IActionResult> GetUserCart()
        {
            var cart = await _cartService.GetUserCart() ?? new ShoppingCart { CartDetails = new List<CartDetail>() };
            return View(cart);
        }

        // Retrieves the total number of items in the cart
        public async Task<IActionResult> GetTotalItemInCart()
        {
            int cartItem = await _cartService.GetCartItemCount();
            return Ok(cartItem);
        }
    }
}
