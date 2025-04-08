using BookShoppingCart.Data.Data;
using BookShoppingCart.Models.Models;
using Microsoft.EntityFrameworkCore;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace BookShoppingCart.Data.Repositories
{
    // Repository for managing book-related database operations
    public class BookRepository : BaseRepository<Book>, IBookRepository
    {
        public BookRepository(ApplicationDbContext context) : base(context) { }

        // Retrieves books along with their associated genres
        public async Task<IEnumerable<Book>> GetBooksWithGenres()
        {
            return await _context.Books.Include(b => b.Genre).ToListAsync();
        }
    }
}
