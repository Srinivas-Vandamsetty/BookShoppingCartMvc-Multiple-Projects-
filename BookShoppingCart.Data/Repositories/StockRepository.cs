using BookShoppingCart.Data.Data;
using BookShoppingCart.Models.Models.DTOs;
using BookShoppingCart.Models.Models;
using Microsoft.EntityFrameworkCore;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace BookShoppingCart.Data.Repositories
{
    public class StockRepository
    {
        private readonly ApplicationDbContext _context;

        // Injecting DbContext
        public StockRepository(ApplicationDbContext context)
        {
            _context = context;
        }

        // Get stock by book ID
        public async Task<Stock?> GetStockByBookId(int bookId)
        {
            return await _context.Stocks.FirstOrDefaultAsync(s => s.BookId == bookId);
        }

        // Add or update stock
        public async Task ManageStock(StockDTO stockToManage)
        {
            var existingStock = await GetStockByBookId(stockToManage.BookId);

            if (existingStock == null)
            {
                _context.Stocks.Add(new Stock
                {
                    BookId = stockToManage.BookId,
                    Quantity = stockToManage.Quantity
                });
            }
            else
            {
                existingStock.Quantity = stockToManage.Quantity;
            }

            await _context.SaveChangesAsync();
        }

        // Get list of all stock entries with book names (filtered by search term)
        public async Task<IEnumerable<StockDisplayModel>> GetStocks(string sTerm = "")
        {
            return await (from book in _context.Books
                          join stock in _context.Stocks
                          on book.Id equals stock.BookId into bookStockJoin
                          from bookStock in bookStockJoin.DefaultIfEmpty()
                          where string.IsNullOrWhiteSpace(sTerm) ||
                                book.BookName.ToLower().Contains(sTerm.ToLower())
                          select new StockDisplayModel
                          {
                              BookId = book.Id,
                              BookName = book.BookName,
                              Quantity = bookStock == null ? 0 : bookStock.Quantity
                          }).ToListAsync();
        }
    }
}
