using System.Security.Claims;
using System.Security.Cryptography;
using System.Text.Json;
using BookShoppingCart.Business.Services;
using BookShoppingCart.Data.Data;
using BookShoppingCart.Models.Models.DTOs;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.WebUtilities;
using Microsoft.EntityFrameworkCore;

namespace BookShoppingCartMvcUI.Services;

// Browsers hold an encrypted Identity cookie with a session reference, never API tokens.
public sealed class BrowserAuthenticationService(ApplicationDbContext db, AuthenticationApiClient api,
    IDataProtectionProvider protection, SignInManager<IdentityUser> signIn, ILogger<BrowserAuthenticationService> logger)
{
    public const string SessionKey = "bookverse:api-session";
    public const string TokensKey = "bookverse:api-tokens";
    private readonly IDataProtector _protector = protection.CreateProtector("BookVerse.BrowserTokens.v1");

    public async Task SignInAsync(string email, string password, bool remember, HttpContext context, CancellationToken ct)
    {
        var tokens = await api.LoginAsync(email, password, ct);
        // The payload comes exclusively from the configured HTTPS API, never browser input.
        Guid id;
        string? userId;
        try
        {
            using var payload = JsonDocument.Parse(WebEncoders.Base64UrlDecode(tokens.AccessToken.Split('.')[1]));
            id = Guid.Parse(payload.RootElement.GetProperty("sid").GetString()!);
            userId = payload.RootElement.GetProperty("sub").GetString();
        }
        catch (Exception ex) when (ex is JsonException or FormatException or IndexOutOfRangeException or KeyNotFoundException)
        { throw new HttpRequestException("Invalid authentication response.", ex); }
        var session = await db.ApiSessions.Include(s => s.User).SingleOrDefaultAsync(s => s.Id == id, ct);
        if (session is null || session.UserId != userId || session.RevokedAtUtc is not null)
            throw new HttpRequestException("Invalid authentication session.");
        var previous = await context.AuthenticateAsync(IdentityConstants.ApplicationScheme);
        if (previous.Properties?.Items.TryGetValue(SessionKey, out var old) == true && Guid.TryParse(old, out var oldId) && oldId != id)
            await RevokeAsync(oldId, ct);
        session.ProtectedBrowserTokens = _protector.Protect(JsonSerializer.Serialize(tokens));
        await db.SaveChangesAsync(ct);
        var properties = new AuthenticationProperties
        {
            IsPersistent = remember, AllowRefresh = false,
            ExpiresUtc = new DateTimeOffset(DateTime.SpecifyKind(session.ExpiresAtUtc, DateTimeKind.Utc))
        };
        properties.Items[SessionKey] = id.ToString("N");
        await signIn.SignInAsync(session.User, properties);
    }

    private async Task LockAsync(Guid id, CancellationToken ct)
    {
        if (db.Database.IsSqlServer())
        {
            var resource = "browser-session:" + id.ToString("N");
            await db.Database.ExecuteSqlInterpolatedAsync($@"
DECLARE @result int;
EXEC @result=sys.sp_getapplock @Resource={resource}, @LockMode='Exclusive', @LockOwner='Transaction', @LockTimeout=20000;
IF @result < 0 THROW 51011, 'Browser session is busy.', 1;", ct);
        }
    }

    public async Task<AuthTokens?> GetTokensAsync(Guid id, string? userId, CancellationToken ct)
    {
        await using var transaction = await db.Database.BeginTransactionAsync(ct);
        await LockAsync(id, ct);
        var session = await db.ApiSessions.AsNoTracking().Include(s => s.User).SingleOrDefaultAsync(s => s.Id == id, ct);
        var now = DateTime.UtcNow;
        if (session is null || session.UserId != userId) return null;
        if (session.RevokedAtUtc is not null || session.ExpiresAtUtc <= now ||
            session.ProtectedBrowserTokens is null || !session.User.EmailConfirmed || session.User.TwoFactorEnabled ||
            (session.User.LockoutEnabled && session.User.LockoutEnd > now) ||
            session.SecurityStampHash != ApiAuthenticationService.Hash(session.User.SecurityStamp ?? ""))
        { await RevokeStoredAsync(id, ct); await transaction.CommitAsync(ct); return null; }
        AuthTokens? tokens;
        try { tokens = JsonSerializer.Deserialize<AuthTokens>(_protector.Unprotect(session.ProtectedBrowserTokens)); }
        catch (Exception ex) when (ex is CryptographicException or JsonException)
        { await RevokeStoredAsync(id, ct); await transaction.CommitAsync(ct); return null; }
        if (tokens is null || tokens.RefreshTokenExpiresAtUtc <= now)
        { await RevokeStoredAsync(id, ct); await transaction.CommitAsync(ct); return null; }
        bool rolesChanged;
        try
        {
            using var payload = JsonDocument.Parse(WebEncoders.Base64UrlDecode(tokens.AccessToken.Split('.')[1]));
            var tokenRoles = payload.RootElement.TryGetProperty("role", out var roles)
                ? roles.ValueKind == JsonValueKind.Array
                    ? roles.EnumerateArray().Select(r => r.GetString()!).ToArray()
                    : new[] { roles.GetString()! }
                : [];
            var currentRoles = await signIn.UserManager.GetRolesAsync(session.User);
            rolesChanged = !currentRoles.OrderBy(r => r, StringComparer.Ordinal)
                .SequenceEqual(tokenRoles.OrderBy(r => r, StringComparer.Ordinal));
        }
        catch (Exception ex) when (ex is JsonException or FormatException or IndexOutOfRangeException or InvalidOperationException)
        { await RevokeStoredAsync(id, ct); await transaction.CommitAsync(ct); return null; }
        if (tokens.AccessTokenExpiresAtUtc <= now.AddMinutes(1) || rolesChanged)
        {
            try { tokens = await api.RefreshAsync(tokens.RefreshToken, ct); }
            catch (Exception ex) when (ex is AuthenticationApiException or HttpRequestException || ex is OperationCanceledException && !ct.IsCancellationRequested)
            {
                // A lost refresh response is ambiguous. Revoke instead of replaying a consumed token.
                await RevokeStoredAsync(id, ct);
                await transaction.CommitAsync(ct);
                throw new AuthenticationApiException("Your session could not be renewed. Please sign in again.");
            }
            var encrypted = _protector.Protect(JsonSerializer.Serialize(tokens));
            var updated = await db.ApiSessions.Where(s => s.Id == id && s.RevokedAtUtc == null)
                .ExecuteUpdateAsync(set => set.SetProperty(s => s.ProtectedBrowserTokens, encrypted), ct);
            if (updated != 1) return null;
        }
        await transaction.CommitAsync(ct);
        return tokens;
    }

    private Task RevokeStoredAsync(Guid id, CancellationToken ct) => db.ApiSessions.Where(s => s.Id == id)
        .ExecuteUpdateAsync(set => set.SetProperty(s => s.RevokedAtUtc, (DateTime?)DateTime.UtcNow)
            .SetProperty(s => s.ProtectedBrowserTokens, (string?)null), ct);

    public async Task RevokeAsync(Guid id, CancellationToken ct)
    {
        await using var transaction = await db.Database.BeginTransactionAsync(ct);
        await LockAsync(id, ct);
        var session = await db.ApiSessions.AsNoTracking().SingleOrDefaultAsync(s => s.Id == id, ct);
        if (session?.ProtectedBrowserTokens is { } encrypted)
        {
            try
            {
                var tokens = JsonSerializer.Deserialize<AuthTokens>(_protector.Unprotect(encrypted));
                if (tokens is not null) await api.LogoutAsync(tokens.AccessToken, ct);
            }
            catch (Exception ex) when (ex is HttpRequestException or CryptographicException or JsonException ||
                ex is OperationCanceledException && !ct.IsCancellationRequested)
            { logger.LogWarning("API logout was unavailable; revoking the shared durable session locally."); }
        }
        await RevokeStoredAsync(id, ct);
        await transaction.CommitAsync(ct);
    }

    public async Task ValidateCookieAsync(CookieValidatePrincipalContext context)
    {
        if (!context.Properties.Items.TryGetValue(SessionKey, out var value) || !Guid.TryParse(value, out var id))
        { await RejectAsync(context); return; }
        try
        {
            var userId = context.Principal?.FindFirstValue(ClaimTypes.NameIdentifier);
            var tokens = await GetTokensAsync(id, userId, context.HttpContext.RequestAborted);
            if (tokens is null)
            { await RejectAsync(context); return; }
            var user = await signIn.UserManager.FindByIdAsync(userId!);
            if (user is null) { await RejectAsync(context); return; }
            context.ReplacePrincipal(await signIn.CreateUserPrincipalAsync(user));
            context.HttpContext.Items[SessionKey] = id;
            context.HttpContext.Items[TokensKey] = tokens;
            context.HttpContext.Response.Headers.CacheControl = "no-store";
            context.HttpContext.Response.Headers.Pragma = "no-cache";
        }
        catch (Exception ex) when (ex is AuthenticationApiException or HttpRequestException or System.Data.Common.DbException ||
            ex is OperationCanceledException && !context.HttpContext.RequestAborted.IsCancellationRequested)
        { logger.LogWarning("Browser authentication could not be validated; sign-in is required."); await RejectAsync(context); }
    }

    private static async Task RejectAsync(CookieValidatePrincipalContext context)
    {
        context.HttpContext.Items["bookverse:session-expired"] = true;
        context.RejectPrincipal();
        await context.HttpContext.SignOutAsync(IdentityConstants.ApplicationScheme);
    }
}
