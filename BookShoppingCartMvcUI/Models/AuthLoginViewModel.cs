using System.ComponentModel.DataAnnotations;

namespace BookShoppingCartMvcUI.Models;

public sealed class AuthLoginViewModel
{
    [Required, EmailAddress, StringLength(256)]
    public string Email { get; set; } = string.Empty;

    [Required, DataType(DataType.Password), StringLength(1024)]
    public string Password { get; set; } = string.Empty;

    [Display(Name = "Keep me signed in")]
    public bool RememberMe { get; set; }

    public string? ReturnUrl { get; set; }
    public bool SessionExpired { get; set; }
}
