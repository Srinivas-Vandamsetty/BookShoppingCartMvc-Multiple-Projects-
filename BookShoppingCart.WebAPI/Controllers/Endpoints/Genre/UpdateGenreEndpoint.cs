using FastEndpoints.Swagger;
using BookShoppingCart.Business.Services;
using BookShoppingCart.Models.Models;
using BookShoppingCart.Models.Models.DTOs;
using FastEndpoints;

public class UpdateGenreEndpoint : Endpoint<GenreDTO>
{
    private readonly IGenreService _genreService;

    public UpdateGenreEndpoint(IGenreService genreService)
    {
        _genreService = genreService;
    }

    public override void Configure()
    {
        Put("/api/genre/UpdateGenre");
        Options(o => o.AutoTagOverride("Genres"));
        Roles("Admin");
    }

    public override async Task HandleAsync(GenreDTO req, CancellationToken ct)
    {
        var genre = new Genre { Id = req.Id, GenreName = req.GenreName };
        if (!ValidationFailed)
        {
            await _genreService.UpdateGenre(genre);
            await SendNoContentAsync(ct);
        }
    }
}
