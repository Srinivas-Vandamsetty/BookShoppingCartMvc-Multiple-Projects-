using Microsoft.AspNetCore.Identity;
namespace BookShoppingCart.Models.Models;
public sealed class ApiSession
{
    public Guid Id { get; set; }
    public string UserId { get; set; } = "";
    public string SecurityStampHash { get; set; } = "";
    public string? ProtectedBrowserTokens { get; set; }
    public DateTime CreatedAtUtc { get; set; }
    public DateTime ExpiresAtUtc { get; set; }
    public DateTime? RevokedAtUtc { get; set; }
    public IdentityUser User { get; set; } = null!;
}
public sealed class ApiRefreshToken
{
    public Guid Id { get; set; }
    public Guid SessionId { get; set; }
    public string TokenHash { get; set; } = "";
    public DateTime CreatedAtUtc { get; set; }
    public DateTime ExpiresAtUtc { get; set; }
    public DateTime? ConsumedAtUtc { get; set; }
    public ApiSession Session { get; set; } = null!;
}
