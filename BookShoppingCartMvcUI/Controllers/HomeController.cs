using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.WebUtilities;
using System.Globalization;
namespace BookShoppingCartMvcUI.Controllers;
public class HomeController(IHttpClientFactory clients, ILogger<HomeController> logger) : Controller
{
    public async Task<IActionResult> Index(string sTerm = "", int genreId = 0, string? page = null, CancellationToken ct = default)
    {
        if (!int.TryParse(page, out var pageNumber) || pageNumber < 1 || pageNumber > 100000) pageNumber = 1;
        if (sTerm.Length > 100 || genreId < 0) return BadRequest("Invalid search.");
        var model = new BookDisplayModel { STerm = sTerm, GenreId = genreId, Page = pageNumber };
        try
        {
            var client = clients.CreateClient("Catalog");
            var url = QueryHelpers.AddQueryString("Home/GetBooksPage", new Dictionary<string, string?>
            {
                ["sTerm"] = sTerm, ["genreId"] = genreId.ToString(CultureInfo.InvariantCulture),
                ["page"] = pageNumber.ToString(CultureInfo.InvariantCulture), ["pageSize"] = "12"
            });
            var booksTask = client.GetFromJsonAsync<PagedBooks>(url, ct);
            var genresTask = client.GetFromJsonAsync<List<GenreDTO>>("Home/GetGenres", ct);
            await Task.WhenAll(booksTask, genresTask);
            var result = await booksTask ?? new PagedBooks();
            model.Books = result.Items.Select(b => new Book
            {
                Id = b.Id, BookName = b.BookName, AuthorName = b.AuthorName, Price = b.Price,
                GenreId = b.GenreId, GenreName = b.GenreName, Image = b.Image, Quantity = b.Quantity
            }).ToList();
            model.Genres = (await genresTask ?? []).Select(g => new Genre { Id = g.Id, GenreName = g.GenreName }).ToList();
            model.TotalCount = result.TotalCount;
            model.Page = result.Page;
        }
        catch (BookShoppingCartMvcUI.Services.AuthenticationApiException)
        {
            return RedirectToPage("/Account/Login", new { area = "Identity", returnUrl = "/Home/Index" });
        }
        catch (HttpRequestException ex)
        {
            logger.LogWarning(ex, "Catalog API request failed");
            TempData["errorMessage"] = ex.StatusCode == System.Net.HttpStatusCode.TooManyRequests ?
                "The catalog is busy. Please try again shortly." : "Unable to load books. Please try again.";
        }
        catch (System.Text.Json.JsonException ex)
        {
            logger.LogError(ex, "Invalid catalog API response");
            TempData["errorMessage"] = "The catalog response was invalid. Please retry.";
        }
        catch (TaskCanceledException) when (!ct.IsCancellationRequested)
        {
            TempData["errorMessage"] = "The catalog request timed out. Please retry.";
        }
        return View(model);
    }
}
