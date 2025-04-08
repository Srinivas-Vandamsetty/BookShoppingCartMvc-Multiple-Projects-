using BookShoppingCart.Models.Models;
using BookShoppingCart.Models.Models.DTOs;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace BookShoppingCart.Business.Services
{
    // Manages genre operations: retrieve, add, update, and delete genres
    public interface IGenreService
    {
        Task<IEnumerable<Genre>> GetGenres();
        Task<Genre?> GetGenreById(int id);
        Task AddGenre(GenreDTO genre);
        Task UpdateGenre(GenreDTO genre);
        Task DeleteGenre(int id);
    }
}
