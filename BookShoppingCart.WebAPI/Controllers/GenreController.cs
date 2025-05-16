using BookShoppingCart.Business.Services;
using Microsoft.AspNetCore.Mvc;
using System.Threading.Tasks;
using BookShoppingCart.Models.Models;

namespace BookShoppingCart.WebAPI.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class GenreController : ControllerBase
    {
        private readonly IGenreService _genreService;

        public GenreController(IGenreService genreService)
        {
            _genreService = genreService;
        }

        // GET: api/Genre/GetGenres
        [HttpGet("GetGenres")]
        public async Task<IActionResult> GetGenres()
        {
            var genres = await _genreService.GetGenres();
            return genres is not null ? Ok(genres) : NotFound("No genres found.");
        }

        // GET: api/Genre/GetGenreById/{id}
        [HttpGet("GetGenreById/{id}")]
        public async Task<IActionResult> GetGenreById(int id)
        {
            var genre = await _genreService.GetGenreById(id);
            return genre is not null ? Ok(genre) : NotFound($"Genre with ID {id} not found.");
        }

        // POST: api/Genre/AddGenre
        [HttpPost("AddGenre")]
        public async Task<IActionResult> AddGenre([FromBody] Genre genre)
        {
            if (!ModelState.IsValid) return BadRequest(ModelState);

            await _genreService.AddGenre(genre);
            return CreatedAtAction(nameof(GetGenreById), new { id = genre.Id }, genre);
        }

        // PUT: api/Genre/UpdateGenre
        [HttpPut("UpdateGenre")]
        public async Task<IActionResult> UpdateGenre([FromBody] Genre genre)
        {
            if (!ModelState.IsValid) return BadRequest(ModelState);

            await _genreService.UpdateGenre(genre);
            return NoContent();
        }

        // DELETE: api/Genre/DeleteGenre/{id}
        [HttpDelete("DeleteGenre/{id}")]
        public async Task<IActionResult> DeleteGenre(int id)
        {
            await _genreService.DeleteGenre(id);
            return NoContent();
        }
    }
}
