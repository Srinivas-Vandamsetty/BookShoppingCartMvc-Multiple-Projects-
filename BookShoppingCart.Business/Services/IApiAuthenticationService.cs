using System.Security.Claims;
using BookShoppingCart.Models.Models.DTOs;
namespace BookShoppingCart.Business.Services;
public interface IApiAuthenticationService
{
    Task<AuthTokens?> LoginAsync(ClaimsPrincipal principal, CancellationToken ct);
    Task<AuthTokens?> RefreshAsync(string token, CancellationToken ct);
    Task LogoutAsync(ClaimsPrincipal principal, CancellationToken ct);
    Task<bool> ValidateAccessAsync(ClaimsPrincipal principal, CancellationToken ct);
}
public interface IAccessTokenIssuer
{
    IssuedAccessToken Issue(string userId, string name, IEnumerable<string> roles, Guid sessionId, DateTime now);
}
public sealed class ApiTokenLifetimes
{
    public TimeSpan RefreshLifetime { get; init; } = TimeSpan.FromDays(7);
    public TimeSpan SessionLifetime { get; init; } = TimeSpan.FromDays(30);
}
