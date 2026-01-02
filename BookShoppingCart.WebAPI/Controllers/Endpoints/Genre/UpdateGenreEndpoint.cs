using BookShoppingCart.Business.Services;
using BookShoppingCart.Models.Models;
using FastEndpoints;

public class UpdateGenreEndpoint : Endpoint<Genre>
{
    private readonly IGenreService _genreService;

    public UpdateGenreEndpoint(IGenreService genreService)
    {
        _genreService = genreService;
    }

    public override void Configure()
    {
        Put("/api/genre/UpdateGenre");
        AllowAnonymous();
    }

    public override async Task HandleAsync(Genre genre, CancellationToken ct)
    {
        if (!ValidationFailed)
        {
            await _genreService.UpdateGenre(genre);
            await SendNoContentAsync(ct);
        }
    }
}
