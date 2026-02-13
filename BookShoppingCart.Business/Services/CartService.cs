using BookShoppingCart.Business.Factories;
using BookShoppingCart.Data.Repositories;
using BookShoppingCart.Models.Models;
using BookShoppingCart.Models.Models.DTOs;
using System;
using System.Threading.Tasks;

namespace BookShoppingCart.Business.Services
{
    public class CartService : ICartService
    {
        private readonly ICartRepository _cartRepo;

        public CartService(ICartRepository cartRepo)
        {
            _cartRepo = cartRepo;
        }

        // Adds a book to the shopping cart
        public async Task<int> AddItem(int bookId, int qty)
        {
            if (bookId <= 0)
                throw new ArgumentException("Invalid book id.");

            if (qty <= 0)
                throw new ArgumentException("Quantity must be greater than zero.");

            return await _cartRepo.AddItem(bookId, qty);
        }

        // Removes a book from the shopping cart
        public async Task<int> RemoveItem(int bookId)
        {
            if (bookId <= 0)
                throw new ArgumentException("Invalid book id.");

            return await _cartRepo.RemoveItem(bookId);
        }

        // Retrieves the shopping cart for current user
        public async Task<ShoppingCart> GetUserCart()
        {
            var cart = await _cartRepo.GetUserCart();

            if (cart == null)
                throw new Exception("Cart not found.");

            return cart;
        }

        // Gets total cart item count
        public async Task<int> GetCartItemCount(string userId = "")
        {
            if (!string.IsNullOrWhiteSpace(userId) && userId.Length < 3)
                throw new ArgumentException("Invalid user id.");

            return await _cartRepo.GetCartItemCount(userId);
        }

        // Handles checkout process
        public async Task<bool> DoCheckout(CheckoutModel model)
        {
            if (string.IsNullOrWhiteSpace(model.PaymentMethod))
                throw new Exception("Payment method is required.");

            var paymentService = PaymentFactory.Create(model.PaymentMethod);

            bool paymentSuccess = await paymentService.ProcessPayment(model.TotalAmount);

            if (!paymentSuccess)
                throw new Exception("Payment failed.");

            return await _cartRepo.DoCheckout(model);
        }
    }
}
