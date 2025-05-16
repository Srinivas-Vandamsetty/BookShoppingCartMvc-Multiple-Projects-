using BookShoppingCart.Models.Models;
using BookShoppingCart.Models.Models.DTOs;
using System.Threading.Tasks;

namespace BookShoppingCart.Data.Repositories
{
    // Repository interface for managing shopping cart operations
    public interface ICartRepository
    {
        Task<int> AddItem(int bookId, int qty);
        Task<int> RemoveItem(int bookId);
        Task<ShoppingCart> GetUserCart();
        Task<int> GetCartItemCount(string userId = "");
        Task<ShoppingCart> GetCart(string userId);
        Task<bool> DoCheckout(CheckoutModel model);
    }
}
