using BookShoppingCart.Data.Repositories;
using BookShoppingCart.Models.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace BookShoppingCart.Business.Services
{
    public class GenreService : IGenreService
    {
        private readonly IGenreRepository _genreRepo;

        public GenreService(IGenreRepository genreRepo)
        {
            _genreRepo = genreRepo;
        }

        public async Task<IEnumerable<Genre>> GetGenres()
        {
            return await _genreRepo.GetAllAsync();
        }

        public async Task<Genre?> GetGenreById(int id)
        {
            if (id <= 0)
                throw new ArgumentException("Invalid genre id.");

            return await _genreRepo.GetByIdAsync(id);
        }

        public async Task AddGenre(Genre genre)
        {
            if (genre == null)
                throw new ArgumentNullException(nameof(genre));

            if (string.IsNullOrWhiteSpace(genre.GenreName))
                throw new ArgumentException("Genre name is required.");

            // Optional: prevent duplicate genre names
            var existingGenres = await _genreRepo.GetAllAsync();
            if (existingGenres.Any(g =>
                g.GenreName.ToLower() == genre.GenreName.ToLower()))
            {
                throw new InvalidOperationException("Genre already exists.");
            }

            await _genreRepo.AddAsync(genre);
        }

        public async Task UpdateGenre(Genre genre)
        {
            if (genre == null)
                throw new ArgumentNullException(nameof(genre));

            if (genre.Id <= 0)
                throw new ArgumentException("Invalid genre id.");

            if (string.IsNullOrWhiteSpace(genre.GenreName))
                throw new ArgumentException("Genre name is required.");

            var existingGenre = await _genreRepo.GetByIdAsync(genre.Id);
            if (existingGenre == null)
                throw new InvalidOperationException(
                    $"Genre with ID {genre.Id} not found.");

            existingGenre.GenreName = genre.GenreName;

            await _genreRepo.UpdateAsync(existingGenre);
        }

        public async Task DeleteGenre(int id)
        {
            if (id <= 0)
                throw new ArgumentException("Invalid genre id.");

            var genre = await _genreRepo.GetByIdAsync(id);

            if (genre == null)
                throw new InvalidOperationException(
                    $"Genre with ID {id} not found");

            await _genreRepo.DeleteAsync(genre);
        }
    }
}
