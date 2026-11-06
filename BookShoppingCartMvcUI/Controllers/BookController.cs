using BookShoppingCartMvcUI.Services;
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
        private readonly CommerceApiClient _api;

        public BookController(
            IBookFacade bookFacade,
            IGenreService genreService,
            ILogger<BookController> logger, CommerceApiClient api)
        {
            _bookFacade = bookFacade;
            _genreService = genreService;
            _logger = logger;
            _api = api;
        }

        private async Task<BookDTO> PopulateGenres(BookDTO dto)
        {
            dto.GenreList = (await _genreService.GetGenres()).Select(g =>
                new SelectListItem(g.GenreName, g.Id.ToString(), g.Id == dto.GenreId));
            return dto;
        }

        public async Task<IActionResult> Index(int page = 1)
        {
            var stopwatch = Stopwatch.StartNew();

            _logger.LogInformation("Fetching book list");

            var result = await _api.Get<CommercePage<BookResponse>>($"admin/books?page={page}");
            var books = result.Items;
            ViewBag.Page = page;
            ViewBag.HasNext = result.HasNext;

            stopwatch.Stop();

            _logger.LogInformation(
                "Fetched {Count} books in {ElapsedMilliseconds} ms",
                books.Count,
                stopwatch.ElapsedMilliseconds);

            return View(books.Select(b => b.ToModel()));
        }

        public async Task<IActionResult> AddBook() => View(await PopulateGenres(new BookDTO()));

        [HttpPost]
        public async Task<IActionResult> AddBook(BookDTO dto)
        {
            if (!ModelState.IsValid)
            {
                _logger.LogWarning(
                    "Invalid model state while adding book: {Errors}",
                    ModelState.Values.SelectMany(v => v.Errors)
                                     .Select(e => e.ErrorMessage));

                return View(await PopulateGenres(dto));
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

                ModelState.AddModelError("", ex is ArgumentException or CommerceApiException or BookWriteUnconfirmedException ? ex.Message : "Unable to save this book. Please check your entries.");
                TempData["errorMessage"] = "Unable to complete the book operation.";
                return View(await PopulateGenres(dto));
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

            return View(await PopulateGenres(dto));
        }

        [HttpPost]
        public async Task<IActionResult> UpdateBook(BookDTO dto)
        {
            if (!ModelState.IsValid)
            {
                _logger.LogWarning(
                    "Invalid model state while updating book ID {Id}",
                    dto.Id);

                return View(await PopulateGenres(dto));
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

                ModelState.AddModelError("", ex is CommerceApiException or BookWriteUnconfirmedException ? ex.Message : "Unable to save this book.");
                return View(await PopulateGenres(dto));
            }
        }

        [HttpPost]
        public async Task<IActionResult> DeleteBook(int id)
        {
            try
            {
                await _bookFacade.DeleteBook(id);

                _logger.LogInformation(
                    "Book deleted successfully for ID {Id}",
                    id);

                TempData["successMessage"] = "Book archived successfully.";
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
            try { return View(await _bookFacade.GetBookById(id)); }
            catch (CommerceApiException ex) when (ex.Status == System.Net.HttpStatusCode.NotFound) { return NotFound(); }
        }

        [HttpPost]
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
