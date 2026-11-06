using BookShoppingCart.Business.Services;
using BookShoppingCart.Models.Models;
namespace BookShoppingCart.Business.Proxies;
// Compatibility decorator: freshness is owned by the read service, not a second cache.
public class BookServiceProxy(IBookService books) : IBookService
{
    public Task<IEnumerable<Book>> GetBooks(int page = 1) => books.GetBooks(page);
    public Task<Book> GetBookById(int id) => books.GetBookById(id);
    public Task AddBook(Book book) => books.AddBook(book);
    public Task UpdateBook(Book book) => books.UpdateBook(book);
    public Task DeleteBook(int id) => books.DeleteBook(id);
    public Task DeleteUnusedImage(string image) => books.DeleteUnusedImage(image);
}
