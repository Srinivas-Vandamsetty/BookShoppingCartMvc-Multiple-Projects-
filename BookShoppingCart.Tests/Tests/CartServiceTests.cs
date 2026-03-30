using BookShoppingCart.Business.Services;
using BookShoppingCart.Data.Repositories;
using BookShoppingCart.Models.Models;
using BookShoppingCart.Models.Models.DTOs;
using Moq;

namespace BookShoppingCart.Tests.Tests
{
    public class CartServiceTests
    {
        private readonly Mock<ICartRepository> _cartRepoMock;
        private readonly CartService _cartService;

        public CartServiceTests()
        {
            _cartRepoMock = new Mock<ICartRepository>();
            _cartService = new CartService(_cartRepoMock.Object);
        }

        [Fact]
        public async Task AddItem_ValidInput_ReturnsResult()
        {
            _cartRepoMock
                .Setup(r => r.AddItem(1, 2))
                .ReturnsAsync(1);

            var result = await _cartService.AddItem(1, 2);

            Assert.Equal(1, result);
            _cartRepoMock.Verify(r => r.AddItem(1, 2), Times.Once);
        }

        [Fact]
        public async Task AddItem_InvalidBookId_ThrowsException()
        {
            await Assert.ThrowsAsync<ArgumentException>(() =>
                _cartService.AddItem(0, 1));
        }

        [Fact]
        public async Task AddItem_InvalidQty_ThrowsException()
        {
            await Assert.ThrowsAsync<ArgumentException>(() =>
                _cartService.AddItem(1, 0));
        }

        [Fact]
        public async Task RemoveItem_ValidId_ReturnsResult()
        {
            _cartRepoMock
                .Setup(r => r.RemoveItem(1))
                .ReturnsAsync(1);

            var result = await _cartService.RemoveItem(1);

            Assert.Equal(1, result);
        }

        [Fact]
        public async Task RemoveItem_InvalidId_ThrowsException()
        {
            await Assert.ThrowsAsync<ArgumentException>(() =>
                _cartService.RemoveItem(0));
        }

        [Fact]
        public async Task GetUserCart_Valid_ReturnsCart()
        {
            var cart = new ShoppingCart { Id = 1 };

            _cartRepoMock
                .Setup(r => r.GetUserCart())
                .ReturnsAsync(cart);

            var result = await _cartService.GetUserCart();

            Assert.Equal(1, result.Id);
        }

        [Fact]
        public async Task GetUserCart_Null_ThrowsException()
        {
            _cartRepoMock
                .Setup(r => r.GetUserCart())
                .ReturnsAsync((ShoppingCart)null);

            await Assert.ThrowsAsync<Exception>(() =>
                _cartService.GetUserCart());
        }

        [Fact]
        public async Task GetCartItemCount_ValidUser_ReturnsCount()
        {
            _cartRepoMock
                .Setup(r => r.GetCartItemCount("user123"))
                .ReturnsAsync(5);

            var result = await _cartService.GetCartItemCount("user123");

            Assert.Equal(5, result);
        }

        [Fact]
        public async Task GetCartItemCount_InvalidUser_ThrowsException()
        {
            await Assert.ThrowsAsync<ArgumentException>(() =>
                _cartService.GetCartItemCount("ab"));
        }

        [Fact]
        public async Task DoCheckout_InvalidPaymentMethod_ThrowsException()
        {
            var model = new CheckoutModel
            {
                PaymentMethod = "",
                TotalAmount = 100
            };

            await Assert.ThrowsAsync<Exception>(() =>
                _cartService.DoCheckout(model));
        }

        [Fact]
        public async Task DoCheckout_Success_ReturnsTrue()
        {
            var model = new CheckoutModel
            {
                PaymentMethod = "COD",
                TotalAmount = 100
            };

            _cartRepoMock
                .Setup(r => r.DoCheckout(model))
                .ReturnsAsync(true);

            var result = await _cartService.DoCheckout(model);

            Assert.True(result);
        }
    }
}
