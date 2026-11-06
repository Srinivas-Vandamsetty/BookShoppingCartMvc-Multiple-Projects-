using BookShoppingCart.Data.Repositories;
using BookShoppingCart.Models.Models;
using BookStoreCore.Shared;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;

namespace BookShoppingCart.Business.Services;
public class BookCatalogService(IBookRepository books, ILogger<BookCatalogService>? logger = null)
{
    private readonly ILogger<BookCatalogService> _logger = logger ?? NullLogger<BookCatalogService>.Instance;
    // Read fresh immutable database state: genres are also edited by a separate host.
    public Task<IEnumerable<Book>> GetBooks(int page = 1) => page is < 1 or > 100000 ? throw new ArgumentException("Invalid page.") : books.GetBooksWithGenres(page, 50);
    public async Task<Book> GetBookById(int id)
    {
        if (id <= 0) throw new ArgumentException("Invalid book id.");
        var book = await books.GetByIdAsync(id);
        return book is null || book.IsArchived ? throw new KeyNotFoundException("Book not found.") : book;
    }
    protected Task<bool> IsImageReferenced(string image) => books.IsImageReferenced(image);
    private static void Validate(Book book)
    {
        ArgumentNullException.ThrowIfNull(book);
        if (string.IsNullOrWhiteSpace(book.BookName) || book.BookName.Trim().Length > 40)
            throw new ArgumentException("Book name must contain 1–40 characters.");
        if (string.IsNullOrWhiteSpace(book.AuthorName) || book.AuthorName.Trim().Length > 40)
            throw new ArgumentException("Author name must contain 1–40 characters.");
        if (book.Price <= 0 || book.Price > 999999999.99m || decimal.Round(book.Price, 2) != book.Price)
            throw new ArgumentException("Price must be positive with at most two decimal places.");
        if (book.GenreId <= 0) throw new ArgumentException("Select a valid genre.");
        book.BookName = book.BookName.Trim();
        book.AuthorName = book.AuthorName.Trim();
    }
    public async Task AddBook(Book book)
    {
        Validate(book);
        await books.AddAsync(book);
        _logger.LogInformation("Created book {BookId}", book.Id);
    }
    public async Task UpdateBook(Book book)
    {
        Validate(book);
        await GetBookById(book.Id);
        await books.UpdateAsync(book);
        _logger.LogInformation("Updated book {BookId}", book.Id);
    }
    public async Task DeleteBook(int id)
    {
        var book = await GetBookById(id);
        book.IsArchived = true;
        await books.UpdateAsync(book);
        _logger.LogInformation("Archived book {BookId}; order history and assets retained", id);
    }
}

public class BookService(IBookRepository books, IFileService files, ILogger<BookService>? logger = null)
    : BookCatalogService(books, logger), IBookService
{
    public async Task DeleteUnusedImage(string image)
    {
        if (!string.IsNullOrEmpty(image) && !await IsImageReferenced(image))
        {
            try { files.DeleteFile(image); }
            catch (ArgumentException ex) { logger?.LogWarning(ex, "Refused cleanup of an invalid legacy image name"); }
        }
    }
}
