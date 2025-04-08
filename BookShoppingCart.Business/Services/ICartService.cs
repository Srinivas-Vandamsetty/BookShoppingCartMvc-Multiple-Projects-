using BookShoppingCart.Models.Models;
using BookShoppingCart.Models.Models.DTOs;
using System.Threading.Tasks;

namespace BookShoppingCart.Business.Services
{
    // Handles cart operations: add, remove, retrieve, count items, and checkout
    public interface ICartService
    {
        Task<int> AddItem(int bookId, int qty);
        Task<int> RemoveItem(int bookId);
        Task<ShoppingCart> GetUserCart();
        Task<int> GetCartItemCount(string userId = "");
    }
}
