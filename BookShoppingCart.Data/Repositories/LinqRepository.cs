using BookShoppingCart.Data.Data;
using Microsoft.EntityFrameworkCore;
using System;
using System.Linq;
using System.Threading.Tasks;

namespace BookShoppingCart.Data.Demo
{
    public class LinqQueryExamples
    {
        private readonly ApplicationDbContext _context;

        public LinqQueryExamples(ApplicationDbContext context)
        {
            _context = context;
        }

        public async Task RunExamplesAsync(int userId, int targetGenreId, int orderId, string searchTerm, int genreId)
        {
            // Join with Filtering and Projection
            var booksWithGenre = await (from book in _context.Books
                                        join genre in _context.Genres on book.GenreId equals genre.Id
                                        where book.Price > 500
                                        select new
                                        {
                                            book.BookName,
                                            genre.GenreName,
                                            book.Price
                                        }).ToListAsync();

            // Group By with Aggregation
            var booksCountPerGenre = await _context.Books
                .GroupBy(b => b.GenreId)
                .Select(g => new
                {
                    GenreId = g.Key,
                    BookCount = g.Count()
                }).ToListAsync();

            // Left Join (Books with optional Stock)
            var booksWithStock = await (from book in _context.Books
                                        join stock in _context.Stocks
                                        on book.Id equals stock.BookId into stockGroup
                                        from s in stockGroup.DefaultIfEmpty()
                                        select new
                                        {
                                            book.BookName,
                                            Quantity = s != null ? s.Quantity : 0
                                        }).ToListAsync();

            // Subquery / Nested Query
            var avgStock = await _context.Stocks.AverageAsync(s => s.Quantity);

            var booksAboveAvgStock = await (from book in _context.Books
                                            join stock in _context.Stocks on book.Id equals stock.BookId
                                            where stock.Quantity > avgStock
                                            select new
                                            {
                                                book.BookName,
                                                stock.Quantity
                                            }).ToListAsync();


            // SelectMany to flatten nested collections
            var allOrderedBooks = await _context.Orders
                .SelectMany(o => o.OrderDetail)
                .Select(od => od.Book)
                .Distinct()
                .ToListAsync();

            // Include with ThenInclude
            var orderWithDetails = await _context.Orders
                .Include(o => o.OrderDetail)
                .ThenInclude(od => od.Book)
                .FirstOrDefaultAsync(o => o.Id == orderId);

            // Dynamic Filtering
            var bookQuery = _context.Books.AsQueryable();

            if (!string.IsNullOrEmpty(searchTerm))
                bookQuery = bookQuery.Where(b => b.BookName.ToLower().Contains(searchTerm.Trim().ToLower()));

            if (genreId > 0)
                bookQuery = bookQuery.Where(b => b.GenreId == genreId);

            var filteredBooks = await bookQuery.ToListAsync();

            // Books ordered more than once
            var frequentlyOrderedBooks = await _context.OrderDetails
                .GroupBy(od => od.BookId)
                .Where(g => g.Count() > 1)
                .Select(g => new
                {
                    BookId = g.Key,
                    TimesOrdered = g.Count()
                }).ToListAsync();

            // Top 5 most expensive books with their genres
            var topExpensiveBooks = await _context.Books
                .OrderByDescending(b => b.Price)
                .Take(5)
                .Select(b => new
                {
                    b.BookName,
                    b.Price,
                    GenreName = b.Genre.GenreName
                }).ToListAsync();

            //GroupBy with Multiple Aggregations
            var genreStatistics = await _context.Books
                .GroupBy(b => b.GenreId)
                .Select(g => new
                {
                    GenreId = g.Key,
                    TotalBooks = g.Count(),
                    MaxPrice = g.Max(b => b.Price),
                    MinPrice = g.Min(b => b.Price),
                    AvgPrice = g.Average(b => b.Price)
                })
                .ToListAsync();

            // Conditional Projection (CASE WHEN equivalent)
            var booksWithPriceCategory = await _context.Books
                .Select(b => new
                {
                    b.BookName,
                    b.Price,
                    PriceCategory = b.Price > 1000 ? "Expensive" : "Affordable"
                })
                .ToListAsync();

            // Pagination (Skip & Take)
            int page = 2;
            int pageSize = 10;

            var pagedBooks = await _context.Books
                .OrderBy(b => b.Id)
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync();

            // Correlated Subquery
            var booksWithOrderCount = await _context.Books
                .Select(b => new
                {
                    b.BookName,
                    OrderCount = _context.OrderDetails
                        .Count(od => od.BookId == b.Id)
                })
                .ToListAsync();

            // Cross Join
            var crossJoinExample = await (
                from b in _context.Books
                from g in _context.Genres
                select new
                {
                    b.BookName,
                    g.GenreName
                })
                .ToListAsync();

            // Set Operations (Intersect)
            var expensiveBookIds = _context.Books
                .Where(b => b.Price > 500)
                .Select(b => b.Id);

            var lowStockBookIds = _context.Stocks
                .Where(s => s.Quantity < 5)
                .Select(s => s.BookId);

            var riskyBooks = await expensiveBookIds
                .Intersect(lowStockBookIds)
                .ToListAsync();

            // AsNoTracking() for Read-Only Queries
            var readOnlyBooks = await _context.Books
                .AsNoTracking()
                .Where(b => b.Price > 300)
                .ToListAsync();

            // Query Splitting (Avoid Cartesian Explosion)
            var ordersWithDetailsSplitQuery = await _context.Orders
                .Include(o => o.OrderDetail)
                .ThenInclude(od => od.Book)
                .AsSplitQuery()
                .ToListAsync();
        }
    }
}
