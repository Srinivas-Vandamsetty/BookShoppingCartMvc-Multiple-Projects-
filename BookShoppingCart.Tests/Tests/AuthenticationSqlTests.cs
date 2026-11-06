using BookShoppingCart.Data.Data;
using BookShoppingCart.Data.Repositories;
using BookShoppingCart.Models.Models;
using Microsoft.AspNetCore.Identity;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
namespace BookShoppingCart.Tests.Tests;

public sealed class AuthenticationSqlTests
{
    [SqlServerFact]
    public async Task ConcurrentRefresh_ProducesOneReplacementAndRevokesReusedSession()
    {
        var connection = new SqlConnectionStringBuilder(Environment.GetEnvironmentVariable("BOOKSTORE_TEST_SQL")!)
        { InitialCatalog = "BookShoppingCartTests_" + Guid.NewGuid().ToString("N") };
        var options = new DbContextOptionsBuilder<ApplicationDbContext>().UseSqlServer(connection.ConnectionString).Options;
        await using var setup = new ApplicationDbContext(options);
        var created = false;
        try
        {
            await setup.Database.MigrateAsync();
            created = true;
            var now = DateTime.UtcNow;
            var user = new IdentityUser { Id = Guid.NewGuid().ToString(), UserName = "refresh-test", SecurityStamp = "stamp" };
            setup.Users.Add(user);
            var session = new ApiSession { Id = Guid.NewGuid(), UserId = user.Id, SecurityStampHash = new string('A',64),
                CreatedAtUtc = now, ExpiresAtUtc = now.AddDays(30) };
            await new AuthSessionRepository(setup).CreateAsync(session, new ApiRefreshToken
            {
                Id = Guid.NewGuid(), SessionId = session.Id, TokenHash = new string('B',64),
                CreatedAtUtc = now, ExpiresAtUtc = now.AddDays(7)
            }, default);
            async Task<ApiRefreshToken?> Rotate(char hash)
            {
                await using var db = new ApplicationDbContext(options);
                return await new AuthSessionRepository(db).RotateAsync(new string('B',64), new string(hash,64), now,
                    TimeSpan.FromDays(7), default);
            }
            var results = await Task.WhenAll(Rotate('C'), Rotate('D'));
            Assert.Single(results, result => result is not null);
            var stored = await setup.ApiSessions.AsNoTracking().SingleAsync();
            Assert.NotNull(stored.RevokedAtUtc);
            Assert.Equal(2, await setup.ApiRefreshTokens.CountAsync());
            Assert.Equal(1, await setup.ApiRefreshTokens.CountAsync(token => token.ConsumedAtUtc != null));
            var replacement = results.Single(result => result is not null)!;
            Assert.Null(await new AuthSessionRepository(setup).RotateAsync(replacement.TokenHash, new string('E',64),
                now, TimeSpan.FromDays(7), default));
            await new AuthSessionRepository(setup).PurgeExpiredAsync(now.AddDays(31), default);
            Assert.Empty(await setup.ApiSessions.ToListAsync());
            Assert.Empty(await setup.ApiRefreshTokens.ToListAsync());
        }
        finally
        {
            if (created && connection.InitialCatalog.StartsWith("BookShoppingCartTests_", StringComparison.Ordinal))
                await setup.Database.EnsureDeletedAsync();
        }
    }
}
