using BookShoppingCart.Data.Repositories;
using BookShoppingCart.Models.Models;
using System.Collections.Generic;
using System.Linq;
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
