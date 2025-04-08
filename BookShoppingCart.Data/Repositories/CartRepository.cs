using BookShoppingCart.Data.Data;
using BookShoppingCart.Models.Models;
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

        // Gets the currently logged-in user's ID.
        private string GetUserId() => _userManager.GetUserId(_httpContextAccessor.HttpContext.User);
    }
}
