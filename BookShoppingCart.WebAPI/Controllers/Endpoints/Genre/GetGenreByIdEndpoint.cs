using BookShoppingCart.Business.Services;
using FastEndpoints;

public class GetGenreByIdRequest
{
    public int Id { get; set; }
}

public class GetGenreByIdEndpoint : Endpoint<GetGenreByIdRequest>
{
    private readonly IGenreService _genreService;

    public GetGenreByIdEndpoint(IGenreService genreService)
    {
        _genreService = genreService;
    }

    public override void Configure()
    {
        Get("/api/genre/GetGenreById/{Id}");
        AllowAnonymous();
    }

    public override async Task HandleAsync(GetGenreByIdRequest req, CancellationToken ct)
    {
        var genre = await _genreService.GetGenreById(req.Id);
        if (genre is not null)
            await SendOkAsync(genre, ct);
        else
            await SendNotFoundAsync(ct);
    }
}
