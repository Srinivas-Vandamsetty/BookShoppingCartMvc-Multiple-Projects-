using BookShoppingCart.Data.Data;
using BookShoppingCart.Models.Models;
using BookShoppingCart.Models.Models.DTOs;
using Microsoft.EntityFrameworkCore;
using System.Collections.Generic;
using System;
using System.Linq;
using System.Threading.Tasks;

namespace BookShoppingCart.Data.Repositories
{
    public class StockRepository : IStockRepository
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
            if (!await _context.Books.AnyAsync(b => b.Id == stockToManage.BookId && !b.IsArchived))
                throw new KeyNotFoundException("Book not found.");
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
                var original = existingStock.Quantity;
                var changed = await _context.Stocks.Where(s => s.Id == existingStock.Id && s.Quantity == original)
                    .ExecuteUpdateAsync(setters => setters.SetProperty(s => s.Quantity, stockToManage.Quantity));
                if (changed != 1) throw new InvalidOperationException("Stock changed. Refresh before updating.");
                _context.Entry(existingStock).State = EntityState.Detached;
            }

            await _context.SaveChangesAsync();
        }

        // Get list of all stock entries with book names (filtered by search term)
        public async Task<IEnumerable<StockDisplayModel>> GetStocks(string sTerm = "", int page = 1)
        {
            return await (from book in _context.Books.AsNoTracking().Where(b => !b.IsArchived)
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
                          }).OrderBy(s => s.BookId).Skip((page - 1) * 50).Take(51).ToListAsync();
        }
    }
}
