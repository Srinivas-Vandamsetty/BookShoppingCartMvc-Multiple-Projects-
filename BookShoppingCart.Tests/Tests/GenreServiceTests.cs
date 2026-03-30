using BookShoppingCart.Business.Services;
using BookShoppingCart.Data.Repositories;
using BookShoppingCart.Models.Models;
using Moq;

namespace BookShoppingCart.Tests.Tests
{
    public class GenreServiceTests
    {
        private readonly Mock<IGenreRepository> _genreRepoMock;
        private readonly GenreService _genreService;

        public GenreServiceTests()
        {
            _genreRepoMock = new Mock<IGenreRepository>();
            _genreService = new GenreService(_genreRepoMock.Object);
        }

        [Fact]
        public async Task GetGenres_ReturnsAllGenres()
        {
            // Arrange
            var genres = new List<Genre>
            {
                new Genre { Id = 1, GenreName = "Fiction" }
            };

            _genreRepoMock
                .Setup(r => r.GetAllAsync())
                .ReturnsAsync(genres);

            // Act
            var result = await _genreService.GetGenres();

            // Assert
            Assert.Single(result);
        }

        [Fact]
        public async Task GetGenreById_InvalidId_ThrowsException()
        {
            await Assert.ThrowsAsync<ArgumentException>(() =>
                _genreService.GetGenreById(0));
        }

        [Fact]
        public async Task GetGenreById_ValidId_ReturnsGenre()
        {
            var genre = new Genre { Id = 1, GenreName = "Fiction" };

            _genreRepoMock
                .Setup(r => r.GetByIdAsync(1))
                .ReturnsAsync(genre);

            var result = await _genreService.GetGenreById(1);

            Assert.Equal("Fiction", result.GenreName);
        }

        [Fact]
        public async Task AddGenre_NullGenre_ThrowsException()
        {
            await Assert.ThrowsAsync<ArgumentNullException>(() =>
                _genreService.AddGenre(null));
        }

        [Fact]
        public async Task AddGenre_EmptyName_ThrowsException()
        {
            var genre = new Genre { GenreName = "" };

            await Assert.ThrowsAsync<ArgumentException>(() =>
                _genreService.AddGenre(genre));
        }

        [Fact]
        public async Task AddGenre_DuplicateName_ThrowsException()
        {
            var existingGenres = new List<Genre>
            {
                new Genre { GenreName = "Fiction" }
            };

            _genreRepoMock
                .Setup(r => r.GetAllAsync())
                .ReturnsAsync(existingGenres);

            var newGenre = new Genre { GenreName = "fiction" };

            await Assert.ThrowsAsync<InvalidOperationException>(() =>
                _genreService.AddGenre(newGenre));
        }

        [Fact]
        public async Task AddGenre_Valid_AddsGenre()
        {
            _genreRepoMock
                .Setup(r => r.GetAllAsync())
                .ReturnsAsync(new List<Genre>());

            var genre = new Genre { GenreName = "Science" };

            await _genreService.AddGenre(genre);

            _genreRepoMock.Verify(r => r.AddAsync(genre), Times.Once);
        }

        [Fact]
        public async Task UpdateGenre_Null_ThrowsException()
        {
            await Assert.ThrowsAsync<ArgumentNullException>(() =>
                _genreService.UpdateGenre(null));
        }

        [Fact]
        public async Task UpdateGenre_InvalidId_ThrowsException()
        {
            var genre = new Genre { Id = 0, GenreName = "Test" };

            await Assert.ThrowsAsync<ArgumentException>(() =>
                _genreService.UpdateGenre(genre));
        }

        [Fact]
        public async Task UpdateGenre_NotFound_ThrowsException()
        {
            var genre = new Genre { Id = 1, GenreName = "Test" };

            _genreRepoMock
                .Setup(r => r.GetByIdAsync(1))
                .ReturnsAsync((Genre)null);

            await Assert.ThrowsAsync<InvalidOperationException>(() =>
                _genreService.UpdateGenre(genre));
        }

        [Fact]
        public async Task UpdateGenre_Valid_UpdatesGenre()
        {
            var existing = new Genre { Id = 1, GenreName = "Old" };

            _genreRepoMock
                .Setup(r => r.GetByIdAsync(1))
                .ReturnsAsync(existing);

            var updated = new Genre { Id = 1, GenreName = "New" };

            await _genreService.UpdateGenre(updated);

            _genreRepoMock.Verify(r => r.UpdateAsync(It.Is<Genre>(g =>
                g.GenreName == "New")), Times.Once);
        }

        [Fact]
        public async Task DeleteGenre_InvalidId_ThrowsException()
        {
            await Assert.ThrowsAsync<ArgumentException>(() =>
                _genreService.DeleteGenre(0));
        }

        [Fact]
        public async Task DeleteGenre_NotFound_ThrowsException()
        {
            _genreRepoMock
                .Setup(r => r.GetByIdAsync(It.IsAny<int>()))
                .ReturnsAsync((Genre)null);

            await Assert.ThrowsAsync<InvalidOperationException>(() =>
                _genreService.DeleteGenre(1));
        }

        [Fact]
        public async Task DeleteGenre_Valid_DeletesGenre()
        {
            var genre = new Genre { Id = 1, GenreName = "Fiction" };

            _genreRepoMock
                .Setup(r => r.GetByIdAsync(1))
                .ReturnsAsync(genre);

            await _genreService.DeleteGenre(1);

            _genreRepoMock.Verify(r => r.DeleteAsync(genre), Times.Once);
        }
    }
}
