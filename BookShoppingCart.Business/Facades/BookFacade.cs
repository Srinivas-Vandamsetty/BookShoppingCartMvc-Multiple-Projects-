using BookShoppingCart.Business.Services;
using BookShoppingCart.Models.Models.DTOs;
using BookShoppingCart.Models.Models;
using BookStoreCore.Shared;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BookShoppingCart.Business.Facades
{
    public class BookFacade : IBookFacade
    {
        private readonly IBookService _bookService;
        private readonly IFileService _fileService;

        public BookFacade(
            IBookService bookService,
            IFileService fileService)
        {
            _bookService = bookService;
            _fileService = fileService;
        }

        public async Task<IEnumerable<Book>> GetBooks()
        {
            return await _bookService.GetBooks();
        }

        public async Task<Book> GetBookById(int id)
        {
            return await _bookService.GetBookById(id);
        }

        public async Task AddBook(BookDTO bookDto)
        {
            // Handle Image Upload
            if (bookDto.ImageFile != null)
            {
                if (bookDto.ImageFile.Length > 1 * 1024 * 1024)
                    throw new InvalidOperationException("Image file cannot exceed 1 MB");

                string[] allowedExtensions = [".jpeg", ".jpg", ".png"];
                bookDto.Image = await _fileService.SaveFile(bookDto.ImageFile, allowedExtensions);
            }

            Book book = new()
            {
                BookName = bookDto.BookName,
                AuthorName = bookDto.AuthorName,
                GenreId = bookDto.GenreId,
                Price = bookDto.Price,
                Image = bookDto.Image
            };

            await _bookService.AddBook(book);
        }

        public async Task UpdateBook(BookDTO bookDto)
        {
            string oldImage = bookDto.Image;

            if (bookDto.ImageFile != null)
            {
                if (bookDto.ImageFile.Length > 1 * 1024 * 1024)
                    throw new InvalidOperationException("Image file cannot exceed 1 MB");

                string[] allowedExtensions = [".jpeg", ".jpg", ".png"];
                bookDto.Image = await _fileService.SaveFile(bookDto.ImageFile, allowedExtensions);
            }

            Book book = new()
            {
                Id = bookDto.Id,
                BookName = bookDto.BookName,
                AuthorName = bookDto.AuthorName,
                GenreId = bookDto.GenreId,
                Price = bookDto.Price,
                Image = bookDto.Image
            };

            await _bookService.UpdateBook(book);

            if (!string.IsNullOrWhiteSpace(oldImage) && oldImage != bookDto.Image)
                _fileService.DeleteFile(oldImage);
        }

        public async Task DeleteBook(int id)
        {
            await _bookService.DeleteBook(id);
        }

        public async Task CloneBook(int id)
        {
            var existingBook = await _bookService.GetBookById(id);

            if (existingBook == null)
                throw new Exception("Book not found");

            var clonedBook = existingBook.Clone();
            clonedBook.BookName += " (Copy)";

            await _bookService.AddBook(clonedBook);
        }
    }
}
