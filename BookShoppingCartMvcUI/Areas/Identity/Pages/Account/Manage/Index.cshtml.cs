using System.ComponentModel.DataAnnotations;
using BookShoppingCart.Models.Models.DTOs;
using BookShoppingCartMvcUI.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using System.Security.Claims;

namespace BookShoppingCartMvcUI.Areas.Identity.Pages.Account.Manage;

[Authorize]
public sealed class IndexModel(CommerceApiClient api) : PageModel
{
    public string Username { get; private set; } = "";
    public string Email { get; private set; } = "";
    public bool EmailConfirmed { get; private set; }
    public string Role { get; private set; } = "Customer";
    public string Initials => string.Concat(Username.Split(' ', StringSplitOptions.RemoveEmptyEntries)
        .Take(2).Select(part => char.ToUpperInvariant(part[0])));
    [TempData] public string? StatusMessage { get; set; }
    [BindProperty] public InputModel Input { get; set; } = new();
    public sealed class InputModel
    {
        [Phone, StringLength(30), Display(Name = "Phone number")]
        public string? PhoneNumber { get; set; }
    }

    public async Task<IActionResult> OnGetAsync()
    {
        await LoadProfileAsync(setPhoneNumber: true);
        return Page();
    }

    public async Task<IActionResult> OnPostAsync()
    {
        if (!ModelState.IsValid)
        {
            await LoadProfileAsync(setPhoneNumber: false);
            return Page();
        }
        try
        {
            await api.Exchange<CustomerProfileResponse>(HttpMethod.Patch, "customers/me", new CustomerProfileRequest { PhoneNumber = Input.PhoneNumber });
            StatusMessage = "Your profile has been updated.";
            return RedirectToPage();
        }
        catch (CommerceApiException ex) { ModelState.AddModelError("", ex.Message); return Page(); }
    }

    private async Task LoadProfileAsync(bool setPhoneNumber)
    {
        Username = User.Identity?.Name ?? "";
        Role = User.FindFirstValue(ClaimTypes.Role) ?? "Customer";
        try
        {
            var profile = await api.Get<CustomerProfileResponse>("customers/me");
            Username = profile.UserName;
            Email = profile.Email;
            EmailConfirmed = profile.EmailConfirmed;
            if (setPhoneNumber) Input.PhoneNumber = profile.PhoneNumber;
        }
        catch (CommerceApiException ex) { ModelState.AddModelError("", ex.Message); }
    }
}
