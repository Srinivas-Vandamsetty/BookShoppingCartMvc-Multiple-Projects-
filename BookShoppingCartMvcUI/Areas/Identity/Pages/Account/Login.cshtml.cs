using System.ComponentModel.DataAnnotations;
using System.Data.Common;
using BookShoppingCartMvcUI.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace BookShoppingCartMvcUI.Areas.Identity.Pages.Account;

[AllowAnonymous]
[ResponseCache(NoStore = true, Location = ResponseCacheLocation.None)]
public sealed class LoginModel(BrowserAuthenticationService authentication, ILogger<LoginModel> logger) : PageModel
{
    [BindProperty] public InputModel Input { get; set; } = new();
    public string? ReturnUrl { get; private set; }
    public bool SessionExpired { get; private set; }
    [TempData] public string? ErrorMessage { get; set; }

    public sealed class InputModel
    {
        [Required, EmailAddress, StringLength(256)]
        public string Email { get; set; } = "";
        [Required, DataType(DataType.Password), StringLength(1024)]
        public string Password { get; set; } = "";
        [Display(Name = "Keep me signed in")]
        public bool RememberMe { get; set; }
    }

    public void OnGet(string? returnUrl = null, bool sessionExpired = false)
    {
        SessionExpired = sessionExpired;
        ReturnUrl = Url.IsLocalUrl(returnUrl) ? returnUrl : null;
        if (!string.IsNullOrEmpty(ErrorMessage)) ModelState.AddModelError("", ErrorMessage);
    }

    public async Task<IActionResult> OnPostAsync(string? returnUrl = null)
    {
        ReturnUrl = Url.IsLocalUrl(returnUrl) ? returnUrl : null;
        if (!ModelState.IsValid) return Page();
        try
        {
            await authentication.SignInAsync(Input.Email.Trim(), Input.Password, Input.RememberMe,
                HttpContext, HttpContext.RequestAborted);
            logger.LogInformation("User signed in through the authentication API.");
            return LocalRedirect(ReturnUrl ?? (HttpContext.User.IsInRole("Admin") ? "/AdminOperations/Dashboard" : "/Home/Index"));
        }
        catch (AuthenticationApiException ex) { ModelState.AddModelError("", ex.Message); }
        catch (Exception ex) when (ex is HttpRequestException or DbException ||
            ex is OperationCanceledException && !HttpContext.RequestAborted.IsCancellationRequested)
        {
            logger.LogWarning("The authentication service could not complete sign-in.");
            ModelState.AddModelError("", "Sign-in is temporarily unavailable. Please try again shortly.");
        }
        Input.Password = "";
        ModelState.Remove("Input.Password");
        return Page();
    }
}
