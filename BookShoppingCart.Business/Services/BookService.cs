using BookShoppingCart.Data.Repositories;
using BookShoppingCart.Models.Models;
using BookStoreCore.Shared;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace BookShoppingCart.Business.Services
{
    public class BookService : IBookService
    {
        private readonly IBookRepository _bookRepository;
        private readonly IFileService _fileService;

        // Constructor to initialize repository and file service
        public BookService(IBookRepository bookRepository, IFileService fileService)
        {
            _bookRepository = bookRepository;
            _fileService = fileService;
        }

        // Get all books along with their genres
        public async Task<IEnumerable<Book>> GetBooks()
        {
            return await _bookRepository.GetBooksWithGenres();
        }

        // Get a single book by its ID
        public async Task<Book?> GetBookById(int id)
        {
            return await _bookRepository.GetByIdAsync(id);
        }

        // Add a new book to the database
        public async Task AddBook(Book book)
        {
            await _bookRepository.AddAsync(book);
        }

        // Update an existing book in the database
        public async Task UpdateBook(Book book)
        {
            await _bookRepository.UpdateAsync(book);
        }

        // Delete a book and remove its image if it exists
        public async Task DeleteBook(int id)
        {
            var book = await _bookRepository.GetByIdAsync(id);
            if (book != null)
            {
                await _bookRepository.DeleteAsync(book);

                // Delete book image if it exists
                if (!string.IsNullOrWhiteSpace(book.Image))
                {
                    _fileService.DeleteFile(book.Image);
                }
            }
        }
    }
}
