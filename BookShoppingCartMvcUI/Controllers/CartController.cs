using BookShoppingCart.Business.Services;
using BookShoppingCart.Business.Strategies;
using BookShoppingCart.Models.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Threading.Tasks;

namespace BookShoppingCartMvcUI.Controllers
{
    [Authorize]
    public class CartController : Controller
    {
        private readonly ICartService _cartService;
        private readonly IDiscountService _discountService;
        private readonly IShippingStrategy _shippingStrategy;

        public CartController(ICartService cartService, IDiscountService discountService, IShippingStrategy shippingStrategy)
        {
            _cartService = cartService;
            _discountService = discountService;
            _shippingStrategy = shippingStrategy;
        }

        public async Task<IActionResult> GetUserCart(string coupon = null)
        {
            var cart = await _cartService.GetUserCart() ?? new ShoppingCart { CartDetails = new List<CartDetail>() };

            decimal itemTotal = (decimal)cart.CartDetails.Sum(cd => cd.Book.Price * cd.Quantity);

            decimal discountAmount = _discountService.CalculateDiscountAmount(itemTotal, coupon);
            decimal discountedTotal = itemTotal - discountAmount;

            decimal shippingCharge = _shippingStrategy.CalculateShipping(discountedTotal);
            decimal finalTotal = discountedTotal + shippingCharge;

            ViewBag.CouponCode = coupon;
            ViewBag.ItemTotal = itemTotal;
            ViewBag.DiscountRate = _discountService.GetDiscountRate(coupon);
            ViewBag.DiscountAmount = discountAmount;
            ViewBag.ShippingCharge = shippingCharge;
            ViewBag.FinalTotal = finalTotal;

            return View(cart);
        }


        public async Task<IActionResult> AddItem(int bookId, int qty = 1, int redirect = 0)
        {
            var cartCount = await _cartService.AddItem(bookId, qty);
            if (redirect == 0)
                return Ok(cartCount);
            return RedirectToAction(nameof(GetUserCart));
        }

        public async Task<IActionResult> RemoveItem(int bookId)
        {
            var cartCount = await _cartService.RemoveItem(bookId);
            return RedirectToAction(nameof(GetUserCart));
        }

        public async Task<IActionResult> GetTotalItemInCart()
        {
            int cartItem = await _cartService.GetCartItemCount();
            return Ok(cartItem);
        }

        [HttpGet]
        public async Task<IActionResult> GetCartItemIds()
        {
            var cart = await _cartService.GetUserCart();
            var bookIds = cart?.CartDetails?.Select(cd => cd.BookId).ToList() ?? new List<int>();
            return Json(bookIds);
        }

        public IActionResult Checkout()
        {
            return View();
        }

        [HttpPost]
        public async Task<IActionResult> Checkout(CheckoutModel model)
        {
            if (!ModelState.IsValid)
                return View(model);

            bool isCheckedOut = await _cartService.DoCheckout(model);
            if (!isCheckedOut)
                return RedirectToAction(nameof(OrderFailure));

            return RedirectToAction(nameof(OrderSuccess));
        }

        public IActionResult OrderSuccess()
        {
            return View();
        }

        public IActionResult OrderFailure()
        {
            return View();
        }
    }
}