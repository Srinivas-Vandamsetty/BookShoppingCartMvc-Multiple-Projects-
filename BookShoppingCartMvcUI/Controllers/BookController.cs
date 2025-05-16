using BookShoppingCart.Business.Services;
using BookStoreCore.Shared;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;

namespace BookShoppingCartMvcUI.Controllers
{
    // Controller for managing books (Admin only)
    [Authorize(Roles = nameof(Roles.Admin))]
    public class BookController : Controller
    {
        private readonly IBookService _bookService;
        private readonly IGenreService _genreService;
        private readonly IFileService _fileService;

        // Constructor to inject dependencies
        public BookController(IBookService bookService, IGenreService genreService, IFileService fileService)
        {
            _bookService = bookService;
            _genreService = genreService;
            _fileService = fileService;
        }

        // Displays the list of books
        public async Task<IActionResult> Index()
        {
            var books = await _bookService.GetBooks();
            return View(books);
        }

        // Loads the Add Book view with genre options
        public async Task<IActionResult> AddBook()
        {
            var genreSelectList = (await _genreService.GetGenres()).Select(genre => new SelectListItem
            {
                Text = genre.GenreName,
                Value = genre.Id.ToString(),
            });

            BookDTO bookToAdd = new() { GenreList = genreSelectList };
            return View(bookToAdd);
        }

        // Handles adding a new book
        [HttpPost]
        public async Task<IActionResult> AddBook(BookDTO bookToAdd)
        {
            // Reload genre list for dropdown
            bookToAdd.GenreList = (await _genreService.GetGenres()).Select(genre => new SelectListItem
            {
                Text = genre.GenreName,
                Value = genre.Id.ToString(),
            });

            if (!ModelState.IsValid)
                return View(bookToAdd);

            try
            {
                // Handle image file upload
                if (bookToAdd.ImageFile != null)
                {
                    if (bookToAdd.ImageFile.Length > 1 * 1024 * 1024) // 1MB limit
                        throw new InvalidOperationException("Image file cannot exceed 1 MB");

                    string[] allowedExtensions = [".jpeg", ".jpg", ".png"];
                    bookToAdd.Image = await _fileService.SaveFile(bookToAdd.ImageFile, allowedExtensions);
                }

                // Create a book entity and save it
                Book book = new()
                {
                    BookName = bookToAdd.BookName,
                    AuthorName = bookToAdd.AuthorName,
                    Image = bookToAdd.Image,
                    GenreId = bookToAdd.GenreId,
                    Price = bookToAdd.Price
                };

                await _bookService.AddBook(book);
                TempData["successMessage"] = "Book added successfully";
                return RedirectToAction(nameof(AddBook));
            }
            catch (Exception ex)
            {
                TempData["errorMessage"] = ex.Message;
                return View(bookToAdd);
            }
        }

        // Loads the Update Book view with book details
        public async Task<IActionResult> UpdateBook(int id)
        {
            var book = await _bookService.GetBookById(id);
            if (book == null)
            {
                TempData["errorMessage"] = $"Book with ID {id} not found";
                return RedirectToAction(nameof(Index));
            }

            // Load genres and select the book's genre
            var genreSelectList = (await _genreService.GetGenres()).Select(genre => new SelectListItem
            {
                Text = genre.GenreName,
                Value = genre.Id.ToString(),
                Selected = genre.Id == book.GenreId
            });

            BookDTO bookToUpdate = new()
            {
                GenreList = genreSelectList,
                BookName = book.BookName,
                AuthorName = book.AuthorName,
                GenreId = book.GenreId,
                Price = book.Price,
                Image = book.Image
            };

            return View(bookToUpdate);
        }

        // Handles updating an existing book
        [HttpPost]
        public async Task<IActionResult> UpdateBook(BookDTO bookToUpdate)
        {
            // Reload genre list for dropdown
            bookToUpdate.GenreList = (await _genreService.GetGenres()).Select(genre => new SelectListItem
            {
                Text = genre.GenreName,
                Value = genre.Id.ToString(),
                Selected = genre.Id == bookToUpdate.GenreId
            });

            if (!ModelState.IsValid)
                return View(bookToUpdate);

            try
            {
                string oldImage = bookToUpdate.Image;

                // Handle new image file upload
                if (bookToUpdate.ImageFile != null)
                {
                    if (bookToUpdate.ImageFile.Length > 1 * 1024 * 1024) // 1MB limit
                        throw new InvalidOperationException("Image file cannot exceed 1 MB");

                    string[] allowedExtensions = [".jpeg", ".jpg", ".png"];
                    bookToUpdate.Image = await _fileService.SaveFile(bookToUpdate.ImageFile, allowedExtensions);
                }

                // Update book details
                Book book = new()
                {
                    Id = bookToUpdate.Id,
                    BookName = bookToUpdate.BookName,
                    AuthorName = bookToUpdate.AuthorName,
                    GenreId = bookToUpdate.GenreId,
                    Price = bookToUpdate.Price,
                    Image = bookToUpdate.Image
                };

                await _bookService.UpdateBook(book);

                // Delete old image if it was replaced
                if (!string.IsNullOrWhiteSpace(oldImage) && oldImage != bookToUpdate.Image)
                    _fileService.DeleteFile(oldImage);

                TempData["successMessage"] = "Book updated successfully";
                return RedirectToAction(nameof(Index));
            }
            catch (Exception ex)
            {
                TempData["errorMessage"] = ex.Message;
                return View(bookToUpdate);
            }
        }

        // Handles deleting a book
        public async Task<IActionResult> DeleteBook(int id)
        {
            try
            {
                await _bookService.DeleteBook(id);
                TempData["successMessage"] = "Book deleted successfully";
            }
            catch (Exception ex)
            {
                TempData["errorMessage"] = ex.Message;
            }

            return RedirectToAction(nameof(Index));
        }

        [AllowAnonymous]
        public async Task<IActionResult> BookDetails(int id)
        {
            var book = await _bookService.GetBookById(id);
            if (book == null)
            {
                return NotFound();
            }
            return View(book);
        }
    }
}
