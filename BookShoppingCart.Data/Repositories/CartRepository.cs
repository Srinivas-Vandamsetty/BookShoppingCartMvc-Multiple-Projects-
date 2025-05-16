using BookShoppingCart.Data.Data;
using BookShoppingCart.Models.Models;
using BookShoppingCart.Models.Models.DTOs;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace BookShoppingCart.Data.Repositories
{
    // Handles database operations related to the shopping cart.
    public class CartRepository : ICartRepository
    {
        private readonly ApplicationDbContext _db;
        private readonly UserManager<IdentityUser> _userManager;
        private readonly IHttpContextAccessor _httpContextAccessor;

        // Constructor to initialize database context and user management services.
        public CartRepository(ApplicationDbContext db, IHttpContextAccessor httpContextAccessor, UserManager<IdentityUser> userManager)
        {
            _db = db;
            _userManager = userManager;
            _httpContextAccessor = httpContextAccessor;
        }

        // Adds a book to the cart or increases its quantity if it already exists.
        public async Task<int> AddItem(int bookId, int qty)
        {
            string userId = GetUserId();
            if (string.IsNullOrEmpty(userId))
                throw new UnauthorizedAccessException("User is not logged in");

            using var transaction = await _db.Database.BeginTransactionAsync();
            try
            {
                var cart = await GetCart(userId);
                if (cart == null)
                {
                    cart = new ShoppingCart { UserId = userId };
                    _db.ShoppingCarts.Add(cart);
                    await _db.SaveChangesAsync();
                }

                var cartItem = await _db.CartDetails.FirstOrDefaultAsync(a => a.ShoppingCartId == cart.Id && a.BookId == bookId);
                if (cartItem != null)
                {
                    cartItem.Quantity += qty;
                }
                else
                {
                    var book = await _db.Books.FindAsync(bookId);
                    if (book == null)
                        throw new InvalidOperationException("Book not found");

                    cartItem = new CartDetail
                    {
                        BookId = bookId,
                        ShoppingCartId = cart.Id,
                        Quantity = qty,
                        UnitPrice = book.Price
                    };
                    _db.CartDetails.Add(cartItem);
                }

                await _db.SaveChangesAsync();
                await transaction.CommitAsync();
            }
            catch (Exception)
            {
                await transaction.RollbackAsync();
                throw;
            }

            return await GetCartItemCount(userId);
        }

        // Removes a book from the cart or decreases its quantity.
        public async Task<int> RemoveItem(int bookId)
        {
            string userId = GetUserId();
            if (string.IsNullOrEmpty(userId))
                throw new UnauthorizedAccessException("User is not logged in");

            using var transaction = await _db.Database.BeginTransactionAsync();
            try
            {
                var cart = await GetCart(userId);
                if (cart == null)
                    throw new InvalidOperationException("Cart not found");

                var cartItem = await _db.CartDetails.FirstOrDefaultAsync(a => a.ShoppingCartId == cart.Id && a.BookId == bookId);
                if (cartItem == null)
                    throw new InvalidOperationException("Item not found in cart");

                if (cartItem.Quantity == 1)
                    _db.CartDetails.Remove(cartItem);
                else
                    cartItem.Quantity--;

                await _db.SaveChangesAsync();
                await transaction.CommitAsync();
            }
            catch (Exception)
            {
                await transaction.RollbackAsync();
                throw;
            }

            return await GetCartItemCount(userId);
        }

        // Retrieves the shopping cart for the currently logged-in user.
        public async Task<ShoppingCart> GetUserCart()
        {
            var userId = GetUserId();
            if (string.IsNullOrEmpty(userId))
                throw new UnauthorizedAccessException("Invalid user ID");

            var cart = await _db.ShoppingCarts
                .Include(a => a.CartDetails)
                    .ThenInclude(a => a.Book)
                        .ThenInclude(a => a.Stock)
                .Include(a => a.CartDetails)
                    .ThenInclude(a => a.Book)
                        .ThenInclude(a => a.Genre)
                .FirstOrDefaultAsync(a => a.UserId == userId);

            return cart ?? new ShoppingCart { CartDetails = new List<CartDetail>() };
        }

        // Retrieves the cart for a specific user.
        public async Task<ShoppingCart> GetCart(string userId)
        {
            var cart = await _db.ShoppingCarts
                .Include(a => a.CartDetails)
                .FirstOrDefaultAsync(x => x.UserId == userId);

            return cart ?? new ShoppingCart { UserId = userId, CartDetails = new List<CartDetail>() };
        }

        // Gets the total number of items in the shopping cart.
        public async Task<int> GetCartItemCount(string userId = "")
        {
            userId = string.IsNullOrEmpty(userId) ? GetUserId() : userId;
            if (string.IsNullOrEmpty(userId)) return 0;

            return await _db.CartDetails
                .Where(cd => cd.ShoppingCart.UserId == userId)
                .SumAsync(cd => cd.Quantity);
        }

        public async Task<bool> DoCheckout(CheckoutModel model)
        {
            // Begin a transaction to group all operations safely – if one fails, everything is rolled back
            using var transaction = await _db.Database.BeginTransactionAsync();
            try
            {
                // Get the currently logged-in user's ID
                var userId = GetUserId();
                if (string.IsNullOrEmpty(userId))
                    throw new UnauthorizedAccessException("User is not logged-in");

                // Retrieve the user's shopping cart
                var cart = await GetCart(userId);
                if (cart == null)
                    throw new InvalidOperationException("Invalid cart");

                // Fetch all cart items
                var cartDetails = await _db.CartDetails
                                           .Where(a => a.ShoppingCartId == cart.Id)
                                           .ToListAsync();
                if (cartDetails.Count == 0)
                    throw new InvalidOperationException("Cart is empty");

                // Get the order status with "Pending" status
                var pendingStatus = await _db.OrderStatuses
                                             .FirstOrDefaultAsync(s => s.StatusName == "Pending");
                if (pendingStatus == null)
                    throw new InvalidOperationException("Order status 'Pending' not found");

                // Create a new order entry
                var order = new Order
                {
                    UserId = userId,
                    CreateDate = DateTime.UtcNow,
                    Name = model.Name,
                    Email = model.Email,
                    MobileNumber = model.MobileNumber,
                    PaymentMethod = model.PaymentMethod,
                    Address = model.Address,
                    IsPaid = false,
                    OrderStatusId = pendingStatus.Id
                };
                _db.Orders.Add(order);
                await _db.SaveChangesAsync();

                // Add order details and update stock
                foreach (var item in cartDetails)
                {
                    // Create order detail record for each cart item
                    var orderDetail = new OrderDetail
                    {
                        BookId = item.BookId,
                        OrderId = order.Id,
                        Quantity = item.Quantity,
                        UnitPrice = item.UnitPrice
                    };
                    _db.OrderDetails.Add(orderDetail);

                    // Fetch current stock for the book
                    var stock = await _db.Stocks.FirstOrDefaultAsync(s => s.BookId == item.BookId);
                    if (stock == null)
                        throw new InvalidOperationException("Stock is null");

                    // Check for stock availability
                    if (item.Quantity > stock.Quantity)
                        throw new InvalidOperationException($"Only {stock.Quantity} item(s) are available in stock");

                    // Deduct ordered quantity from stock
                    stock.Quantity -= item.Quantity;
                }

                // Remove cart details after successful order placement
                _db.CartDetails.RemoveRange(cartDetails);

                // Save all changes to database
                await _db.SaveChangesAsync();

                // Commit the transaction
                await transaction.CommitAsync();

                return true;
            }
            catch (Exception)
            {
                // Rollback if any error occurs
                await transaction.RollbackAsync();
                return false;
            }
        }

        // Gets the currently logged-in user's ID.
        private string GetUserId() => _userManager.GetUserId(_httpContextAccessor.HttpContext.User);
    }
}
