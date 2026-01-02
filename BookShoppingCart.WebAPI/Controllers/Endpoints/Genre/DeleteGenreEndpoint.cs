using BookShoppingCart.Business.Services;
using FastEndpoints;

public class DeleteGenreRequest
{
    public int Id { get; set; }
}

public class DeleteGenreEndpoint : Endpoint<DeleteGenreRequest>
{
    private readonly IGenreService _genreService;

    public DeleteGenreEndpoint(IGenreService genreService)
    {
        _genreService = genreService;
    }

    public override void Configure()
    {
        Delete("/api/genre/DeleteGenre/{Id}");
        AllowAnonymous();
    }

    public override async Task HandleAsync(DeleteGenreRequest req, CancellationToken ct)
    {
        await _genreService.DeleteGenre(req.Id);
        await SendNoContentAsync(ct);
    }
}
