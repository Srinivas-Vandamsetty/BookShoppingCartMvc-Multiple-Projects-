using BookShoppingCart.Models.Models;
using System.Threading.Tasks;

namespace BookShoppingCart.Data.Repositories
{
    // Repository interface for managing genre-related operations
    public interface IGenreRepository : IBaseRepository<Genre>
    {
        Task<bool> HasBooks(int genreId);
        Task<bool> NameExists(string name, int exceptId = 0);
    }
}
