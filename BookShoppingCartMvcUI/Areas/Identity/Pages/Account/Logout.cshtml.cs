using System.Data.Common;
using BookShoppingCartMvcUI.Services;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace BookShoppingCartMvcUI.Areas.Identity.Pages.Account;

public sealed class LogoutModel(BrowserAuthenticationService authentication, SignInManager<IdentityUser> signIn,
    ILogger<LogoutModel> logger) : PageModel
{
    public async Task<IActionResult> OnPostAsync(string? returnUrl = null)
    {
        var ticket = await HttpContext.AuthenticateAsync(IdentityConstants.ApplicationScheme);
        try
        {
            if (ticket.Properties?.Items.TryGetValue(BrowserAuthenticationService.SessionKey, out var value) == true &&
                Guid.TryParse(value, out var id))
                await authentication.RevokeAsync(id, HttpContext.RequestAborted);
        }
        catch (DbException)
        {
            logger.LogError("Logout could not revoke the durable session because the database is unavailable.");
            TempData["errorMessage"] = "You are signed out of this browser. Session revocation could not be confirmed; please try again when the service is available.";
        }
        finally { await signIn.SignOutAsync(); }
        return LocalRedirect(Url.IsLocalUrl(returnUrl) ? returnUrl! : "/");
    }
}
