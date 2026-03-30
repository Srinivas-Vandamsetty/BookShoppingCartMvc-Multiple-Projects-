using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using System.Net.Http.Json;

namespace BookShoppingCartMvcUI.Controllers
{
    public class HomeController : Controller
    {
        private readonly HttpClient _httpClient;
        private readonly string _baseUrl;
        private readonly ILogger<HomeController> _logger;

        // Constructor
        public HomeController(
            HttpClient httpClient,
            IConfiguration configuration,
            ILogger<HomeController> logger)
        {
            _httpClient = httpClient;
            _logger = logger;

            _baseUrl = configuration["ApiSettings:BaseUrl"]
                       ?? throw new ArgumentNullException(nameof(_baseUrl), "API base URL is not configured.");
        }

        // Home Page
        public async Task<IActionResult> Index(string sTerm = "", int genreId = 0)
        {
            _logger.LogInformation(
                "Loading Home page. SearchTerm: {SearchTerm}, GenreId: {GenreId}",
                sTerm, genreId);

            try
            {
                // Fetch books
                var books = await _httpClient.GetFromJsonAsync<IEnumerable<Book>>(
                    $"{_baseUrl}Home/GetBooks?sTerm={sTerm}&genreId={genreId}")
                    ?? Enumerable.Empty<Book>();

                _logger.LogInformation("Fetched {BookCount} books", books.Count());

                // Fetch genres
                var genres = await _httpClient.GetFromJsonAsync<IEnumerable<Genre>>(
                    $"{_baseUrl}Home/GetGenres")
                    ?? Enumerable.Empty<Genre>();

                _logger.LogInformation("Fetched {GenreCount} genres", genres.Count());

                // Prepare model
                var bookModel = new BookDisplayModel
                {
                    Books = books,
                    Genres = genres,
                    STerm = sTerm,
                    GenreId = genreId
                };

                return View(bookModel);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex,
                    "Error occurred while loading Home page. SearchTerm: {SearchTerm}, GenreId: {GenreId}",
                    sTerm, genreId);

                TempData["errorMessage"] = "Failed to load books. Please try again later.";

                return View(new BookDisplayModel
                {
                    Books = Enumerable.Empty<Book>(),
                    Genres = Enumerable.Empty<Genre>(),
                    STerm = sTerm,
                    GenreId = genreId
                });
            }
        }
    }
}