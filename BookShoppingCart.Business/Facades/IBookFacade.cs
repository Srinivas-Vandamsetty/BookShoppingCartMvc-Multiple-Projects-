using BookShoppingCart.Models.Models.DTOs;
using BookShoppingCart.Models.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BookShoppingCart.Business.Facades
{
    public interface IBookFacade
    {
        Task<IEnumerable<Book>> GetBooks(int page = 1);
        Task<Book> GetBookById(int id);
        Task AddBook(BookDTO bookDto);
        Task UpdateBook(BookDTO bookDto);
        Task DeleteBook(int id);
        Task CloneBook(int id);
    }
}
