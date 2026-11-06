using BookShoppingCart.Data.Data;
using BookShoppingCart.Models.Models;
using Microsoft.EntityFrameworkCore;
namespace BookShoppingCart.Data.Repositories;
public sealed class AuthSessionRepository(ApplicationDbContext db) : IAuthSessionRepository
{
    public async Task CreateAsync(ApiSession session, ApiRefreshToken token, CancellationToken ct)
    {
        db.ApiSessions.Add(session);
        db.ApiRefreshTokens.Add(token);
        await db.SaveChangesAsync(ct);
    }
    public Task<ApiSession?> FindSessionAsync(Guid id, CancellationToken ct) =>
        db.ApiSessions.AsNoTracking().Include(s => s.User).SingleOrDefaultAsync(s => s.Id == id, ct);
    private async Task LockAsync(Guid id, CancellationToken ct)
    {
        if (db.Database.IsSqlServer())
        {
            var resource = "api-session:" + id.ToString("N");
            await db.Database.ExecuteSqlInterpolatedAsync($@"
DECLARE @result int;
EXEC @result = sys.sp_getapplock @Resource={resource}, @LockMode='Exclusive',
    @LockOwner='Transaction', @LockTimeout=10000;
IF @result < 0 THROW 51010, 'Authentication session is busy.', 1;", ct);
        }
    }
    public async Task<ApiRefreshToken?> RotateAsync(string hash, string replacementHash,
        DateTime now, TimeSpan lifetime, CancellationToken ct)
    {
        var sessionId = await db.ApiRefreshTokens.AsNoTracking().Where(t => t.TokenHash == hash)
            .Select(t => (Guid?)t.SessionId).SingleOrDefaultAsync(ct);
        if (sessionId is null) return null;
        await using var tx = await db.Database.BeginTransactionAsync(ct);
        await LockAsync(sessionId.Value, ct);
        var token = await db.ApiRefreshTokens.AsNoTracking().Include(t => t.Session)
            .SingleOrDefaultAsync(t => t.TokenHash == hash, ct);
        if (token is null)
        {
            await tx.CommitAsync(ct);
            return null;
        }
        var session = token.Session;
        if (session.RevokedAtUtc is not null || session.ExpiresAtUtc <= now)
        {
            await tx.CommitAsync(ct);
            return null;
        }
        if (token.ConsumedAtUtc is not null)
        {
            // Reuse indicates a copied token (or concurrent refresh): revoke the whole family.
            await db.ApiSessions.Where(s => s.Id == session.Id && s.RevokedAtUtc == null)
                .ExecuteUpdateAsync(s => s.SetProperty(x => x.RevokedAtUtc, (DateTime?)now), ct);
            await tx.CommitAsync(ct);
            return null;
        }
        if (token.ExpiresAtUtc <= now)
        {
            await tx.CommitAsync(ct);
            return null;
        }
        var consumed = await db.ApiRefreshTokens.Where(t => t.Id == token.Id && t.ConsumedAtUtc == null)
            .ExecuteUpdateAsync(s => s.SetProperty(t => t.ConsumedAtUtc, (DateTime?)now), ct);
        if (consumed != 1)
        {
            await db.ApiSessions.Where(s => s.Id == session.Id && s.RevokedAtUtc == null)
                .ExecuteUpdateAsync(s => s.SetProperty(x => x.RevokedAtUtc, (DateTime?)now), ct);
            await tx.CommitAsync(ct);
            return null;
        }
        var replacement = new ApiRefreshToken
        {
            Id = Guid.NewGuid(), SessionId = session.Id, TokenHash = replacementHash, CreatedAtUtc = now,
            ExpiresAtUtc = new[] { now.Add(lifetime), session.ExpiresAtUtc }.Min()
        };
        db.ApiRefreshTokens.Add(replacement);
        await db.SaveChangesAsync(ct);
        await tx.CommitAsync(ct);
        return replacement;
    }
    public async Task RevokeAsync(Guid sessionId, string userId, DateTime now, CancellationToken ct)
    {
        await using var tx = await db.Database.BeginTransactionAsync(ct);
        await LockAsync(sessionId, ct);
        await db.ApiSessions.Where(s => s.Id == sessionId && s.UserId == userId && s.RevokedAtUtc == null)
            .ExecuteUpdateAsync(s => s.SetProperty(x => x.RevokedAtUtc, (DateTime?)now), ct);
        await tx.CommitAsync(ct);
    }
    public Task PurgeExpiredAsync(DateTime cutoff, CancellationToken ct) =>
        db.ApiSessions.Where(s => s.ExpiresAtUtc < cutoff).ExecuteDeleteAsync(ct);
}
