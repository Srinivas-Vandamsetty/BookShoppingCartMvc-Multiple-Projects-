using BookShoppingCart.Data.Repositories;
namespace BookShoppingCart.WebAPI.Security;
public sealed class AuthenticationCleanupService(IServiceScopeFactory scopes, TimeProvider clock,
    ILogger<AuthenticationCleanupService> logger) : BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken ct)
    {
        using var timer=new PeriodicTimer(TimeSpan.FromHours(1));
        try
        {
            while(await timer.WaitForNextTickAsync(ct))
            {
                try
                {
                    using var scope=scopes.CreateScope();
                    // Retain consumed refresh tokens until the entire family has expired for replay detection.
                    await scope.ServiceProvider.GetRequiredService<IAuthSessionRepository>()
                        .PurgeExpiredAsync(clock.GetUtcNow().UtcDateTime.AddDays(-1),ct);
                }
                catch (OperationCanceledException) when (ct.IsCancellationRequested) { break; }
                catch (Exception ex) { logger.LogError(ex,"Expired API session cleanup failed"); }
            }
        }
        catch (OperationCanceledException) when (ct.IsCancellationRequested) { }
    }
}
