using System.Net.Http.Headers;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using System.Text.Encodings.Web;
using BookShoppingCart.Business.Services;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Options;
namespace BookShoppingCart.WebAPI.Security;
public sealed class BasicLoginHandler(IOptionsMonitor<AuthenticationSchemeOptions> options, ILoggerFactory logs,
    UrlEncoder encoder, UserManager<IdentityUser> users, SignInManager<IdentityUser> signIn)
    : AuthenticationHandler<AuthenticationSchemeOptions>(options,logs,encoder)
{
    public const string SchemeName="BasicLogin";
    private static readonly IdentityUser DummyUser=new();
    private static readonly string DummyHash=new PasswordHasher<IdentityUser>().HashPassword(DummyUser,
        Convert.ToHexString(RandomNumberGenerator.GetBytes(32)));
    protected override async Task<AuthenticateResult> HandleAuthenticateAsync()
    {
        if (!Request.IsHttps) return AuthenticateResult.Fail("HTTPS is required.");
        var header=Request.Headers.Authorization.ToString();
        if (header.Length > 4096 || !AuthenticationHeaderValue.TryParse(header,out var authorization) ||
            !authorization.Scheme.Equals("Basic",StringComparison.OrdinalIgnoreCase) || string.IsNullOrEmpty(authorization.Parameter))
            return AuthenticateResult.NoResult();
        string decoded;
        try { decoded=new UTF8Encoding(false,true).GetString(Convert.FromBase64String(authorization.Parameter)); }
        catch (Exception ex) when (ex is FormatException or DecoderFallbackException)
        { return AuthenticateResult.Fail("Invalid credentials."); }
        var colon=decoded.IndexOf(':');
        if (colon < 1 || colon > 256 || colon == decoded.Length-1 || decoded.Length-colon-1 > 1024)
            return AuthenticateResult.Fail("Invalid credentials.");
        var identifier=decoded[..colon];
        var password=decoded[(colon+1)..];
        var user=identifier.Contains('@') ? await users.FindByEmailAsync(identifier) : await users.FindByNameAsync(identifier);
        if (user is null)
        {
            _=users.PasswordHasher.VerifyHashedPassword(DummyUser,DummyHash,password);
            return AuthenticateResult.Fail("Invalid credentials.");
        }
        if (user.TwoFactorEnabled)
        {
            // No remembered-client cookie/MFA flow exists on this API. Never bypass it.
            _=users.PasswordHasher.VerifyHashedPassword(user,user.PasswordHash ?? DummyHash,password);
            return AuthenticateResult.Fail("Invalid credentials.");
        }
        var result=await signIn.CheckPasswordSignInAsync(user,password,lockoutOnFailure:true);
        // CheckPasswordSignInAsync alone does not complete a second factor.
        if (!result.Succeeded || user.TwoFactorEnabled)
            return AuthenticateResult.Fail("Invalid credentials.");
        var principal=new ClaimsPrincipal(new ClaimsIdentity(new[]
        {
            new Claim(ClaimTypes.NameIdentifier,user.Id),
            new Claim("credential_stamp",ApiAuthenticationService.Hash(user.SecurityStamp ?? ""))
        },SchemeName));
        return AuthenticateResult.Success(new AuthenticationTicket(principal,SchemeName));
    }
    protected override async Task HandleChallengeAsync(AuthenticationProperties properties)
    {
        Response.Headers.WWWAuthenticate="Basic realm=\"BookShoppingCart\", charset=\"UTF-8\"";
        Response.Headers.CacheControl="no-store";
        Response.StatusCode=401;
        await Response.WriteAsJsonAsync(new Microsoft.AspNetCore.Mvc.ProblemDetails
        { Status=401,Title="Invalid credentials or account unavailable." });
    }
}
