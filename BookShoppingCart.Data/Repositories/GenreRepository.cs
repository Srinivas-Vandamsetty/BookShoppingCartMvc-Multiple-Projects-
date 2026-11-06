using BookShoppingCart.Data.Data;
using BookShoppingCart.Data.Repositories;
using BookShoppingCart.Models.Models;
using Microsoft.EntityFrameworkCore;
using System.Threading.Tasks;

public class GenreRepository : BaseRepository<Genre>, IGenreRepository
{


    public GenreRepository(ApplicationDbContext context) : base(context)
    {

    }

    public Task<bool> NameExists(string name, int exceptId = 0) =>
        _context.Genres.AnyAsync(g => g.Id != exceptId && g.GenreName == name);

    public async Task<bool> HasBooks(int genreId)
    {
        return await _context.Books.AnyAsync(b => b.GenreId == genreId);
    }
}
