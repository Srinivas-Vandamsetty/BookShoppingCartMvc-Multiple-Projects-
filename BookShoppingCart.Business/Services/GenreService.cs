using BookShoppingCart.Data.Repositories;
using BookShoppingCart.Models.Models;
using System;
using System.Collections.Generic;
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
            return await _genreRepo.GetByIdAsync(id);
        }

        public async Task AddGenre(Genre genre)
        {
            await _genreRepo.AddAsync(genre);
        }

        public async Task UpdateGenre(Genre genre)
        {
            var existingGenre = await _genreRepo.GetByIdAsync(genre.Id);
            if (existingGenre == null)
            {
                throw new InvalidOperationException($"Genre with ID {genre.Id} not found.");
            }

            // Optionally update only modified fields
            existingGenre.GenreName = genre.GenreName;

            await _genreRepo.UpdateAsync(existingGenre);
        }


        public async Task DeleteGenre(int id)
        {
            var genre = await _genreRepo.GetByIdAsync(id);
            if (genre != null)
            {
                await _genreRepo.DeleteAsync(genre);
            }
            else
            {
                throw new InvalidOperationException($"Genre with ID {id} not found");
            }
        }
    }
}
