using BookShoppingCart.Models.Models;
using System.Threading.Tasks;

namespace BookShoppingCart.Data.Repositories
{
    // Repository interface for managing genre-related operations
    public interface IGenreRepository : IBaseRepository<Genre>
    {
        Task<int> GetBookCount(int genreId);
        Task<IEnumerable<Genre>> GetAllWithBookCounts();
        Task<bool> NameExists(string name, int exceptId = 0);
    }
}
