using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using BookShoppingCart.Data.Repositories;
using BookShoppingCart.Models.Models;
using BookShoppingCart.Models.Models.DTOs;
using Microsoft.AspNetCore.Identity;
namespace BookShoppingCart.Business.Services;

public sealed class ApiAuthenticationService(IAuthSessionRepository sessions, UserManager<IdentityUser> users,
    IAccessTokenIssuer issuer, ApiTokenLifetimes lifetimes, TimeProvider clock) : IApiAuthenticationService
{
    public static string Hash(string value) => Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(value)));
    private static string NewToken() => Microsoft.AspNetCore.WebUtilities.WebEncoders.Base64UrlEncode(RandomNumberGenerator.GetBytes(64));
    private DateTime Now => clock.GetUtcNow().UtcDateTime;
    private static bool IsUsable(IdentityUser user, DateTime now) =>
        user.EmailConfirmed && !user.TwoFactorEnabled && (!user.LockoutEnabled || user.LockoutEnd is null || user.LockoutEnd <= now);

    public async Task<AuthTokens?> LoginAsync(ClaimsPrincipal principal, CancellationToken ct)
    {
        var id = principal.FindFirstValue(ClaimTypes.NameIdentifier);
        if (id is null) return null;
        var user = await users.FindByIdAsync(id);
        var now = Now;
        if (user is null || !IsUsable(user, now) ||
            principal.FindFirstValue("credential_stamp") != Hash(user.SecurityStamp ?? "")) return null;
        var session = new ApiSession
        {
            Id = Guid.NewGuid(), UserId = user.Id, SecurityStampHash = Hash(user.SecurityStamp ?? ""),
            CreatedAtUtc = now, ExpiresAtUtc = now.Add(lifetimes.SessionLifetime)
        };
        var raw = NewToken();
        var refresh = new ApiRefreshToken
        {
            Id = Guid.NewGuid(), SessionId = session.Id, TokenHash = Hash(raw), CreatedAtUtc = now,
            ExpiresAtUtc = new[] { now.Add(lifetimes.RefreshLifetime), session.ExpiresAtUtc }.Min()
        };
        var access = issuer.Issue(user.Id, user.UserName ?? "", await users.GetRolesAsync(user), session.Id, now);
        await sessions.CreateAsync(session, refresh, ct);
        return new(access.Token, raw, access.ExpiresAtUtc, refresh.ExpiresAtUtc);
    }
    public async Task<AuthTokens?> RefreshAsync(string token, CancellationToken ct)
    {
        if (string.IsNullOrEmpty(token) || token.Length != 86 ||
            token.Any(c => !(char.IsAsciiLetterOrDigit(c) || c is '-' or '_'))) return null;
        var now = Now;
        var raw = NewToken();
        var replacement = await sessions.RotateAsync(Hash(token), Hash(raw), now, lifetimes.RefreshLifetime, ct);
        if (replacement is null) return null;
        var session = await sessions.FindSessionAsync(replacement.SessionId, ct);
        if (session is null) return null;
        var user = session.User;
        if (session.RevokedAtUtc is not null || !IsUsable(user, now) || session.SecurityStampHash != Hash(user.SecurityStamp ?? ""))
        {
            await sessions.RevokeAsync(session.Id, session.UserId, now, ct);
            return null;
        }
        var access = issuer.Issue(user.Id, user.UserName ?? "", await users.GetRolesAsync(user), session.Id, now);
        return new(access.Token, raw, access.ExpiresAtUtc, replacement.ExpiresAtUtc);
    }
    public async Task<bool> ValidateAccessAsync(ClaimsPrincipal principal, CancellationToken ct)
    {
        if (!Guid.TryParse(principal.FindFirstValue("sid"), out var sessionId) ||
            !Guid.TryParse(principal.FindFirstValue("jti"), out _)) return false;
        var session = await sessions.FindSessionAsync(sessionId, ct);
        var now = Now;
        if (session is null || principal.FindFirstValue("sub") != session.UserId || session.RevokedAtUtc is not null ||
            session.ExpiresAtUtc <= now || !IsUsable(session.User, now) ||
            session.SecurityStampHash != Hash(session.User.SecurityStamp ?? "")) return false;
        var roles = await users.GetRolesAsync(session.User);
        return roles.OrderBy(r => r, StringComparer.Ordinal)
            .SequenceEqual(principal.FindAll("role").Select(c => c.Value).OrderBy(r => r, StringComparer.Ordinal));
    }
    public async Task LogoutAsync(ClaimsPrincipal principal, CancellationToken ct)
    {
        if (Guid.TryParse(principal.FindFirstValue("sid"), out var id) && principal.FindFirstValue("sub") is { } user)
            await sessions.RevokeAsync(id, user, Now, ct);
    }
}
