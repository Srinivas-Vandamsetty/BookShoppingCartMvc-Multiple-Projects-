using BookShoppingCart.Models.Models.DTOs;
using BookShoppingCart.Models.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BookShoppingCart.Data.Repositories
{
    public interface IStockRepository
    {
        // // Repository interface for handling stock operations: retrieving, adding, and updating stock details
        Task<IEnumerable<StockDisplayModel>> GetStocks(string sTerm = "");
        Task<Stock?> GetStockByBookId(int bookId);
        Task ManageStock(StockDTO stockToManage);
    }
}
