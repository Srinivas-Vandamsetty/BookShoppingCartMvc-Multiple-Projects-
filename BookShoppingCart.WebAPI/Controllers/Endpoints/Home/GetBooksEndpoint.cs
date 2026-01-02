using BookShoppingCart.Business.Services;
using FastEndpoints;

public class GetBooksRequest
{
    public string STerm { get; set; } = "";
    public int GenreId { get; set; } = 0;
}

public class GetBooksEndpoint : Endpoint<GetBooksRequest>
{
    private readonly IHomeService _homeService;

    public GetBooksEndpoint(IHomeService homeService)
    {
        _homeService = homeService;
    }

    public override void Configure()
    {
        Get("/api/home/GetBooks");
        AllowAnonymous();
    }

    public override async Task HandleAsync(GetBooksRequest req, CancellationToken ct)
    {
        var books = await _homeService.GetBooks(req.STerm, req.GenreId);
        if (books is not null)
            await SendOkAsync(books, ct);
        else
            await SendNotFoundAsync(ct);
    }
}
