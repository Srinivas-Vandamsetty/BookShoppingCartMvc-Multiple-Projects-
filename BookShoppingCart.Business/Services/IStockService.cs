using BookShoppingCart.Models.Models;
using BookShoppingCart.Models.Models.DTOs;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace BookShoppingCart.Business.Services
{
    public interface IStockService
    {
        // Service interface for handling stock operations: retrieving, adding, and updating stock details
        Task<IEnumerable<StockDisplayModel>> GetStocks(string sTerm = "");
        Task<Stock?> GetStockByBookId(int bookId);
        Task ManageStock(StockDTO stockToManage);
    }
}
