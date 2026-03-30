using BookShoppingCart.Business.Services;
using BookShoppingCart.Data.Repositories;
using BookShoppingCart.Models.Models;
using BookStoreCore.Shared;
using Microsoft.Extensions.Caching.Memory;
using Moq;

namespace BookShoppingCart.Tests.Tests
{
    public class BookServiceTests
    {
        private readonly Mock<IBookRepository> _bookRepoMock;
        private readonly Mock<IFileService> _fileServiceMock;
        private readonly IMemoryCache _memoryCache;

        private readonly BookService _bookService;

        public BookServiceTests()
        {
            _bookRepoMock = new Mock<IBookRepository>();
            _fileServiceMock = new Mock<IFileService>();
            _memoryCache = new MemoryCache(new MemoryCacheOptions());

            _bookService = new BookService(
                _bookRepoMock.Object,
                _fileServiceMock.Object,
                _memoryCache);
        }

        [Fact]
        public async Task GetBooks_CacheMiss_ReturnsBooksAndCaches()
        {
            // Arrange
            var books = new List<Book>
            {
                new Book { Id = 1, BookName = "Test", AuthorName = "Author", Price = 100, GenreId = 1 }
            };

            _bookRepoMock
                .Setup(r => r.GetBooksWithGenres())
                .ReturnsAsync(books);

            // Act
            var result = await _bookService.GetBooks();

            // Assert
            Assert.Single(result);

            // Call again to verify cache hit
            var cachedResult = await _bookService.GetBooks();
            Assert.Single(cachedResult);

            _bookRepoMock.Verify(r => r.GetBooksWithGenres(), Times.Once);
        }

        [Fact]
        public async Task GetBookById_InvalidId_ThrowsException()
        {
            await Assert.ThrowsAsync<ArgumentException>(() =>
                _bookService.GetBookById(0));
        }

        [Fact]
        public async Task GetBookById_NotFound_ThrowsException()
        {
            _bookRepoMock
                .Setup(r => r.GetByIdAsync(It.IsAny<int>()))
                .ReturnsAsync((Book)null);

            await Assert.ThrowsAsync<KeyNotFoundException>(() =>
                _bookService.GetBookById(1));
        }

        [Fact]
        public async Task AddBook_ValidBook_AddsAndClearsCache()
        {
            // Arrange
            var book = new Book
            {
                BookName = "Test",
                AuthorName = "Author",
                Price = 100,
                GenreId = 1
            };

            // Act
            await _bookService.AddBook(book);

            // Assert
            _bookRepoMock.Verify(r => r.AddAsync(book), Times.Once);
        }

        [Fact]
        public async Task AddBook_NullBook_ThrowsException()
        {
            await Assert.ThrowsAsync<ArgumentNullException>(() =>
                _bookService.AddBook(null));
        }

        [Fact]
        public async Task UpdateBook_InvalidId_ThrowsException()
        {
            var book = new Book
            {
                Id = 0,
                BookName = "Test",
                AuthorName = "Author",
                Price = 100,
                GenreId = 1
            };

            await Assert.ThrowsAsync<ArgumentException>(() =>
                _bookService.UpdateBook(book));
        }

        [Fact]
        public async Task UpdateBook_NotFound_ThrowsException()
        {
            var book = new Book
            {
                Id = 1,
                BookName = "Test",
                AuthorName = "Author",
                Price = 100,
                GenreId = 1
            };

            _bookRepoMock
                .Setup(r => r.GetByIdAsync(book.Id))
                .ReturnsAsync((Book)null);

            await Assert.ThrowsAsync<KeyNotFoundException>(() =>
                _bookService.UpdateBook(book));
        }

        [Fact]
        public async Task DeleteBook_ValidId_DeletesBookAndFile()
        {
            // Arrange
            var book = new Book
            {
                Id = 1,
                BookName = "Test",
                AuthorName = "Author",
                Price = 100,
                GenreId = 1,
                Image = "test.jpg"
            };

            _bookRepoMock
                .Setup(r => r.GetByIdAsync(1))
                .ReturnsAsync(book);

            // Act
            await _bookService.DeleteBook(1);

            // Assert
            _bookRepoMock.Verify(r => r.DeleteAsync(book), Times.Once);
            _fileServiceMock.Verify(f => f.DeleteFile("test.jpg"), Times.Once);
        }

        [Fact]
        public async Task DeleteBook_InvalidId_ThrowsException()
        {
            await Assert.ThrowsAsync<ArgumentException>(() =>
                _bookService.DeleteBook(0));
        }

        [Fact]
        public async Task DeleteBook_NotFound_ThrowsException()
        {
            _bookRepoMock
                .Setup(r => r.GetByIdAsync(It.IsAny<int>()))
                .ReturnsAsync((Book)null);

            await Assert.ThrowsAsync<KeyNotFoundException>(() =>
                _bookService.DeleteBook(1));
        }
    }
}
