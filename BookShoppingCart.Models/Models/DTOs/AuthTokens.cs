namespace BookShoppingCart.Models.Models.DTOs;
public sealed record AuthTokens(string AccessToken, string RefreshToken,
    DateTime AccessTokenExpiresAtUtc, DateTime RefreshTokenExpiresAtUtc, string TokenType = "Bearer");
public sealed record IssuedAccessToken(string Token, DateTime ExpiresAtUtc);
