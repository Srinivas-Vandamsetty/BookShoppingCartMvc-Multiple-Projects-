using BookShoppingCart.Business.Services;
using FastEndpoints;

public class GetAllGenresEndpoint : EndpointWithoutRequest
{
    private readonly IGenreService _genreService;

    public GetAllGenresEndpoint(IGenreService genreService)
    {
        _genreService = genreService;
    }

    public override void Configure()
    {
        Get("/api/genre/GetGenres");
        AllowAnonymous();
    }

    public override async Task HandleAsync(CancellationToken ct)
    {
        var genres = await _genreService.GetGenres();
        if (genres is not null)
            await SendOkAsync(genres, ct);
        else
            await SendNotFoundAsync(ct);
    }
}
