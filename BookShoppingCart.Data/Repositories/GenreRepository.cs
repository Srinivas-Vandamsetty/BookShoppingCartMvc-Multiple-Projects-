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

    public Task<int> GetBookCount(int genreId) => _context.Books.CountAsync(b => b.GenreId == genreId);

    public async Task<IEnumerable<Genre>> GetAllWithBookCounts() =>
        await _context.Genres.AsNoTracking().OrderBy(genre => genre.GenreName)
            .Select(genre => new Genre
            {
                Id = genre.Id,
                GenreName = genre.GenreName,
                BookCount = genre.Books.Count()
            }).ToListAsync();
}
