using Moq;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.ViewFeatures;
using BookShoppingCartMvcUI.Controllers;
using BookShoppingCart.Business.Services;
using BookShoppingCart.Models.Models.DTOs;
using BookShoppingCart.Models.Models;
using BookStoreCore.Shared;

namespace BookShoppingCart.Tests.Controllers
{
    public class BookControllerTests
    {
        private readonly Mock<IBookService> _bookServiceMock;
        private readonly Mock<IGenreService> _genreServiceMock;
        private readonly Mock<IFileService> _fileServiceMock;
        private readonly BookController _bookController;
        private readonly Mock<ITempDataDictionary> _tempDataMock;

        public BookControllerTests()
        {
            _bookServiceMock = new Mock<IBookService>();
            _genreServiceMock = new Mock<IGenreService>();
            _fileServiceMock = new Mock<IFileService>();
            _tempDataMock = new Mock<ITempDataDictionary>();

            _bookController = new BookController(_bookServiceMock.Object, _genreServiceMock.Object, _fileServiceMock.Object);
            _bookController.TempData = _tempDataMock.Object;
        }

        [Fact]
        public async Task AddBook_Post_ValidBook_RedirectsToAddBook()
        {
            // Arrange: Create test book DTO
            var bookDto = new BookDTO
            {
                BookName = "Test Book",
                AuthorName = "Test Author",
                GenreId = 1,
                Price = 100.0
            };

            _bookServiceMock.Setup(s => s.AddBook(It.IsAny<Book>())).Returns(Task.CompletedTask);

            // Act: Call AddBook method
            var result = await _bookController.AddBook(bookDto);

            // Assert: Check if it redirects to AddBook
            var redirectResult = Assert.IsType<RedirectToActionResult>(result);
            Assert.Equal("AddBook", redirectResult.ActionName);
            _tempDataMock.VerifySet(t => t["successMessage"] = "Book added successfully", Times.Once);
        }
    }
}
