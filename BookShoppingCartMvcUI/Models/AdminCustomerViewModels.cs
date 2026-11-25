using System.ComponentModel.DataAnnotations;

namespace BookShoppingCartMvcUI.Models;

public sealed class AdminCustomerListItem
{
    public required string Id { get; init; }
    public required string UserName { get; init; }
    public required string Email { get; init; }
    public string? PhoneNumber { get; init; }
    public required string Role { get; init; }
    public bool EmailConfirmed { get; init; }
    public bool IsLockedOut { get; init; }
    public int OrderCount { get; init; }
}

public sealed class EditCustomerViewModel
{
    [Required, StringLength(100)]
    [Display(Name = "Customer name")]
    public string UserName { get; set; } = string.Empty;

    [Required, EmailAddress, StringLength(256)]
    public string Email { get; set; } = string.Empty;

    [Phone, StringLength(30)]
    [Display(Name = "Phone number")]
    public string? PhoneNumber { get; set; }
}

public sealed class AdminUserRoleListItem
{
    public required string Id { get; init; }
    public required string UserName { get; init; }
    public required string Email { get; init; }
    public required string Role { get; init; }
    public bool EmailConfirmed { get; init; }
}

public sealed class UpdateUserRoleViewModel
{
    [Required]
    public string UserId { get; set; } = string.Empty;

    [Required]
    [Display(Name = "Application role")]
    public string Role { get; set; } = string.Empty;

    public string UserName { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
}
