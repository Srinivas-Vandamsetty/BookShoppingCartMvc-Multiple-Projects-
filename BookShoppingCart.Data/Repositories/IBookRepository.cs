using BookShoppingCart.Models.Models;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace BookShoppingCart.Data.Repositories
{
    // Repository interface for book-related operations
    public interface IBookRepository : IBaseRepository<Book>
    {
        // Retrieves books along with their genres
        Task<IEnumerable<Book>> GetBooksWithGenres();
    }
}
