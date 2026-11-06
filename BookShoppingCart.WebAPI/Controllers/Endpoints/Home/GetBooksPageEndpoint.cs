using FastEndpoints.Swagger;
using BookShoppingCart.Business.Services;
using FastEndpoints;
public class GetBooksPageRequest
{
    public string STerm { get; set; } = "";
    public int GenreId { get; set; }
    public int Page { get; set; } = 1;
    public int PageSize { get; set; } = 12;
}
public class GetBooksPageEndpoint(IHomeService home) : Endpoint<GetBooksPageRequest>
{
    public override void Configure()
    {
        Get("/api/home/GetBooksPage");
        AllowAnonymous();
        Options(o => o.RequireRateLimiting("BooksPolicy").AutoTagOverride("Books"));
    }
    public override async Task HandleAsync(GetBooksPageRequest req, CancellationToken ct) =>
        await SendOkAsync(await home.SearchBooks(req.STerm, req.GenreId, req.Page, req.PageSize, ct), ct);
}
