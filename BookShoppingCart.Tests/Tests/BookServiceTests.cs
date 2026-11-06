using BookShoppingCart.Business.Services;
using BookShoppingCart.Data.Repositories;
using BookShoppingCart.Models.Models;
using BookStoreCore.Shared;
using Moq;

namespace BookShoppingCart.Tests.Tests
{
    public class BookServiceTests
    {
        private readonly Mock<IBookRepository> _bookRepoMock;
        private readonly Mock<IFileService> _fileServiceMock;

        private readonly BookService _bookService;

        public BookServiceTests()
        {
            _bookRepoMock = new Mock<IBookRepository>();
            _fileServiceMock = new Mock<IFileService>();

            _bookService = new BookService(
                _bookRepoMock.Object,
                _fileServiceMock.Object);
        }

        [Fact]
        public async Task GetBooks_ReturnsFreshBooksAcrossReads()
        {
            // Arrange
            var books = new List<Book>
            {
                new Book { Id = 1, BookName = "Test", AuthorName = "Author", Price = 100, GenreId = 1 }
            };

            _bookRepoMock
                .Setup(r => r.GetBooksWithGenres(1, 50))
                .ReturnsAsync(books);

            // Act
            var result = await _bookService.GetBooks();

            // Assert
            Assert.Single(result);

            // A second read must refresh database state across API-host genre changes.
            var refreshedResult = await _bookService.GetBooks();
            Assert.Single(refreshedResult);

            _bookRepoMock.Verify(r => r.GetBooksWithGenres(1, 50), Times.Exactly(2));
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
                .ReturnsAsync((Book)null!);

            await Assert.ThrowsAsync<KeyNotFoundException>(() =>
                _bookService.GetBookById(1));
        }

        [Fact]
        public async Task AddBook_ValidBook_PersistsValidatedBook()
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
                _bookService.AddBook(null!));
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
                .ReturnsAsync((Book)null!);

            await Assert.ThrowsAsync<KeyNotFoundException>(() =>
                _bookService.UpdateBook(book));
        }

        [Fact]
        public async Task DeleteBook_ValidId_ArchivesBookAndRetainsHistoryAndImage()
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
            _bookRepoMock.Verify(r => r.UpdateAsync(It.Is<Book>(b => b.IsArchived)), Times.Once);
            _bookRepoMock.Verify(r => r.DeleteAsync(It.IsAny<Book>()), Times.Never);
            _fileServiceMock.Verify(f => f.DeleteFile(It.IsAny<string>()), Times.Never);
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
                .ReturnsAsync((Book)null!);

            await Assert.ThrowsAsync<KeyNotFoundException>(() =>
                _bookService.DeleteBook(1));
        }
    }
}
