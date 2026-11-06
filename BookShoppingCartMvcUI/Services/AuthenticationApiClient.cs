using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text;
using System.Text.Json;
using BookShoppingCart.Models.Models.DTOs;

namespace BookShoppingCartMvcUI.Services;

public sealed class AuthenticationApiException(string message) : Exception(message);

public sealed class AuthenticationApiClient(IHttpClientFactory clients)
{
    public async Task<AuthTokens> LoginAsync(string email, string password, CancellationToken ct)
    {
        using var request = new HttpRequestMessage(HttpMethod.Post, "auth/login");
        request.Headers.Authorization = new AuthenticationHeaderValue("Basic",
            Convert.ToBase64String(Encoding.UTF8.GetBytes(email + ":" + password)));
        return await ExchangeAsync(request, ct);
    }

    public async Task<AuthTokens> RefreshAsync(string refreshToken, CancellationToken ct)
    {
        using var request = new HttpRequestMessage(HttpMethod.Post, "auth/refresh")
        { Content = JsonContent.Create(new { refreshToken }) };
        return await ExchangeAsync(request, ct);
    }

    private async Task<AuthTokens> ExchangeAsync(HttpRequestMessage request, CancellationToken ct)
    {
        using var response = await clients.CreateClient("Authentication").SendAsync(request, ct);
        if (response.StatusCode == HttpStatusCode.Unauthorized)
            throw new AuthenticationApiException(request.RequestUri!.ToString().EndsWith("login", StringComparison.Ordinal)
                ? "We couldn't sign you in. Check your email and password, confirm your email, or try again after a lockout. Two-factor accounts require an MFA sign-in flow."
                : "Your session has expired. Please sign in again.");
        if (response.StatusCode == HttpStatusCode.TooManyRequests)
            throw new AuthenticationApiException("Too many attempts. Please wait a minute and try again.");
        if (!response.IsSuccessStatusCode)
            throw new HttpRequestException("Authentication service unavailable.");
        try
        {
            var tokens = await response.Content.ReadFromJsonAsync<AuthTokens>(ct);
            if (tokens is null || string.IsNullOrWhiteSpace(tokens.AccessToken) || tokens.RefreshToken?.Length != 86 ||
                tokens.TokenType != "Bearer" || tokens.RefreshTokenExpiresAtUtc <= DateTime.UtcNow ||
                tokens.AccessTokenExpiresAtUtc <= DateTime.UtcNow)
                throw new HttpRequestException("Invalid authentication response.");
            return tokens;
        }
        catch (JsonException ex) { throw new HttpRequestException("Invalid authentication response.", ex); }
    }

    public async Task LogoutAsync(string accessToken, CancellationToken ct)
    {
        using var request = new HttpRequestMessage(HttpMethod.Post, "auth/logout");
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", accessToken);
        using var response = await clients.CreateClient("Authentication").SendAsync(request, ct);
        if (!response.IsSuccessStatusCode && response.StatusCode != HttpStatusCode.Unauthorized)
            throw new HttpRequestException("Logout service unavailable.");
    }
}
