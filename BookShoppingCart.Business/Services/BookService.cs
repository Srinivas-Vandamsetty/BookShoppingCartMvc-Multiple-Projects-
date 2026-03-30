using BookShoppingCart.Data.Repositories;
using BookShoppingCart.Models.Models;
using BookStoreCore.Shared;
using Microsoft.Extensions.Caching.Memory;
using System.Collections.Generic;
using System.Threading.Tasks;
using System;
using BookShoppingCart.Business.Logging;

namespace BookShoppingCart.Business.Services
{
    public class BookService : IBookService
    {
        private readonly IBookRepository _bookRepository;
        private readonly IFileService _fileService;
        private readonly IMemoryCache _cache;

        private const string BOOK_CACHE_KEY = "book_list_cache";

        public BookService(
            IBookRepository bookRepository,
            IFileService fileService,
            IMemoryCache cache)
        {
            _bookRepository = bookRepository;
            _fileService = fileService;
            _cache = cache;
        }

        // Cached Read Operation
        public async Task<IEnumerable<Book>> GetBooks()
        {
            if (!_cache.TryGetValue(BOOK_CACHE_KEY, out IEnumerable<Book> books))
            {
                AppLogger.Instance.LogInfo("Cache MISS: Fetching books from database");

                books = await _bookRepository.GetBooksWithGenres();

                _cache.Set(BOOK_CACHE_KEY, books,
                    new MemoryCacheEntryOptions
                    {
                        AbsoluteExpirationRelativeToNow = TimeSpan.FromMinutes(5)
                    });

                AppLogger.Instance.LogInfo("Books cached successfully");
            }
            else
            {
                AppLogger.Instance.LogInfo("Cache HIT: Returning books from cache");
            }

            return books;
        }

        public async Task<Book> GetBookById(int id)
        {
            if (id <= 0)
            {
                AppLogger.Instance.LogError("Invalid book ID provided");
                throw new ArgumentException("Invalid book id.");
            }

            var book = await _bookRepository.GetByIdAsync(id);

            if (book == null)
            {
                AppLogger.Instance.LogError($"Book not found for ID: {id}");
                throw new KeyNotFoundException("Book not found.");
            }

            AppLogger.Instance.LogInfo($"Book retrieved: {book.BookName}");

            return book;
        }

        public async Task AddBook(Book book)
        {
            if (book == null)
            {
                AppLogger.Instance.LogError("AddBook failed: Book is null");
                throw new ArgumentNullException(nameof(book));
            }

            ValidateBook(book);

            await _bookRepository.AddAsync(book);

            AppLogger.Instance.LogInfo($"Book added: {book.BookName}");

            // Invalidate cache
            _cache.Remove(BOOK_CACHE_KEY);
            AppLogger.Instance.LogInfo("Cache invalidated after adding book");
        }

        public async Task UpdateBook(Book book)
        {
            if (book == null)
            {
                AppLogger.Instance.LogError("UpdateBook failed: Book is null");
                throw new ArgumentNullException(nameof(book));
            }

            if (book.Id <= 0)
            {
                AppLogger.Instance.LogError("Invalid book ID for update");
                throw new ArgumentException("Invalid book id.");
            }

            ValidateBook(book);

            var existingBook = await _bookRepository.GetByIdAsync(book.Id);

            if (existingBook == null)
            {
                AppLogger.Instance.LogError($"Update failed: Book not found (ID: {book.Id})");
                throw new KeyNotFoundException("Book not found.");
            }

            await _bookRepository.UpdateAsync(book);

            AppLogger.Instance.LogInfo($"Book updated: {book.BookName}");

            // Invalidate cache
            _cache.Remove(BOOK_CACHE_KEY);
            AppLogger.Instance.LogInfo("Cache invalidated after updating book");
        }

        public async Task DeleteBook(int id)
        {
            if (id <= 0)
            {
                AppLogger.Instance.LogError("Invalid book ID for delete");
                throw new ArgumentException("Invalid book id.");
            }

            var book = await _bookRepository.GetByIdAsync(id);

            if (book == null)
            {
                AppLogger.Instance.LogError($"Delete failed: Book not found (ID: {id})");
                throw new KeyNotFoundException("Book not found.");
            }

            await _bookRepository.DeleteAsync(book);

            AppLogger.Instance.LogInfo($"Book deleted: {book.BookName}");

            if (!string.IsNullOrWhiteSpace(book.Image))
            {
                _fileService.DeleteFile(book.Image);
                AppLogger.Instance.LogInfo("Book image deleted from storage");
            }

            // Invalidate cache
            _cache.Remove(BOOK_CACHE_KEY);
            AppLogger.Instance.LogInfo("Cache invalidated after deleting book");
        }

        private void ValidateBook(Book book)
        {
            if (string.IsNullOrWhiteSpace(book.BookName))
            {
                AppLogger.Instance.LogError("Validation failed: Book name is required");
                throw new ArgumentException("Book name is required.");
            }

            if (string.IsNullOrWhiteSpace(book.AuthorName))
            {
                AppLogger.Instance.LogError("Validation failed: Author name is required");
                throw new ArgumentException("Author name is required.");
            }

            if (book.Price <= 0)
            {
                AppLogger.Instance.LogError("Validation failed: Invalid price");
                throw new ArgumentException("Price must be greater than zero.");
            }

            if (book.GenreId <= 0)
            {
                AppLogger.Instance.LogError("Validation failed: Invalid genre");
                throw new ArgumentException("Valid genre must be selected.");
            }
        }
    }
}