using FastEndpoints.Swagger;
using BookShoppingCart.Business.Services;
using FastEndpoints;
public class GetBooksPageRequest
{
    public string STerm { get; set; } = "";
    public int GenreId { get; set; }
    public int Page { get; set; } = 1;
    public int PageSize { get; set; } = 12;
    public string? GenreIds { get; set; }
    public string? Author { get; set; }
    public decimal? MinPrice { get; set; }
    public decimal? MaxPrice { get; set; }
    public bool? InStock { get; set; }
    public string Sort { get; set; } = "id";
}
public class GetBooksPageEndpoint(IHomeService home) : Endpoint<GetBooksPageRequest>
{
    public override void Configure()
    {
        Get("/api/home/GetBooksPage");
        AllowAnonymous();
        Options(o => o.RequireRateLimiting("BooksPolicy").AutoTagOverride("Books"));
    }
    public override async Task HandleAsync(GetBooksPageRequest req, CancellationToken ct)
    {
        var genreIds = (req.GenreIds ?? string.Empty).Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
            .Select(value => int.TryParse(value, out var id) ? id : 0).Where(id => id > 0).ToArray();
        var filters = new BookShoppingCart.Models.Models.DTOs.CatalogSearchFilters
        {
            GenreIds = genreIds,
            Author = req.Author ?? string.Empty,
            MinPrice = req.MinPrice,
            MaxPrice = req.MaxPrice,
            InStock = req.InStock,
            Sort = req.Sort
        };
        await SendOkAsync(await home.SearchBooks(req.STerm, req.GenreId, req.Page, req.PageSize, filters, ct), ct);
    }
}
