using BookShoppingCart.Data.Data;
using BookShoppingCart.Models.Models;
using Microsoft.EntityFrameworkCore;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace BookShoppingCart.Data.Repositories
{
    public class HomeRepository : IHomeRepository
    {
        private readonly ApplicationDbContext _db;

        public HomeRepository(ApplicationDbContext db)
        {
            _db = db;
        }

        // Retrieves all available genres
        public async Task<IEnumerable<Genre>> Genres()
        {
            return await _db.Genres.ToListAsync();
        }

        // Retrieves books optionally filtered by search term and genreId
        public async Task<IEnumerable<Book>> GetBooks(string sTerm = "", int genreId = 0)
        {
            var booksQuery = from book in _db.Books
                             join genre in _db.Genres on book.GenreId equals genre.Id
                             join stock in _db.Stocks on book.Id equals stock.BookId into bookStocks
                             from bookWithStock in bookStocks.DefaultIfEmpty()
                             select new Book
                             {
                                 Id = book.Id,
                                 Image = book.Image,
                                 AuthorName = book.AuthorName,
                                 BookName = book.BookName,
                                 GenreId = book.GenreId,
                                 Price = book.Price,
                                 GenreName = genre.GenreName,
                                 Quantity = bookWithStock == null ? 0 : bookWithStock.Quantity
                             };

            // Apply genre filtering if a genre ID is provided greater than 0
            if (genreId > 0)
            {
                booksQuery = booksQuery.Where(book => book.GenreId == genreId);
            }


            if (!string.IsNullOrWhiteSpace(sTerm))
            {
                sTerm = sTerm.ToLower().Trim();

                booksQuery = booksQuery.Where(book =>
                    (book.BookName != null && book.BookName.ToLower().Contains(sTerm)) ||
                    (book.AuthorName != null && book.AuthorName.ToLower().Contains(sTerm)) ||
                    (book.GenreName != null && book.GenreName.ToLower().Contains(sTerm))
                );
            }

            return await booksQuery.ToListAsync();
        }
    }
}
