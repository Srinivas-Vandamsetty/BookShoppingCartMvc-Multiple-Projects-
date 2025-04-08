using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using System.Net.Http.Json;
using BookShoppingCart.Models.Models;
using System.Text.Json;
using System.Text;

namespace BookShoppingCartMvcUI.Controllers
{
    [Authorize(Roles = nameof(Roles.Admin))]
    public class GenreController : Controller
    {
        private readonly HttpClient _httpClient;
        private readonly string _baseUrl;

        public GenreController(HttpClient httpClient, IConfiguration configuration)
        {
            _httpClient = httpClient;
            _baseUrl = configuration["ApiSettings:BaseUrl"]
                       ?? throw new ArgumentNullException(nameof(_baseUrl), "API base URL is not configured.");
        }

        // Display list of genres
        public async Task<IActionResult> Index()
        {
            var genres = await _httpClient.GetFromJsonAsync<IEnumerable<Genre>>($"{_baseUrl}Genre/GetGenres")
                          ?? Enumerable.Empty<Genre>();
            return View(genres);
        }

        // Load Add Genre view
        public IActionResult AddGenre()
        {
            return View();
        }

        // Handle adding a new genre
        [HttpPost]
        public async Task<IActionResult> AddGenre(Genre genre)
        {
            if (!ModelState.IsValid)
                return View(genre);

            var response = await _httpClient.PostAsJsonAsync($"{_baseUrl}Genre/AddGenre", genre);

            if (response.IsSuccessStatusCode)
            {
                TempData["successMessage"] = "Genre added successfully.";
                return RedirectToAction(nameof(Index));
            }

            TempData["errorMessage"] = "Failed to add genre.";
            return View(genre);
        }

        // Load Update Genre view
        public async Task<IActionResult> UpdateGenre(int id)
        {
            var genre = await _httpClient.GetFromJsonAsync<Genre>($"{_baseUrl}Genre/GetGenre/{id}");

            if (genre == null)
            {
                TempData["errorMessage"] = $"Genre with ID {id} not found.";
                return RedirectToAction(nameof(Index));
            }

            return View(genre);
        }

        // Handle updating a genre
        [HttpPost]
        public async Task<IActionResult> UpdateGenre(Genre genre)
        {
            if (!ModelState.IsValid)
                return View(genre);

            var jsonContent = new StringContent(JsonSerializer.Serialize(genre), Encoding.UTF8, "application/json");
            var response = await _httpClient.PutAsync($"{_baseUrl}Genre/UpdateGenre/{genre.Id}", jsonContent);

            if (response.IsSuccessStatusCode)
            {
                TempData["successMessage"] = "Genre updated successfully.";
                return RedirectToAction(nameof(Index));
            }

            TempData["errorMessage"] = "Failed to update genre.";
            return View(genre);
        }

        // Handle deleting a genre
        public async Task<IActionResult> DeleteGenre(int id)
        {
            var response = await _httpClient.DeleteAsync($"{_baseUrl}Genre/DeleteGenre/{id}");

            if (response.IsSuccessStatusCode)
                TempData["successMessage"] = "Genre deleted successfully.";
            else
                TempData["errorMessage"] = "Failed to delete genre.";

            return RedirectToAction(nameof(Index));
        }
    }
}
