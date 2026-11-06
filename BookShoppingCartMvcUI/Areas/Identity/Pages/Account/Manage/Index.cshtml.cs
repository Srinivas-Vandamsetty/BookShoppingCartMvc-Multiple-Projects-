using System.ComponentModel.DataAnnotations;
using BookShoppingCart.Models.Models.DTOs;
using BookShoppingCartMvcUI.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace BookShoppingCartMvcUI.Areas.Identity.Pages.Account.Manage;

[Authorize]
public sealed class IndexModel(CommerceApiClient api) : PageModel
{
    public string Username { get; private set; } = "";
    [TempData] public string? StatusMessage { get; set; }
    [BindProperty] public InputModel Input { get; set; } = new();
    public sealed class InputModel
    {
        [Phone, StringLength(30), Display(Name = "Phone number")]
        public string? PhoneNumber { get; set; }
    }

    public async Task<IActionResult> OnGetAsync()
    {
        try
        {
            var profile = await api.Get<CustomerProfileResponse>("customers/me");
            Username = profile.UserName;
            Input.PhoneNumber = profile.PhoneNumber;
        }
        catch (CommerceApiException ex) { ModelState.AddModelError("", ex.Message); }
        return Page();
    }

    public async Task<IActionResult> OnPostAsync()
    {
        Username = User.Identity?.Name ?? "";
        if (!ModelState.IsValid) return Page();
        try
        {
            await api.Exchange<CustomerProfileResponse>(HttpMethod.Patch, "customers/me", new CustomerProfileRequest { PhoneNumber = Input.PhoneNumber });
            StatusMessage = "Your profile has been updated.";
            return RedirectToPage();
        }
        catch (CommerceApiException ex) { ModelState.AddModelError("", ex.Message); return Page(); }
    }
}
