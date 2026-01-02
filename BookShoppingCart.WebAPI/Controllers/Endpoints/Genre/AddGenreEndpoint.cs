using BookShoppingCart.Business.Services;
using BookShoppingCart.Models.Models;
using FastEndpoints;

public class AddGenreEndpoint : Endpoint<Genre>
{
    private readonly IGenreService _genreService;

    public AddGenreEndpoint(IGenreService genreService)
    {
        _genreService = genreService;
    }

    public override void Configure()
    {
        Post("/api/genre/AddGenre");
        AllowAnonymous();
    }

    public override async Task HandleAsync(Genre genre, CancellationToken ct)
    {
        if (!ValidationFailed)
        {
            await _genreService.AddGenre(genre);
            await SendCreatedAtAsync<GetGenreByIdEndpoint>(new { Id = genre.Id }, genre, generateAbsoluteUrl: true, cancellation: ct);
        }
    }
}
