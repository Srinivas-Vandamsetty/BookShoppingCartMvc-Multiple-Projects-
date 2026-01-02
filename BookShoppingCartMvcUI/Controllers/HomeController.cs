using Microsoft.AspNetCore.Mvc;

namespace BookShoppingCartMvcUI.Controllers
{
    public class HomeController : Controller
    {
        private readonly HttpClient _httpClient;
        private readonly string _baseUrl;

        // Constructor to initialize HttpClient and API base URL
        public HomeController(HttpClient httpClient, IConfiguration configuration)
        {
            _httpClient = httpClient;
            _baseUrl = configuration["ApiSettings:BaseUrl"]
                       ?? throw new ArgumentNullException(nameof(_baseUrl), "API base URL is not configured.");
        }

        // Handles the request for the home page and fetches books and genres from the API
        public async Task<IActionResult> Index(string sTerm = "", int genreId = 0)
        {
            // Fetch books from the API based on search term and genre ID
            var books = await _httpClient.GetFromJsonAsync<IEnumerable<Book>>(
                $"{_baseUrl}Home/GetBooks?sTerm={sTerm}&genreId={genreId}")
                ?? Enumerable.Empty<Book>();

            // Fetch available genres from the API
            var genres = await _httpClient.GetFromJsonAsync<IEnumerable<Genre>>(
                $"{_baseUrl}Home/GetGenres")
                ?? Enumerable.Empty<Genre>();

            // Create a model to pass data to the view
            var bookModel = new BookDisplayModel
            {
                Books = books,
                Genres = genres,
                STerm = sTerm,
                GenreId = genreId
            };

            // Return the view with the book model
            return View(bookModel);
        }
    }
}
