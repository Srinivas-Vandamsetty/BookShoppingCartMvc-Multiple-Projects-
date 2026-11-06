using BookShoppingCart.Business.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
namespace BookShoppingCartMvcUI.Controllers;

[Authorize(Roles = "Admin")]
public class GenreController(IGenreService genres) : Controller
{
    public async Task<IActionResult> Index() => View(await genres.GetGenres());
    public IActionResult AddGenre() => View(new GenreDTO());
    [HttpPost]
    public async Task<IActionResult> AddGenre(GenreDTO dto)
    {
        if (!ModelState.IsValid) return View(dto);
        try
        {
            await genres.AddGenre(new Genre { GenreName = dto.GenreName });
            TempData["successMessage"] = "Genre added.";
            return RedirectToAction(nameof(Index));
        }
        catch (Exception ex) when (ex is ArgumentException or InvalidOperationException)
        {
            ModelState.AddModelError(nameof(dto.GenreName), ex.Message);
            return View(dto);
        }
    }
    public async Task<IActionResult> UpdateGenre(int id)
    {
        var genre = await genres.GetGenreById(id);
        return genre is null ? NotFound() : View(new GenreDTO { Id = genre.Id, GenreName = genre.GenreName });
    }
    [HttpPost]
    public async Task<IActionResult> UpdateGenre(GenreDTO dto)
    {
        if (!ModelState.IsValid) return View(dto);
        try
        {
            await genres.UpdateGenre(new Genre { Id = dto.Id, GenreName = dto.GenreName });
            TempData["successMessage"] = "Genre updated.";
            return RedirectToAction(nameof(Index));
        }
        catch (Exception ex) when (ex is ArgumentException or InvalidOperationException)
        {
            ModelState.AddModelError(nameof(dto.GenreName), ex.Message);
            return View(dto);
        }
    }
    [HttpPost]
    public async Task<IActionResult> DeleteGenre(int id)
    {
        try { await genres.DeleteGenre(id); TempData["successMessage"] = "Genre deleted."; }
        catch (InvalidOperationException ex) { TempData["errorMessage"] = ex.Message; }
        return RedirectToAction(nameof(Index));
    }
}
