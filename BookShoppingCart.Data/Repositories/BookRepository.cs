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

        public Task<bool> IsImageReferenced(string image) => _context.Books.AnyAsync(b => b.Image == image);

        // Retrieves books along with their associated genres
        public async Task<IEnumerable<Book>> GetBooksWithGenres(int page = 1, int pageSize = 50)
        {
            return await _context.Books.AsNoTracking().Where(b => !b.IsArchived).Include(b => b.Genre).OrderBy(b => b.Id).Skip((page - 1) * pageSize).Take(pageSize + 1).ToListAsync();
        }
    }
}
