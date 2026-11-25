using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.WebUtilities;
using System.Globalization;
namespace BookShoppingCartMvcUI.Controllers;
public class HomeController(IHttpClientFactory clients, ILogger<HomeController> logger) : Controller
{
    [AllowAnonymous]
    [HttpGet]
    public async Task<IActionResult> HeaderSearch(string term = "", CancellationToken ct = default)
    {
        var searchTerm = (term ?? string.Empty).Trim();
        if (searchTerm.Length < 2) return Ok(Array.Empty<HeaderBookSearchResult>());
        if (searchTerm.Length > 100) return BadRequest("Search term is too long.");

        try
        {
            var client = clients.CreateClient("Catalog");
            var url = QueryHelpers.AddQueryString("Home/GetBooksPage", new Dictionary<string, string?>
            {
                ["sTerm"] = searchTerm,
                ["page"] = "1",
                ["pageSize"] = "6"
            });
            var result = await client.GetFromJsonAsync<PagedBooks>(url, ct) ?? new PagedBooks();
            return Ok(result.Items.Select(book => new HeaderBookSearchResult(book.Id, book.BookName, book.AuthorName, book.Image)));
        }
        catch (HttpRequestException ex)
        {
            logger.LogWarning(ex, "Header book search failed");
            return StatusCode(StatusCodes.Status503ServiceUnavailable, "Book search is temporarily unavailable.");
        }
        catch (TaskCanceledException) when (!ct.IsCancellationRequested)
        {
            return StatusCode(StatusCodes.Status504GatewayTimeout, "Book search timed out.");
        }
    }

    public async Task<IActionResult> Index(string sTerm = "", int[]? genreIds = null, string author = "", decimal? minPrice = null, decimal? maxPrice = null,
        bool inStockOnly = false, string sort = "title", string? page = null, CancellationToken ct = default)
    {
        sTerm ??= string.Empty;
        author ??= string.Empty;
        sort = (sort ?? "title").Trim().ToLowerInvariant();
        if (sort is not ("title" or "newest" or "price-asc" or "price-desc")) sort = "title";
        if (!int.TryParse(page, out var pageNumber) || pageNumber < 1 || pageNumber > 100000) pageNumber = 1;
        var selectedGenres = (genreIds ?? []).Where(id => id > 0).Distinct().Take(25).ToArray();
        if (sTerm.Length > 100 || author.Length > 100 || minPrice is < 0 || maxPrice is < 0 || (minPrice.HasValue && maxPrice.HasValue && minPrice > maxPrice)) return BadRequest("Invalid search.");
        var model = new BookDisplayModel
        {
            STerm = sTerm, GenreIds = selectedGenres, Author = author, MinPrice = minPrice, MaxPrice = maxPrice,
            InStockOnly = inStockOnly, Sort = sort, Page = pageNumber
        };
        try
        {
            var client = clients.CreateClient("Catalog");
            var url = QueryHelpers.AddQueryString("Home/GetBooksPage", new Dictionary<string, string?>
            {
                ["sTerm"] = sTerm, ["genreIds"] = string.Join(',', selectedGenres), ["author"] = author,
                ["minPrice"] = minPrice?.ToString(CultureInfo.InvariantCulture), ["maxPrice"] = maxPrice?.ToString(CultureInfo.InvariantCulture),
                ["inStock"] = inStockOnly ? "true" : null, ["sort"] = sort,
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
            return RedirectToAction("Login", "Auth", new { returnUrl = "/Home/Index" });
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

public sealed record HeaderBookSearchResult(int Id, string BookName, string AuthorName, string Image);
