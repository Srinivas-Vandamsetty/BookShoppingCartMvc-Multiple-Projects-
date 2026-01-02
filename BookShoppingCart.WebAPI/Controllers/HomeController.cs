//using BookShoppingCart.Business.Services;
//using Microsoft.AspNetCore.Mvc;

//namespace BookShoppingCart.WebAPI.Controllers
//{
//    [Route("api/[controller]")]
//    [ApiController]
//    public class HomeController : ControllerBase
//    {
//        private readonly IHomeService _homeService;

//        public HomeController(IHomeService homeService) => _homeService = homeService;

//        // Get books based on search term and genre
//        [HttpGet("GetBooks")]
//        public async Task<IActionResult> GetBooks([FromQuery] string sTerm = "", [FromQuery] int genreId = 0)
//        {
//            var books = await _homeService.GetBooks(sTerm, genreId);
//            return books is not null ? Ok(books) : NotFound("No books found.");
//        }

//        // Get all genres
//        [HttpGet("GetGenres")]
//        public async Task<IActionResult> GetGenres()
//        {
//            var genres = await _homeService.GetGenres();
//            return genres is not null ? Ok(genres) : NotFound("No genres found.");
//        }
//    }
//}
