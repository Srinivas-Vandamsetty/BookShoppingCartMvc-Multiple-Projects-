using FastEndpoints.Swagger;
using BookShoppingCart.Business.Services;
using BookShoppingCart.Models.Models;
using BookShoppingCart.Models.Models.DTOs;
using FastEndpoints;

public class AddGenreEndpoint : Endpoint<GenreDTO>
{
    private readonly IGenreService _genreService;

    public AddGenreEndpoint(IGenreService genreService)
    {
        _genreService = genreService;
    }

    public override void Configure()
    {
        Post("/api/genre/AddGenre");
        Options(o => o.AutoTagOverride("Genres"));
        Roles("Admin");
    }

    public override async Task HandleAsync(GenreDTO req, CancellationToken ct)
    {
        var genre = new Genre { Id = req.Id, GenreName = req.GenreName };
        if (!ValidationFailed)
        {
            await _genreService.AddGenre(genre);
            await SendCreatedAtAsync<GetGenreByIdEndpoint>(new { Id = genre.Id }, new GenreDTO { Id = genre.Id, GenreName = genre.GenreName }, generateAbsoluteUrl: true, cancellation: ct);
        }
    }
}
