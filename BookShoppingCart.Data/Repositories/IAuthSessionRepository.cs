using BookShoppingCart.Models.Models;
namespace BookShoppingCart.Data.Repositories;
public interface IAuthSessionRepository
{
    Task CreateAsync(ApiSession session, ApiRefreshToken token, CancellationToken ct);
    Task<ApiSession?> FindSessionAsync(Guid id, CancellationToken ct);
    Task<ApiRefreshToken?> RotateAsync(string hash, string replacementHash, DateTime now, TimeSpan lifetime, CancellationToken ct);
    Task RevokeAsync(Guid sessionId, string userId, DateTime now, CancellationToken ct);
    Task PurgeExpiredAsync(DateTime cutoff, CancellationToken ct);
}
