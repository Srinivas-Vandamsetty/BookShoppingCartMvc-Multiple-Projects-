using System.Net.Http.Headers;
using BookShoppingCart.Models.Models.DTOs;

namespace BookShoppingCartMvcUI.Services;

public sealed class ApiBearerHandler(IHttpContextAccessor accessor) : DelegatingHandler
{
    protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken ct)
    {
        // Cookie validation refreshes once per request. Parallel API calls share the validated result,
        // never a DbContext or mutable HttpClient default header.
        if (accessor.HttpContext?.Items[BrowserAuthenticationService.TokensKey] is AuthTokens tokens)
            request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", tokens.AccessToken);
        return base.SendAsync(request, ct);
    }
}
