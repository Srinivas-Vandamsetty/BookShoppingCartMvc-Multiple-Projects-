using BookShoppingCart.Data.Repositories;
using BookShoppingCart.Models.Models;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace BookShoppingCart.Business.Services
{
    public class HomeService : IHomeService
    {
        private readonly IHomeRepository _homeRepository;

        // Constructor to initialize the home repository
        public HomeService(IHomeRepository homeRepository)
        {
            _homeRepository = homeRepository;
        }

        public Task<BookShoppingCart.Models.Models.DTOs.PagedBooks> SearchBooks(string sTerm = "", int genreId = 0, int page = 1, int pageSize = 12, CancellationToken ct = default) =>
            _homeRepository.SearchBooks(sTerm, genreId, page, pageSize, ct);

        // Retrieve a list of books based on search term and genre filter
        public async Task<IEnumerable<Book>> GetBooks(string sTerm = "", int genreId = 0)
        {
            return await _homeRepository.GetBooks(sTerm, genreId);
        }


        // Retrieve a list of available genres
        public async Task<IEnumerable<Genre>> GetGenres()
        {
            return await _homeRepository.Genres();
        }
    }
}
