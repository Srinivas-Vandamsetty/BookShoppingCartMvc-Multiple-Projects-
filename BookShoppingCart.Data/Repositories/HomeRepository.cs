using BookShoppingCart.Data.Data;
using BookShoppingCart.Models.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace BookShoppingCart.Data.Repositories
{
    public class HomeRepository : IHomeRepository
    {
        private readonly ApplicationDbContext _db;
        private readonly IMemoryCache _cache;

        public HomeRepository(ApplicationDbContext db, IMemoryCache cache)
        {
            _db = db;
            _cache = cache;
        }

        // Caches and retrieves all genres
        public async Task<IEnumerable<Genre>> Genres()
        {
            return await _cache.GetOrCreateAsync("genres_cache", entry =>
            {
                // Cache for 30 mins
                entry.AbsoluteExpirationRelativeToNow = TimeSpan.FromMinutes(30);
                return _db.Genres.AsNoTracking().ToListAsync();
            });
        }

        // Retrieves books with filters, search, and stock info
        public async Task<IEnumerable<Book>> GetBooks(string sTerm = "", int genreId = 0)
        {
            var booksQuery = from book in _db.Books.AsNoTracking()
                             join genre in _db.Genres.AsNoTracking() on book.GenreId equals genre.Id
                             join stock in _db.Stocks.AsNoTracking() on book.Id equals stock.BookId into bookStocks
                             from bookWithStock in bookStocks.DefaultIfEmpty()
                             select new Book
                             {
                                 Id = book.Id,
                                 BookName = book.BookName,
                                 AuthorName = book.AuthorName,
                                 GenreId = book.GenreId,
                                 GenreName = genre.GenreName,
                                 Price = book.Price,
                                 Image = book.Image,
                                 Quantity = bookWithStock != null ? bookWithStock.Quantity : 0
                             };

            // Apply genre filter
            if (genreId > 0)
            {
                booksQuery = booksQuery.Where(b => b.GenreId == genreId);
            }

            // Apply search filter (case-insensitive)
            if (!string.IsNullOrWhiteSpace(sTerm))
            {
                sTerm = sTerm.Trim().ToLower();

                booksQuery = booksQuery.Where(b =>
                    (b.BookName != null && b.BookName.ToLower().Contains(sTerm)) ||
                    (b.AuthorName != null && b.AuthorName.ToLower().Contains(sTerm)) ||
                    (b.GenreName != null && b.GenreName.ToLower().Contains(sTerm))
                );
            }

            return await booksQuery.ToListAsync();
        }
    }
}
