using BookShoppingCart.Business.Services;
using BookShoppingCart.Business.Strategies;
using BookShoppingCart.Models.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;

namespace BookShoppingCartMvcUI.Controllers
{
    [Authorize]
    public class CartController : Controller
    {
        private readonly ICartService _cartService;
        private readonly IDiscountService _discountService;
        private readonly IShippingStrategy _shippingStrategy;
        private readonly ILogger<CartController> _logger;

        public CartController(
            ICartService cartService,
            IDiscountService discountService,
            IShippingStrategy shippingStrategy,
            ILogger<CartController> logger)
        {
            _cartService = cartService;
            _discountService = discountService;
            _shippingStrategy = shippingStrategy;
            _logger = logger;
        }

        public async Task<IActionResult> GetUserCart(string? coupon = null)
        {
            _logger.LogInformation("Loading user cart. Coupon: {Coupon}", coupon);

            try
            {
                var cart = await _cartService.GetUserCart()
                           ?? new ShoppingCart { CartDetails = new List<CartDetail>() };

                decimal itemTotal = (decimal)cart.CartDetails
                    .Sum(cd => cd.Book.Price * cd.Quantity);

                decimal discountAmount =
                    _discountService.CalculateDiscountAmount(itemTotal, coupon);

                decimal discountedTotal = itemTotal - discountAmount;

                decimal shippingCharge =
                    _shippingStrategy.CalculateShipping(discountedTotal);

                decimal finalTotal = discountedTotal + shippingCharge;

                _logger.LogInformation(
                    "Cart Calculated. ItemTotal: {ItemTotal}, Discount: {Discount}, Shipping: {Shipping}, FinalTotal: {FinalTotal}",
                    itemTotal, discountAmount, shippingCharge, finalTotal);

                ViewBag.CouponCode = coupon;
                ViewBag.ItemTotal = itemTotal;
                ViewBag.DiscountRate = _discountService.GetDiscountRate(coupon);
                ViewBag.DiscountAmount = discountAmount;
                ViewBag.ShippingCharge = shippingCharge;
                ViewBag.FinalTotal = finalTotal;

                return View(cart);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error occurred while loading user cart");
                TempData["errorMessage"] = "Failed to load cart.";
                return View(new ShoppingCart { CartDetails = new List<CartDetail>() });
            }
        }

        public async Task<IActionResult> AddItem(int bookId, int qty = 1, int redirect = 0)
        {
            _logger.LogInformation("Adding item to cart. BookId: {BookId}, Quantity: {Quantity}", bookId, qty);

            try
            {
                var cartCount = await _cartService.AddItem(bookId, qty);

                _logger.LogInformation("Item added successfully. Updated Cart Count: {CartCount}", cartCount);

                if (redirect == 0)
                    return Ok(cartCount);

                return RedirectToAction(nameof(GetUserCart));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error adding item to cart. BookId: {BookId}", bookId);
                return BadRequest("Unable to add item.");
            }
        }

        public async Task<IActionResult> RemoveItem(int bookId)
        {
            _logger.LogInformation("Removing item from cart. BookId: {BookId}", bookId);

            try
            {
                await _cartService.RemoveItem(bookId);
                _logger.LogInformation("Item removed successfully. BookId: {BookId}", bookId);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error removing item from cart. BookId: {BookId}", bookId);
                TempData["errorMessage"] = "Unable to remove item.";
            }

            return RedirectToAction(nameof(GetUserCart));
        }

        public async Task<IActionResult> GetTotalItemInCart()
        {
            _logger.LogInformation("Fetching total cart item count");

            try
            {
                int cartItem = await _cartService.GetCartItemCount();
                return Ok(cartItem);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error fetching cart item count");
                return Ok(0);
            }
        }

        [HttpGet]
        public async Task<IActionResult> GetCartItemIds()
        {
            _logger.LogInformation("Fetching cart item IDs");

            try
            {
                var cart = await _cartService.GetUserCart();
                var bookIds = cart?.CartDetails?.Select(cd => cd.BookId).ToList()
                              ?? new List<int>();

                return Json(bookIds);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error fetching cart item IDs");
                return Json(new List<int>());
            }
        }

        public IActionResult Checkout()
        {
            _logger.LogInformation("Loading Checkout page");
            return View();
        }

        [HttpPost]
        public async Task<IActionResult> Checkout(CheckoutModel model)
        {
            if (!ModelState.IsValid)
            {
                _logger.LogWarning("Checkout model state invalid");
                return View(model);
            }

            _logger.LogInformation("Processing checkout for user {User}", User.Identity?.Name);

            try
            {
                bool isCheckedOut = await _cartService.DoCheckout(model);

                if (!isCheckedOut)
                {
                    _logger.LogWarning("Checkout failed for user {User}", User.Identity?.Name);
                    return RedirectToAction(nameof(OrderFailure));
                }

                _logger.LogInformation("Checkout successful for user {User}", User.Identity?.Name);
                return RedirectToAction(nameof(OrderSuccess));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error occurred during checkout");
                return RedirectToAction(nameof(OrderFailure));
            }
        }

        public IActionResult OrderSuccess()
        {
            _logger.LogInformation("Order success page loaded");
            return View();
        }

        public IActionResult OrderFailure()
        {
            _logger.LogWarning("Order failure page loaded");
            return View();
        }
    }
}