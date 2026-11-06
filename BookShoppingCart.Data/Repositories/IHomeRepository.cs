using BookShoppingCart.Models.Models;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace BookShoppingCart.Data.Repositories
{
    // Repository interface for retrieving books and genres
    public interface IHomeRepository
    {
        Task<IEnumerable<Book>> GetBooks(string sTerm = "", int genreId = 0, int page = 1, int pageSize = 10);
        Task<IEnumerable<Genre>> Genres();
        Task<BookShoppingCart.Models.Models.DTOs.PagedBooks> SearchBooks(string sTerm = "", int genreId = 0, int page = 1, int pageSize = 12, CancellationToken ct = default);
    }

}
