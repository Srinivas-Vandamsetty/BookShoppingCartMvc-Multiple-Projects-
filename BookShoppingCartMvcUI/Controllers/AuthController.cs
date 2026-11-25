using System.Data.Common;
using BookShoppingCartMvcUI.Models;
using BookShoppingCartMvcUI.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;

namespace BookShoppingCartMvcUI.Controllers;

[ResponseCache(NoStore = true, Location = ResponseCacheLocation.None)]
public sealed class AuthController(BrowserAuthenticationService authentication, ILogger<AuthController> logger) : Controller
{
    [AllowAnonymous]
    [HttpGet]
    public IActionResult Login(string? returnUrl = null, bool sessionExpired = false)
    {
        var model = new AuthLoginViewModel
        {
            ReturnUrl = Url.IsLocalUrl(returnUrl) ? returnUrl : null,
            SessionExpired = sessionExpired
        };
        return View(model);
    }

    [AllowAnonymous]
    [HttpPost]
    [ValidateAntiForgeryToken]
    [EnableRateLimiting("Login")]
    public async Task<IActionResult> Login(AuthLoginViewModel model, CancellationToken ct)
    {
        model.ReturnUrl = Url.IsLocalUrl(model.ReturnUrl) ? model.ReturnUrl : null;
        if (!ModelState.IsValid) return View(model);
        try
        {
            await authentication.SignInAsync(model.Email.Trim(), model.Password, model.RememberMe, HttpContext, ct);
            logger.LogInformation("User signed in through the JWT authentication API.");
            return LocalRedirect(model.ReturnUrl ?? "/FrontPage/Index");
        }
        catch (AuthenticationApiException ex) { ModelState.AddModelError(string.Empty, ex.Message); }
        catch (Exception ex) when (ex is HttpRequestException or DbException || ex is OperationCanceledException && !ct.IsCancellationRequested)
        {
            logger.LogWarning("The authentication service could not complete sign-in.");
            ModelState.AddModelError(string.Empty, "Sign-in is temporarily unavailable. Please try again shortly.");
        }
        model.Password = string.Empty;
        ModelState.Remove(nameof(model.Password));
        return View(model);
    }

    [Authorize]
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Logout(string? returnUrl = null, CancellationToken ct = default)
    {
        var ticket = await HttpContext.AuthenticateAsync(BookVerseAuthenticationDefaults.Scheme);
        try
        {
            if (ticket.Properties?.Items.TryGetValue(BrowserAuthenticationService.SessionKey, out var value) == true &&
                Guid.TryParse(value, out var id))
                await authentication.RevokeAsync(id, ct);
        }
        catch (DbException)
        {
            logger.LogError("Logout could not revoke the durable session because the database is unavailable.");
            TempData["errorMessage"] = "You are signed out of this browser. Session revocation could not be confirmed.";
        }
        finally { await HttpContext.SignOutAsync(BookVerseAuthenticationDefaults.Scheme); }
        return LocalRedirect(Url.IsLocalUrl(returnUrl) ? returnUrl : "/");
    }
}
