using BookShoppingCart.Data.Data;
using BookShoppingCart.Models.Models;

namespace BookShoppingCart.Data.Repositories
{
    // Repository for handling genre-related database operations
    public class GenreRepository : BaseRepository<Genre>, IGenreRepository
    {
        public GenreRepository(ApplicationDbContext context) : base(context) { }
    }
}
