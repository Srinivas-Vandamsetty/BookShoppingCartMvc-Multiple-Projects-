using BookShoppingCartMvcUI.Services;
using BookShoppingCart.Business.Services;
using BookShoppingCart.Models.Models;
using BookStoreCore.Pricing;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace BookShoppingCartMvcUI.Controllers;
[Authorize]
public class CartController(ICartService cartService, ICommerceCartGateway checkout) : Controller
{
    public async Task<IActionResult> GetUserCart(string? coupon = null)
    {
        var cart = await cartService.GetUserCart();
        CartQuote quote;
        try { quote = CartPricing.Quote(cart.CartDetails.Sum(d => d.Book.Price * d.Quantity), coupon, cart.CartDetails.Count > 0); }
        catch (ArgumentException ex)
        {
            TempData["errorMessage"] = ex.Message;
            coupon = null;
            quote = CartPricing.Quote(cart.CartDetails.Sum(d => d.Book.Price * d.Quantity), null, cart.CartDetails.Count > 0);
        }
        ViewBag.CouponCode = coupon;
        ViewBag.ItemTotal = quote.Subtotal;
        ViewBag.DiscountRate = CartPricing.DiscountRate(coupon);
        ViewBag.DiscountAmount = quote.Discount;
        ViewBag.ShippingCharge = quote.Shipping;
        ViewBag.FinalTotal = quote.Total;
        return View(cart);
    }
    [HttpPost]
    public async Task<IActionResult> AddItem(int bookId, int qty = 1, int redirect = 0, string? coupon = null)
    {
        try
        {
            var count = await cartService.AddItem(bookId, qty);
            return redirect == 0 ? Ok(count) : RedirectToAction(nameof(GetUserCart), new { coupon });
        }
        catch (CommerceApiException ex)
        {
            if (redirect == 0) return StatusCode((int)ex.Status, new { message = ex.Message });
            TempData["errorMessage"] = ex.Message;
            return RedirectToAction(nameof(GetUserCart), new { coupon });
        }
        catch (Exception ex) when (ex is ArgumentException or InvalidOperationException or KeyNotFoundException)
        {
            if (redirect == 0) return BadRequest(new { message = ex.Message });
            TempData["errorMessage"] = ex.Message;
            return RedirectToAction(nameof(GetUserCart), new { coupon });
        }
    }
    [HttpPost]
    public async Task<IActionResult> RemoveItem(int bookId, string? coupon = null)
    {
        try { await cartService.RemoveItem(bookId); }
        catch (CommerceApiException ex) { TempData["errorMessage"] = ex.Message; }
        return RedirectToAction(nameof(GetUserCart), new { coupon });
    }
    public async Task<IActionResult> GetTotalItemInCart() => Ok(await cartService.GetCartItemCount());
    public async Task<IActionResult> GetCartItemIds() =>
        Json((await cartService.GetUserCart()).CartDetails.Select(d => d.BookId).ToList());

    [HttpPost]
    public async Task<IActionResult> SetQuantity(int bookId, int quantity, string? coupon = null)
    {
        try { await checkout.SetQuantity(bookId, quantity); }
        catch (CommerceApiException ex) { TempData["errorMessage"] = ex.Message; }
        return RedirectToAction(nameof(GetUserCart), new { coupon });
    }
    [HttpPost]
    public async Task<IActionResult> RemoveLine(int bookId, string? coupon = null)
    {
        try { await checkout.RemoveLine(bookId); }
        catch (CommerceApiException ex) { TempData["errorMessage"] = ex.Message; }
        return RedirectToAction(nameof(GetUserCart), new { coupon });
    }

    private async Task PrepareQuote(CheckoutModel model)
    {
        var quote = await checkout.Quote(model.CouponCode);
        ViewBag.PaymentMethods = await checkout.PaymentMethods();
        model.CheckoutKey = quote.CheckoutKey;
        model.QuoteToken = quote.QuoteToken;
        model.TotalAmount = quote.Total;
        ModelState.Remove(nameof(model.QuoteToken));
        ModelState.Remove(nameof(model.CheckoutKey));
    }

    [HttpGet]
    public async Task<IActionResult> Checkout(string? coupon = null)
    {
        var model = new CheckoutModel { CouponCode = coupon, PaymentMethod = "cash" };
        try { await PrepareQuote(model); return View(model); }
        catch (CommerceApiException ex)
        {
            TempData["errorMessage"] = ex.Message;
            return RedirectToAction(nameof(GetUserCart), new { coupon });
        }
    }
    [HttpPost]
    public async Task<IActionResult> Checkout(CheckoutModel model)
    {
        if (ModelState.IsValid)
        {
            try
            {
                var placed = await checkout.Checkout(model);
                return RedirectToAction(nameof(OrderSuccess), new { orderId = placed.OrderId });
            }
            catch (CommerceApiException ex) when ((int)ex.Status >= 500 || (int)ex.Status == 429)
            {
                // A network failure can occur after commit. Preserve the original attempt key.
                ViewBag.Unconfirmed = true;
                ViewBag.PaymentMethods = new List<PaymentMethodResponse> { new("cash", "Cash on delivery") };
                ModelState.AddModelError("", "We could not confirm the order. Retry this same checkout or check your order history.");
                return View(model);
            }
            catch (CommerceApiException ex) { ModelState.AddModelError("", ex.Message); }
        }
        try { await PrepareQuote(model); return View(model); }
        catch (CommerceApiException ex)
        {
            TempData["errorMessage"] = ex.Message;
            return RedirectToAction(nameof(GetUserCart), new { coupon = model.CouponCode });
        }
    }
    public IActionResult OrderSuccess(int? orderId = null)
    { ViewBag.OrderId = orderId; return View(); }
    public IActionResult OrderFailure() => View();
}
