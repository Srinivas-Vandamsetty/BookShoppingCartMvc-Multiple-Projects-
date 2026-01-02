using BookShoppingCart.Data.Repositories;
using BookShoppingCart.Models.Models;
using BookShoppingCart.Models.Models.DTOs;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace BookShoppingCart.Business.Services
{
    public class StockService : IStockService
    {
        private readonly StockRepository _stockRepository;

        // Constructor to inject StockRepository
        public StockService(StockRepository stockRepository)
        {
            _stockRepository = stockRepository;
        }

        // Get all stock items (with optional search term)
        public Task<IEnumerable<StockDisplayModel>> GetStocks(string sTerm = "")
        {
            return _stockRepository.GetStocks(sTerm);
        }

        // Get stock details by book ID
        public Task<Stock?> GetStockByBookId(int bookId)
        {
            return _stockRepository.GetStockByBookId(bookId);
        }

        // Add or update stock for a book
        public Task ManageStock(StockDTO stockToManage)
        {
            return _stockRepository.ManageStock(stockToManage);
        }
    }
}
