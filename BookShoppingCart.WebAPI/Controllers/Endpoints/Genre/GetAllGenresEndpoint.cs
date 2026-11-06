using FastEndpoints.Swagger;
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
        Options(o => o.AutoTagOverride("Genres"));
        AllowAnonymous();
    }

    public override async Task HandleAsync(CancellationToken ct)
    {
        var genres = await _genreService.GetGenres();
        if (genres is not null)
            await SendOkAsync(genres.Select(g => new BookShoppingCart.Models.Models.DTOs.GenreDTO { Id = g.Id, GenreName = g.GenreName }), ct);
        else
            await SendNotFoundAsync(ct);
    }
}
