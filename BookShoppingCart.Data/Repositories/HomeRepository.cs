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

    public async Task<PagedBooks> SearchBooks(string sTerm = "", int genreId = 0, int page = 1, int pageSize = 12, CatalogSearchFilters? filters = null, CancellationToken ct = default)
    {
        var search = (sTerm ?? "").Trim();
        filters ??= new CatalogSearchFilters();
        var genreIds = filters.GenreIds.Where(id => id > 0).Distinct().Take(25).ToArray();
        var author = (filters.Author ?? string.Empty).Trim();
        var sort = (filters.Sort ?? "id").Trim().ToLowerInvariant();
        if (search.Length > 100 || author.Length > 100 || genreId < 0 || page < 1 || page > 100000 || pageSize is < 1 or > 100 ||
            filters.MinPrice is < 0 || filters.MaxPrice is < 0 || (filters.MinPrice.HasValue && filters.MaxPrice.HasValue && filters.MinPrice > filters.MaxPrice) ||
            sort is not ("id" or "title" or "price-asc" or "price-desc" or "newest"))
            throw new ArgumentException("Invalid search or page parameters.");
        var query = db.Books.AsNoTracking().Where(b => !b.IsArchived);
        if (genreId > 0) query = query.Where(b => b.GenreId == genreId);
        if (genreIds.Length > 0) query = query.Where(b => genreIds.Contains(b.GenreId));
        if (search.Length > 0)
            query = query.Where(b => b.BookName.Contains(search) || b.AuthorName.Contains(search) || b.Genre.GenreName.Contains(search));
        if (author.Length > 0) query = query.Where(b => b.AuthorName.Contains(author));
        if (filters.MinPrice.HasValue) query = query.Where(b => b.Price >= filters.MinPrice.Value);
        if (filters.MaxPrice.HasValue) query = query.Where(b => b.Price <= filters.MaxPrice.Value);
        if (filters.InStock == true) query = query.Where(b => b.Stock != null && b.Stock.Quantity > 0);
        var count = await query.CountAsync(ct);
        page = Math.Min(page, Math.Max(1, (count + pageSize - 1) / pageSize));
        query = sort switch
        {
            "price-asc" => query.OrderBy(b => (double)b.Price),
            "price-desc" => query.OrderByDescending(b => (double)b.Price),
            "newest" => query.OrderByDescending(b => b.Id),
            "title" => query.OrderBy(b => b.BookName).ThenBy(b => b.Id),
            _ => query.OrderBy(b => b.Id)
        };
        var items = await query.Skip((page - 1) * pageSize).Take(pageSize)
            .Select(b => new BookListDTO
            {
                Id = b.Id, BookName = b.BookName, AuthorName = b.AuthorName, Price = b.Price,
                Image = b.Image, GenreId = b.GenreId, GenreName = b.Genre.GenreName,
                Quantity = b.Stock == null ? 0 : b.Stock.Quantity
            }).ToListAsync(ct);
        return new PagedBooks { Items = items, TotalCount = count, Page = page, PageSize = pageSize };
    }
}
