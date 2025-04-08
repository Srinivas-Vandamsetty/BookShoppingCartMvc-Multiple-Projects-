using Xunit;
using Moq;
using Microsoft.AspNetCore.Mvc;
using System.Threading.Tasks;
using System.Collections.Generic;
using BookShoppingCart.WebAPI.Controllers;
using BookShoppingCart.Business.Services;
using BookShoppingCart.Models.Models;

namespace BookShoppingCart.Tests.Controllers
{
    public class HomeControllerTests
    {
        private readonly Mock<IHomeService> _homeServiceMock;
        private readonly HomeController _homeController;

        public HomeControllerTests()
        {
            _homeServiceMock = new Mock<IHomeService>();
            _homeController = new HomeController(_homeServiceMock.Object);
        }

        [Fact]
        public async Task GetBooks_ReturnsOk_WithBooks()
        {
            // Arrange: Create dummy book list
            var books = new List<Book> { new Book { BookName = "Test Book", GenreId = 1 } };
            _homeServiceMock.Setup(s => s.GetBooks(It.IsAny<string>(), It.IsAny<int>()))
                            .ReturnsAsync(books);

            // Act: Call the GetBooks method
            var result = await _homeController.GetBooks("Test", 1);

            // Assert: Ensure it returns 200 OK with books
            var okResult = Assert.IsType<OkObjectResult>(result);
            Assert.Equal(200, okResult.StatusCode);
            Assert.Equal(books, okResult.Value);
        }

        [Fact]
        public async Task GetBooks_ReturnsNotFound_WhenNoBooks()
        {
            // Arrange: Mock empty book list
            _homeServiceMock.Setup(s => s.GetBooks(It.IsAny<string>(), It.IsAny<int>()))
                            .ReturnsAsync((List<Book>)null);

            // Act
            var result = await _homeController.GetBooks("Test", 1);

            // Assert
            var notFoundResult = Assert.IsType<NotFoundObjectResult>(result);
            Assert.Equal(404, notFoundResult.StatusCode);
            Assert.Equal("No books found.", notFoundResult.Value);
        }

        [Fact]
        public async Task GetGenres_ReturnsOk_WithGenres()
        {
            // Arrange: Create dummy genres
            var genres = new List<Genre> { new Genre { Id = 1, GenreName = "Fiction" } };
            _homeServiceMock.Setup(s => s.GetGenres()).ReturnsAsync(genres);

            // Act
            var result = await _homeController.GetGenres();

            // Assert
            var okResult = Assert.IsType<OkObjectResult>(result);
            Assert.Equal(200, okResult.StatusCode);
            Assert.Equal(genres, okResult.Value);
        }

        [Fact]
        public async Task GetGenres_ReturnsNotFound_WhenNoGenres()
        {
            // Arrange: Mock no genres
            _homeServiceMock.Setup(s => s.GetGenres()).ReturnsAsync((List<Genre>)null);

            // Act
            var result = await _homeController.GetGenres();

            // Assert
            var notFoundResult = Assert.IsType<NotFoundObjectResult>(result);
            Assert.Equal(404, notFoundResult.StatusCode);
            Assert.Equal("No genres found.", notFoundResult.Value);
        }
    }
}
