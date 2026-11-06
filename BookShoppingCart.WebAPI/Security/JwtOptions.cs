using System.ComponentModel.DataAnnotations;
using System.Security.Cryptography;
using Microsoft.IdentityModel.Tokens;
namespace BookShoppingCart.WebAPI.Security;
public sealed class JwtOptions
{
    [Required, MaxLength(200)] public string Issuer { get; set; } = "";
    [Required, MaxLength(200)] public string Audience { get; set; } = "";
    [Required] public string SigningKey { get; set; } = "";
    public string[] PreviousSigningKeys { get; set; } = [];
    [Range(1,30)] public int AccessTokenMinutes { get; set; } = 15;
    [Range(1,30)] public int RefreshTokenDays { get; set; } = 7;
    [Range(1,90)] public int SessionDays { get; set; } = 30;
    public bool HasValidKeys()
    {
        try
        {
            return PreviousSigningKeys is not null && PreviousSigningKeys.Length <= 2 && SessionDays >= RefreshTokenDays &&
                new[] { SigningKey }.Concat(PreviousSigningKeys).All(k => !string.IsNullOrWhiteSpace(k) && Convert.FromBase64String(k).Length >= 32);
        }
        catch (FormatException) { return false; }
    }
    public static SymmetricSecurityKey Key(string encoded)
    {
        var bytes=Convert.FromBase64String(encoded);
        return new SymmetricSecurityKey(bytes) { KeyId = Convert.ToHexString(SHA256.HashData(bytes))[..16] };
    }
    public TokenValidationParameters ValidationParameters() => new()
    {
        ValidateIssuer = true, ValidIssuer = Issuer,
        ValidateAudience = true, ValidAudience = Audience, IgnoreTrailingSlashWhenValidatingAudience = false,
        ValidateIssuerSigningKey = true, IssuerSigningKeys = new[] { SigningKey }.Concat(PreviousSigningKeys).Select(Key),
        RequireSignedTokens = true, RequireExpirationTime = true, ValidateLifetime = true,
        ValidAlgorithms = [SecurityAlgorithms.HmacSha256],
        ValidTypes = ["at+jwt"], ClockSkew = TimeSpan.FromSeconds(30),
        NameClaimType = "name", RoleClaimType = "role"
    };
}
