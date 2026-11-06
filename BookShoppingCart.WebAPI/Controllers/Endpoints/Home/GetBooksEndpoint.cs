using FastEndpoints.Swagger;
using BookShoppingCart.Business.Services;
using BookShoppingCart.Models.Models;
using FastEndpoints;
public class GetBooksRequest
{
    public string STerm { get; set; } = "";
    public int GenreId { get; set; }
    public int Page { get; set; } = 1;
    public int PageSize { get; set; } = 10;
}
public class GetBooksEndpoint(IHomeService home) : Endpoint<GetBooksRequest>
{
    public override void Configure()
    {
        Get("/api/home/GetBooks");
        AllowAnonymous();
        Options(o => o.RequireRateLimiting("BooksPolicy").AutoTagOverride("Books"));
    }
    public override async Task HandleAsync(GetBooksRequest req, CancellationToken ct)
    {
        var result = await home.SearchBooks(req.STerm, req.GenreId, req.Page, req.PageSize, ct);
        HttpContext.Response.Headers["X-Total-Count"] = result.TotalCount.ToString(System.Globalization.CultureInfo.InvariantCulture);
        // Retain the legacy list-shaped Book response; new clients can use GetBooksPage.
        var books = result.Items.Select(b => new Book
        {
            Id=b.Id, BookName=b.BookName, AuthorName=b.AuthorName, Price=b.Price,
            GenreId=b.GenreId, GenreName=b.GenreName, Image=b.Image, Quantity=b.Quantity
        });
        await SendOkAsync(books, ct);
    }
}
