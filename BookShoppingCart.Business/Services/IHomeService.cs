using BookShoppingCart.Models.Models;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace BookShoppingCart.Business.Services
{
    // Manages book and genre retrieval for the home page
    public interface IHomeService
    {
        Task<IEnumerable<Book>> GetBooks(string sTerm = "", int genreId = 0);
        Task<IEnumerable<Genre>> GetGenres();
        Task<BookShoppingCart.Models.Models.DTOs.PagedBooks> SearchBooks(string sTerm = "", int genreId = 0, int page = 1, int pageSize = 12, CancellationToken ct = default);
    }
}
