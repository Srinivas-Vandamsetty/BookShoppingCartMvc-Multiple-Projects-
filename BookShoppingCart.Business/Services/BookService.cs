using BookShoppingCart.Data.Repositories;
using BookShoppingCart.Models.Models;
using BookStoreCore.Shared;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace BookShoppingCart.Business.Services
{
    public class BookService : IBookService
    {
        private readonly IBookRepository _bookRepository;
        private readonly IFileService _fileService;

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
            if (id <= 0)
                throw new ArgumentException("Invalid book id.");

            return await _bookRepository.GetByIdAsync(id);
        }

        // Add a new book to the database
        public async Task AddBook(Book book)
        {
            if (book == null)
                throw new ArgumentNullException(nameof(book));

            if (string.IsNullOrWhiteSpace(book.BookName))
                throw new Exception("Book name is required.");

            if (string.IsNullOrWhiteSpace(book.AuthorName))
                throw new Exception("Author name is required.");

            if (book.Price <= 0)
                throw new Exception("Price must be greater than zero.");

            if (book.GenreId <= 0)
                throw new Exception("Valid genre must be selected.");

            await _bookRepository.AddAsync(book);
        }

        // Update an existing book in the database
        public async Task UpdateBook(Book book)
        {
            if (book == null)
                throw new ArgumentNullException(nameof(book));

            if (book.Id <= 0)
                throw new ArgumentException("Invalid book id.");

            if (string.IsNullOrWhiteSpace(book.BookName))
                throw new Exception("Book name is required.");

            if (string.IsNullOrWhiteSpace(book.AuthorName))
                throw new Exception("Author name is required.");

            if (book.Price <= 0)
                throw new Exception("Price must be greater than zero.");

            var existingBook = await _bookRepository.GetByIdAsync(book.Id);
            if (existingBook == null)
                throw new Exception("Book not found.");

            await _bookRepository.UpdateAsync(book);
        }

        // Delete a book and remove its image if it exists
        public async Task DeleteBook(int id)
        {
            if (id <= 0)
                throw new ArgumentException("Invalid book id.");

            var book = await _bookRepository.GetByIdAsync(id);

            if (book == null)
                throw new Exception("Book not found.");

            await _bookRepository.DeleteAsync(book);

            if (!string.IsNullOrWhiteSpace(book.Image))
            {
                _fileService.DeleteFile(book.Image);
            }
        }
    }
}
