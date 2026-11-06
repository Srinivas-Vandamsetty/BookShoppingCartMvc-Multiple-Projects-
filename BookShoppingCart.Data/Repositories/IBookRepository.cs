using BookShoppingCart.Models.Models;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace BookShoppingCart.Data.Repositories
{
    // Repository interface for book-related operations
    public interface IBookRepository : IBaseRepository<Book>
    {
        // Retrieves books along with their genres
        Task<IEnumerable<Book>> GetBooksWithGenres(int page = 1, int pageSize = 50);
        Task<bool> IsImageReferenced(string image);
    }
}
