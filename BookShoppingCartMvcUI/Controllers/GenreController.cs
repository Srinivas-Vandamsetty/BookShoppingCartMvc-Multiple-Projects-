using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using System.Text;
using System.Text.Json;
using System.Net.Http.Json;

namespace BookShoppingCartMvcUI.Controllers
{
    [Authorize(Roles = nameof(Roles.Admin))]
    public class GenreController : Controller
    {
        private readonly HttpClient _httpClient;
        private readonly string _baseUrl;
        private readonly ILogger<GenreController> _logger;

        public GenreController(
            HttpClient httpClient,
            IConfiguration configuration,
            ILogger<GenreController> logger)
        {
            _httpClient = httpClient;
            _logger = logger;

            _baseUrl = configuration["ApiSettings:BaseUrl"]
                       ?? throw new ArgumentNullException(nameof(_baseUrl), "API base URL is not configured.");
        }

        // Display list of genres
        public async Task<IActionResult> Index()
        {
            _logger.LogInformation("Fetching genre list from API");

            try
            {
                var genres = await _httpClient.GetFromJsonAsync<IEnumerable<Genre>>(
                                $"{_baseUrl}Genre/GetGenres")
                             ?? Enumerable.Empty<Genre>();

                _logger.LogInformation("Fetched {Count} genres", genres.Count());

                return View(genres);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error occurred while fetching genres");

                TempData["errorMessage"] = "Failed to load genres.";
                return View(Enumerable.Empty<Genre>());
            }
        }

        // Load Add Genre view
        public IActionResult AddGenre()
        {
            _logger.LogInformation("Loading AddGenre page");
            return View();
        }

        // Handle adding a new genre
        [HttpPost]
        public async Task<IActionResult> AddGenre(Genre genre)
        {
            if (!ModelState.IsValid)
            {
                _logger.LogWarning("Invalid model state while adding genre");
                return View(genre);
            }

            try
            {
                _logger.LogInformation("Sending AddGenre request for {GenreName}", genre.GenreName);

                var response = await _httpClient.PostAsJsonAsync(
                    $"{_baseUrl}Genre/AddGenre", genre);

                if (response.IsSuccessStatusCode)
                {
                    _logger.LogInformation("Genre added successfully: {GenreName}", genre.GenreName);
                    TempData["successMessage"] = "Genre added successfully.";
                    return RedirectToAction(nameof(Index));
                }

                _logger.LogWarning("API failed to add genre. StatusCode: {StatusCode}",
                    response.StatusCode);

                TempData["errorMessage"] = "Failed to add genre.";
                return View(genre);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error occurred while adding genre {GenreName}", genre.GenreName);

                TempData["errorMessage"] = "Something went wrong.";
                return View(genre);
            }
        }

        // Load Update Genre view
        public async Task<IActionResult> UpdateGenre(int id)
        {
            _logger.LogInformation("Fetching genre with ID {Id} from API", id);

            try
            {
                var genre = await _httpClient.GetFromJsonAsync<Genre>(
                    $"{_baseUrl}Genre/GetGenreById/{id}");

                if (genre == null)
                {
                    _logger.LogWarning("Genre not found for ID {Id}", id);
                    TempData["errorMessage"] = $"Genre with ID {id} not found.";
                    return RedirectToAction(nameof(Index));
                }

                return View(genre);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error occurred while fetching genre ID {Id}", id);

                TempData["errorMessage"] = "Failed to load genre.";
                return RedirectToAction(nameof(Index));
            }
        }

        // Handle updating a genre
        [HttpPost]
        public async Task<IActionResult> UpdateGenre(Genre genre)
        {
            if (!ModelState.IsValid)
            {
                _logger.LogWarning("Invalid model state while updating genre ID {Id}", genre.Id);
                return View(genre);
            }

            try
            {
                _logger.LogInformation("Sending UpdateGenre request for ID {Id}", genre.Id);

                var jsonContent = new StringContent(
                    JsonSerializer.Serialize(genre),
                    Encoding.UTF8,
                    "application/json");

                var response = await _httpClient.PutAsync(
                    $"{_baseUrl}Genre/UpdateGenre",
                    jsonContent);

                if (response.IsSuccessStatusCode)
                {
                    _logger.LogInformation("Genre updated successfully for ID {Id}", genre.Id);
                    TempData["successMessage"] = "Genre updated successfully.";
                    return RedirectToAction(nameof(Index));
                }

                _logger.LogWarning("API failed to update genre ID {Id}. StatusCode: {StatusCode}",
                    genre.Id, response.StatusCode);

                TempData["errorMessage"] = "Failed to update genre.";
                return View(genre);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error occurred while updating genre ID {Id}", genre.Id);

                TempData["errorMessage"] = "Something went wrong.";
                return View(genre);
            }
        }

        // Handle deleting a genre
        public async Task<IActionResult> DeleteGenre(int id)
        {
            _logger.LogInformation("Sending DeleteGenre request for ID {Id}", id);

            try
            {
                var response = await _httpClient.DeleteAsync(
                    $"{_baseUrl}Genre/DeleteGenre/{id}");

                if (response.IsSuccessStatusCode)
                {
                    _logger.LogInformation("Genre deleted successfully for ID {Id}", id);
                    TempData["successMessage"] = "Genre deleted successfully.";
                }
                else
                {
                    _logger.LogWarning("API failed to delete genre ID {Id}. StatusCode: {StatusCode}",
                        id, response.StatusCode);

                    TempData["errorMessage"] = "Failed to delete genre.";
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error occurred while deleting genre ID {Id}", id);
                TempData["errorMessage"] = "Something went wrong.";
            }

            return RedirectToAction(nameof(Index));
        }
    }
}