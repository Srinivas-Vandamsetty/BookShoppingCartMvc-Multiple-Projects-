using BookShoppingCart.Business.Facades;
using BookShoppingCart.Business.Services;
using BookShoppingCart.Models.Models.DTOs;
using BookStoreCore.Shared;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.Extensions.Logging;
using System.Diagnostics;

namespace BookShoppingCartMvcUI.Controllers
{
    [Authorize(Roles = nameof(Roles.Admin))]
    public class BookController : Controller
    {
        private readonly IBookFacade _bookFacade;
        private readonly IGenreService _genreService;
        private readonly ILogger<BookController> _logger;

        public BookController(
            IBookFacade bookFacade,
            IGenreService genreService,
            ILogger<BookController> logger)
        {
            _bookFacade = bookFacade;
            _genreService = genreService;
            _logger = logger;
        }

        public async Task<IActionResult> Index()
        {
            var stopwatch = Stopwatch.StartNew();

            _logger.LogInformation("Fetching book list");

            var books = (await _bookFacade.GetBooks()).ToList();

            stopwatch.Stop();

            _logger.LogInformation(
                "Fetched {Count} books in {ElapsedMilliseconds} ms",
                books.Count,
                stopwatch.ElapsedMilliseconds);

            return View(books);
        }

        public async Task<IActionResult> AddBook()
        {
            _logger.LogInformation("Loading AddBook page");

            var genres = await _genreService.GetGenres();

            var dto = new BookDTO
            {
                GenreList = genres.Select(g => new SelectListItem
                {
                    Text = g.GenreName,
                    Value = g.Id.ToString()
                })
            };

            return View(dto);
        }

        [HttpPost]
        public async Task<IActionResult> AddBook(BookDTO dto)
        {
            if (!ModelState.IsValid)
            {
                _logger.LogWarning(
                    "Invalid model state while adding book: {Errors}",
                    ModelState.Values.SelectMany(v => v.Errors)
                                     .Select(e => e.ErrorMessage));

                return View(dto);
            }

            try
            {
                await _bookFacade.AddBook(dto);

                _logger.LogInformation(
                    "Book added successfully: {BookName}",
                    dto.BookName);

                TempData["successMessage"] = "Book added successfully";
                return RedirectToAction(nameof(AddBook));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex,
                    "Error occurred while adding book: {BookName}",
                    dto.BookName);

                TempData["errorMessage"] = "Something went wrong.";
                return View(dto);
            }
        }

        public async Task<IActionResult> UpdateBook(int id)
        {
            _logger.LogInformation("Loading UpdateBook page for ID {Id}", id);

            var book = await _bookFacade.GetBookById(id);

            if (book == null)
            {
                _logger.LogWarning("Book not found for ID {Id}", id);
                TempData["errorMessage"] = "Book not found";
                return RedirectToAction(nameof(Index));
            }

            var genres = await _genreService.GetGenres();

            var dto = new BookDTO
            {
                Id = book.Id,
                BookName = book.BookName,
                AuthorName = book.AuthorName,
                GenreId = book.GenreId,
                Price = book.Price,
                Image = book.Image,
                GenreList = genres.Select(g => new SelectListItem
                {
                    Text = g.GenreName,
                    Value = g.Id.ToString(),
                    Selected = g.Id == book.GenreId
                })
            };

            return View(dto);
        }

        [HttpPost]
        public async Task<IActionResult> UpdateBook(BookDTO dto)
        {
            if (!ModelState.IsValid)
            {
                _logger.LogWarning(
                    "Invalid model state while updating book ID {Id}",
                    dto.Id);

                return View(dto);
            }

            try
            {
                await _bookFacade.UpdateBook(dto);

                _logger.LogInformation(
                    "Book updated successfully: {BookName}",
                    dto.BookName);

                TempData["successMessage"] = "Book updated successfully";
                return RedirectToAction(nameof(Index));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex,
                    "Error occurred while updating book ID {Id}",
                    dto.Id);

                TempData["errorMessage"] = "Something went wrong.";
                return View(dto);
            }
        }

        public async Task<IActionResult> DeleteBook(int id)
        {
            try
            {
                await _bookFacade.DeleteBook(id);

                _logger.LogInformation(
                    "Book deleted successfully for ID {Id}",
                    id);

                TempData["successMessage"] = "Book deleted successfully";
            }
            catch (Exception ex)
            {
                _logger.LogError(ex,
                    "Error occurred while deleting book ID {Id}",
                    id);

                TempData["errorMessage"] = "Something went wrong.";
            }

            return RedirectToAction(nameof(Index));
        }

        [AllowAnonymous]
        public async Task<IActionResult> BookDetails(int id)
        {
            var book = await _bookFacade.GetBookById(id);
            if (book == null)
            {
                return NotFound();
            }
            return View(book);
        }

        public async Task<IActionResult> CloneBook(int id)
        {
            try
            {
                await _bookFacade.CloneBook(id);

                _logger.LogInformation(
                    "Book cloned successfully for ID {Id}",
                    id);

                TempData["successMessage"] = "Book cloned successfully";
            }
            catch (Exception ex)
            {
                _logger.LogError(ex,
                    "Error occurred while cloning book ID {Id}",
                    id);

                TempData["errorMessage"] = "Something went wrong.";
            }

            return RedirectToAction(nameof(Index));
        }
    }
}