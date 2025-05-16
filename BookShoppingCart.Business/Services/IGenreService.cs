using BookShoppingCart.Models.Models;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace BookShoppingCart.Business.Services
{
    public interface IGenreService
    {
        Task<IEnumerable<Genre>> GetGenres();
        Task<Genre?> GetGenreById(int id);
        Task AddGenre(Genre genre);
        Task UpdateGenre(Genre genre);
        Task DeleteGenre(int id);
    }
}
