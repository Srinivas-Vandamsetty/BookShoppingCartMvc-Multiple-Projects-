using BookShoppingCart.Data.Repositories;
using BookShoppingCart.Models.Models;
using BookShoppingCart.Models.Models.DTOs;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace BookShoppingCart.Business.Services
{
    public class StockService : IStockService
    {
        private readonly StockRepository _stockRepository;

        public StockService(StockRepository stockRepository)
        {
            _stockRepository = stockRepository;
        }

        // Get all stock items
        public async Task<IEnumerable<StockDisplayModel>> GetStocks(string sTerm = "")
        {
            if (sTerm != null && sTerm.Length > 100)
                throw new ArgumentException("Search term too long.");

            return await _stockRepository.GetStocks(sTerm);
        }

        // Get stock details by book ID
        public async Task<Stock?> GetStockByBookId(int bookId)
        {
            if (bookId <= 0)
                throw new ArgumentException("Invalid book id.");

            return await _stockRepository.GetStockByBookId(bookId);
        }

        // Add or update stock
        public async Task ManageStock(StockDTO stockToManage)
        {
            if (stockToManage == null)
                throw new ArgumentNullException(nameof(stockToManage));

            if (stockToManage.BookId <= 0)
                throw new ArgumentException("Invalid book id.");

            if (stockToManage.Quantity < 0)
                throw new ArgumentException("Stock quantity cannot be negative.");

            await _stockRepository.ManageStock(stockToManage);
        }
    }
}
