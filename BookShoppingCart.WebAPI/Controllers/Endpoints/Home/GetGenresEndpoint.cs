using BookShoppingCart.Business.Services;
using FastEndpoints;

public class GetGenresEndpoint : EndpointWithoutRequest
{
    private readonly IHomeService _homeService;

    public GetGenresEndpoint(IHomeService homeService)
    {
        _homeService = homeService;
    }

    public override void Configure()
    {
        Get("/api/home/GetGenres");
        AllowAnonymous();
    }

    public override async Task HandleAsync(CancellationToken ct)
    {
        var genres = await _homeService.GetGenres();
        if (genres is not null)
            await SendOkAsync(genres, ct);
        else
            await SendNotFoundAsync(ct);
    }
}
