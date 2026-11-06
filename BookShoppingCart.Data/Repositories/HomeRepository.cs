using BookShoppingCart.Data.Data;
using BookShoppingCart.Models.Models;
using BookShoppingCart.Models.Models.DTOs;
using Microsoft.EntityFrameworkCore;

namespace BookShoppingCart.Data.Repositories;
public class HomeRepository(ApplicationDbContext db) : IHomeRepository
{
    public async Task<IEnumerable<Genre>> Genres() =>
        await db.Genres.AsNoTracking().OrderBy(g => g.GenreName).ToListAsync();

    public async Task<IEnumerable<Book>> GetBooks(string sTerm = "", int genreId = 0, int page = 1, int pageSize = 10)
    {
        var result = await SearchBooks(sTerm, genreId, page, pageSize);
        return result.Items.Select(b => new Book
        {
            Id = b.Id, BookName = b.BookName, AuthorName = b.AuthorName, Price = b.Price,
            Image = b.Image, GenreId = b.GenreId, GenreName = b.GenreName, Quantity = b.Quantity
        }).ToList();
    }

    public async Task<PagedBooks> SearchBooks(string sTerm = "", int genreId = 0, int page = 1, int pageSize = 12, CancellationToken ct = default)
    {
        var search = (sTerm ?? "").Trim();
        if (search.Length > 100 || genreId < 0 || page < 1 || page > 100000 || pageSize is < 1 or > 100)
            throw new ArgumentException("Invalid search or page parameters.");
        var query = db.Books.AsNoTracking().Where(b => !b.IsArchived);
        if (genreId > 0) query = query.Where(b => b.GenreId == genreId);
        if (search.Length > 0)
            query = query.Where(b => b.BookName.Contains(search) || b.AuthorName.Contains(search) || b.Genre.GenreName.Contains(search));
        var count = await query.CountAsync(ct);
        page = Math.Min(page, Math.Max(1, (count + pageSize - 1) / pageSize));
        var items = await query.OrderBy(b => b.Id).Skip((page - 1) * pageSize).Take(pageSize)
            .Select(b => new BookListDTO
            {
                Id = b.Id, BookName = b.BookName, AuthorName = b.AuthorName, Price = b.Price,
                Image = b.Image, GenreId = b.GenreId, GenreName = b.Genre.GenreName,
                Quantity = b.Stock == null ? 0 : b.Stock.Quantity
            }).ToListAsync(ct);
        return new PagedBooks { Items = items, TotalCount = count, Page = page, PageSize = pageSize };
    }
}
