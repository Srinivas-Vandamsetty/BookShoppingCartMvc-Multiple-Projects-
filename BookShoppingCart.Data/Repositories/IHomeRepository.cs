using BookShoppingCart.Models.Models;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace BookShoppingCart.Data.Repositories
{
    // Repository interface for retrieving books and genres
    public interface IHomeRepository
    {
        Task<IEnumerable<Book>> GetBooks(string sTerm = "", int genreId = 0);
        Task<IEnumerable<Genre>> Genres();
    }

}
