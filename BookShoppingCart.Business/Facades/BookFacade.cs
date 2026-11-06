using BookShoppingCart.Business.Services;
using BookShoppingCart.Models.Models;
using BookShoppingCart.Models.Models.DTOs;
using BookStoreCore.Shared;
namespace BookShoppingCart.Business.Facades;
public class BookFacade(IBookService books, IFileService files) : IBookFacade
{
    public Task<IEnumerable<Book>> GetBooks(int page = 1) => books.GetBooks(page);
    public Task<Book> GetBookById(int id) => books.GetBookById(id);
    public async Task AddBook(BookDTO dto)
    {
        string? uploaded = null;
        try
        {
            if (dto.ImageFile is not null) uploaded = await files.SaveFile(dto.ImageFile, [".jpeg", ".jpg", ".png"]);
            await books.AddBook(Map(dto, uploaded ?? ""));
        }
        catch (BookWriteUnconfirmedException)
        {
            // A committed remote save may already reference this file. Let background cleanup verify it.
            throw;
        }
        catch
        {
            if (uploaded is not null) files.DeleteFile(uploaded);
            throw;
        }
    }
    public async Task UpdateBook(BookDTO dto)
    {
        var existing = await books.GetBookById(dto.Id);
        var oldImage = existing.Image; // Trust persisted ownership, never a hidden form field.
        string? uploaded = null;
        try
        {
            if (dto.ImageFile is not null) uploaded = await files.SaveFile(dto.ImageFile, [".jpeg", ".jpg", ".png"]);
            await books.UpdateBook(Map(dto, uploaded ?? oldImage));
        }
        catch (BookWriteUnconfirmedException)
        {
            // A committed remote save may already reference this file. Let background cleanup verify it.
            throw;
        }
        catch
        {
            if (uploaded is not null) files.DeleteFile(uploaded);
            throw;
        }
        if (uploaded is not null && !string.IsNullOrEmpty(oldImage))
            await books.DeleteUnusedImage(oldImage);
    }
    private static Book Map(BookDTO dto, string image) => new()
    {
        Id = dto.Id, BookName = dto.BookName, AuthorName = dto.AuthorName,
        GenreId = dto.GenreId, Price = dto.Price, Image = image
    };
    public Task DeleteBook(int id) => books.DeleteBook(id);
    public async Task CloneBook(int id)
    {
        var original = await books.GetBookById(id);
        var clone = original.Clone();
        const string suffix = " (Copy)";
        clone.BookName = original.BookName[..Math.Min(original.BookName.Length, 40 - suffix.Length)] + suffix;
        // Shared assets are retained until no book (including archives) references them.
        await books.AddBook(clone);
    }
}
