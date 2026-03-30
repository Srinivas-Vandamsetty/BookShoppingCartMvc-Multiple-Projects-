using BookShoppingCart.Business.Services;
using BookShoppingCart.Models.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BookShoppingCart.Business.Proxies
{
    public class BookServiceProxy : IBookService
    {
        private readonly IBookService _bookService;
        private IEnumerable<Book> _cachedBooks;

        public BookServiceProxy(IBookService bookService)
        {
            _bookService = bookService;
        }

        public async Task<IEnumerable<Book>> GetBooks()
        {
            if (_cachedBooks != null)
            {
                Console.WriteLine("Returning books from cache");
                return _cachedBooks;
            }

            Console.WriteLine("Fetching books from database");

            _cachedBooks = await _bookService.GetBooks();
            return _cachedBooks;
        }

        // Forward other methods directly
        public async Task<Book> GetBookById(int id)
            => await _bookService.GetBookById(id);

        public async Task AddBook(Book book)
            => await _bookService.AddBook(book);

        public async Task UpdateBook(Book book)
            => await _bookService.UpdateBook(book);

        public async Task DeleteBook(int id)
            => await _bookService.DeleteBook(id);
    }
}
