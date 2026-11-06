using System.ComponentModel.DataAnnotations;
using BookShoppingCart.Business.Services;
using BookShoppingCart.Models.Models.DTOs;
using BookShoppingCart.WebAPI.Security;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
namespace BookShoppingCart.WebAPI.Controllers;
public sealed class RefreshTokenRequest
{
    [Required, StringLength(86,MinimumLength=86)]
    public string RefreshToken { get; set; } = "";
}
[ApiController]
[RequestSizeLimit(4096)]
[Route("api/auth")]
[EnableRateLimiting("AuthenticationPolicy")]
[ResponseCache(NoStore=true,Location=ResponseCacheLocation.None)]
public sealed class AuthenticationController(IApiAuthenticationService authentication) : ControllerBase
{
    private void PreventCaching()
    {
        Response.Headers.CacheControl="no-store";
        Response.Headers.Pragma="no-cache";
    }
    [HttpPost("login")]
    [ProducesResponseType<AuthTokens>(StatusCodes.Status200OK)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status401Unauthorized)]
    [Authorize(AuthenticationSchemes=BasicLoginHandler.SchemeName)]
    public async Task<IActionResult> Login(CancellationToken ct)
    {
        PreventCaching();
        var tokens=await authentication.LoginAsync(User,ct);
        return tokens is null ? Unauthorized(new ProblemDetails { Status=401,Title="Invalid credentials or account unavailable." }) : Ok(tokens);
    }
    [HttpPost("refresh")]
    [ProducesResponseType<AuthTokens>(StatusCodes.Status200OK)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status401Unauthorized)]
    [AllowAnonymous]
    public async Task<IActionResult> Refresh(RefreshTokenRequest request,CancellationToken ct)
    {
        PreventCaching();
        var tokens=await authentication.RefreshAsync(request.RefreshToken,ct);
        return tokens is null ? Unauthorized(new ProblemDetails { Status=401,Title="Invalid or expired refresh token." }) : Ok(tokens);
    }
    [HttpPost("logout")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [Authorize(AuthenticationSchemes=JwtBearerDefaults.AuthenticationScheme)]
    public async Task<IActionResult> Logout(CancellationToken ct)
    {
        PreventCaching();
        await authentication.LogoutAsync(User,ct);
        return NoContent();
    }
}
