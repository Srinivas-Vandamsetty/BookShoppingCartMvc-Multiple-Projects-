using BookShoppingCart.Data.Repositories;
using BookShoppingCart.Models.Models;
using System.Threading.Tasks;

namespace BookShoppingCart.Business.Services
{
    // Provides business logic for managing shopping cart operations.
    public class CartService : ICartService
    {
        private readonly ICartRepository _cartRepo;

        // Initializes a new instance of the CartService class.
        public CartService(ICartRepository cartRepo)
        {
            _cartRepo = cartRepo;
        }

        // Adds a book to the shopping cart or increases its quantity if already present.
        public async Task<int> AddItem(int bookId, int qty)
        {
            return await _cartRepo.AddItem(bookId, qty);
        }

        // Removes a book from the shopping cart or decreases its quantity if more than one exists.
        public async Task<int> RemoveItem(int bookId)
        {
            return await _cartRepo.RemoveItem(bookId);
        }

        // Retrieves the shopping cart for the currently logged-in user.
        public async Task<ShoppingCart> GetUserCart()
        {
            return await _cartRepo.GetUserCart();
        }

        // Gets the total number of items in the shopping cart.
        public async Task<int> GetCartItemCount(string userId = "")
        {
            return await _cartRepo.GetCartItemCount(userId);
        }
    }
}
