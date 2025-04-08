using BookShoppingCart.Data.Repositories;
using BookShoppingCart.Models.Models;
using BookShoppingCart.Models.Models.DTOs;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace BookShoppingCart.Business.Services
{
    public class GenreService : IGenreService
    {
        private readonly IGenreRepository _genreRepo;

        // Constructor to initialize the genre repository
        public GenreService(IGenreRepository genreRepo)
        {
            _genreRepo = genreRepo;
        }

        // Retrieve all genres from the database
        public async Task<IEnumerable<Genre>> GetGenres()
        {
            return await _genreRepo.GetAllAsync();
        }

        // Get a specific genre by its ID
        public async Task<Genre?> GetGenreById(int id)
        {
            return await _genreRepo.GetByIdAsync(id);
        }

        // Add a new genre using GenreDTO
        public async Task AddGenre(GenreDTO genreDto)
        {
            var genre = new Genre { GenreName = genreDto.GenreName, Id = genreDto.Id };
            await _genreRepo.AddAsync(genre);
        }

        // Update an existing genre using GenreDTO
        public async Task UpdateGenre(GenreDTO genreDto)
        {
            var genre = new Genre { GenreName = genreDto.GenreName, Id = genreDto.Id };
            await _genreRepo.UpdateAsync(genre);
        }

        // Delete a genre by its ID
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
