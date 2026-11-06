using BookShoppingCart.Business.Services;
using BookShoppingCart.Data.Repositories;
using BookShoppingCart.Models.Models;
using BookShoppingCart.Models.Models.DTOs;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;

namespace BookShoppingCart.WebAPI.Controllers;

[ApiController, Route("api/books"), Authorize(Roles = "Admin")]
public sealed class BooksController(BookCatalogService books, IGenreService genres, CommerceOperations operations) : ControllerBase
{
    [HttpGet("{bookId:int}"), AllowAnonymous, EnableRateLimiting("BooksPolicy")]
    public async Task<BookResponse> Get(int bookId, CancellationToken ct) => (await Find(bookId, ct)).ToResponse();

    [HttpGet("{bookId:int}/availability"), AllowAnonymous, EnableRateLimiting("BooksPolicy")]
    public async Task<AvailabilityResponse> Availability(int bookId, CancellationToken ct)
    {
        var book = await Find(bookId, ct);
        var stock = book.Stock?.Quantity ?? 0;
        return new(bookId, stock, Math.Min(stock, 1000), stock > 0);
    }

    [HttpGet("/api/admin/books")]
    public async Task<CommercePage<BookResponse>> AdminList([FromQuery] int page = 1) =>
        CommercePage<BookResponse>.From((await books.GetBooks(page)).Select(b => b.ToResponse()), page);

    [HttpPost]
    public async Task<ActionResult<BookResponse>> Create(BookWriteRequest request, CancellationToken ct)
    {
        var book = await Map(request, 0);
        await books.AddBook(book);
        return CreatedAtAction(nameof(Get), new { bookId = book.Id }, (await Find(book.Id, ct)).ToResponse());
    }

    [HttpPut("{bookId:int}")]
    public async Task<BookResponse> Update(int bookId, BookWriteRequest request, CancellationToken ct)
    {
        await books.UpdateBook(await Map(request, bookId));
        return (await Find(bookId, ct)).ToResponse();
    }

    [HttpDelete("{bookId:int}")]
    public async Task<IActionResult> Archive(int bookId)
    {
        try { await books.DeleteBook(bookId); }
        catch (KeyNotFoundException) { return NoContent(); }
        return NoContent();
    }

    private async Task<Book> Find(int id, CancellationToken ct) => await operations.FindBook(id, ct)
        ?? throw new KeyNotFoundException("Book not found.");

    private async Task<Book> Map(BookWriteRequest request, int id)
    {
        // Covers are managed by the existing MVC upload workflow. Accept filenames only.
        if (request.Image.Length > 0 && (!System.Text.RegularExpressions.Regex.IsMatch(request.Image,
                @"\A[a-zA-Z0-9][a-zA-Z0-9_.-]*\.(?:jpg|jpeg|png)\z", System.Text.RegularExpressions.RegexOptions.IgnoreCase) ||
                request.Image.Contains("..", StringComparison.Ordinal)))
            throw new ArgumentException("Invalid cover filename.");
        if (await genres.GetGenreById(request.GenreId) is null) throw new ArgumentException("Select a valid genre.");
        return new Book { Id = id, BookName = request.BookName, AuthorName = request.AuthorName,
            Price = request.Price, GenreId = request.GenreId, Image = request.Image };
    }
}
