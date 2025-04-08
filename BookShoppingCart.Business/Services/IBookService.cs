using BookShoppingCart.Models.Models;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace BookShoppingCart.Business.Services
{
    // Manages book operations: retrieve, add, update, and delete books 
    public interface IBookService
    {
        Task<IEnumerable<Book>> GetBooks();
        Task<Book?> GetBookById(int id);
        Task AddBook(Book book);
        Task UpdateBook(Book book);
        Task DeleteBook(int id);
    }
}
