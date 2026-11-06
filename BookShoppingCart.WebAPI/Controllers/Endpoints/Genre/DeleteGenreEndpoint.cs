using FastEndpoints.Swagger;
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
        Options(o => o.AutoTagOverride("Genres"));
        Roles("Admin");
    }

    public override async Task HandleAsync(DeleteGenreRequest req, CancellationToken ct)
    {
        try
        {
            await _genreService.DeleteGenre(req.Id);
            await SendNoContentAsync(ct);
        }
        catch (InvalidOperationException ex)
        {
            AddError(ex.Message);
            await SendErrorsAsync(400, ct);
        }
    }
}
