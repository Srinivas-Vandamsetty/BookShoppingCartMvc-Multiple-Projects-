using BookShoppingCart.Business.Services;
using BookShoppingCart.Models.Models;
using BookShoppingCart.Models.Models.DTOs;
using BookShoppingCartMvcUI.Controllers;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
namespace BookShoppingCart.Tests.Tests;
public class CheckoutReviewTests
{
    [Fact]
    public async Task RebuildingQuote_RotatesAttemptKeyAndClearsOldHiddenFieldState()
    {
        var cart = new Mock<ICartService>();
        cart.Setup(c=>c.GetUserCart()).ReturnsAsync(new ShoppingCart
        {
            CartDetails = [new CartDetail { BookId=1,Quantity=1,Book=new Book { Id=1,Price=100 } }]
        });
        var gateway = new Mock<BookShoppingCartMvcUI.Services.ICommerceCartGateway>();
        gateway.Setup(g => g.Quote(null)).ReturnsAsync(() => new CheckoutQuoteResponse(Guid.NewGuid(), Guid.NewGuid().ToString(),
            DateTime.UtcNow.AddMinutes(15), 100m, 0m, 50m, 150m));
        gateway.Setup(g => g.PaymentMethods()).ReturnsAsync([new("cash", "Cash on delivery")]);
        var controller = new CartController(cart.Object, gateway.Object);
        var initial = Assert.IsType<CheckoutModel>(Assert.IsType<ViewResult>(await controller.Checkout()).Model);
        var oldKey = initial.CheckoutKey;
        var oldToken = initial.QuoteToken;
        controller.ModelState.SetModelValue(nameof(initial.CheckoutKey),oldKey.ToString(),oldKey.ToString());
        controller.ModelState.SetModelValue(nameof(initial.QuoteToken),oldToken,oldToken);
        controller.ModelState.AddModelError(nameof(initial.Name),"Name is required.");
        var revised = Assert.IsType<CheckoutModel>(Assert.IsType<ViewResult>(await controller.Checkout(initial)).Model);
        Assert.NotEqual(oldKey,revised.CheckoutKey);
        Assert.NotEqual(oldToken,revised.QuoteToken);
        Assert.False(controller.ModelState.ContainsKey(nameof(initial.CheckoutKey)));
        Assert.False(controller.ModelState.ContainsKey(nameof(initial.QuoteToken)));
        Assert.Equal(150m,revised.TotalAmount);
    }
}
