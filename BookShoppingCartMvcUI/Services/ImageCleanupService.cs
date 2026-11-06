using BookShoppingCart.Data.Data;
using BookStoreCore.Shared;
using Microsoft.EntityFrameworkCore;
namespace BookShoppingCartMvcUI.Services;
// Retry abandoned-upload/old-image cleanup without affecting committed catalog writes.
public sealed class ImageCleanupService(IServiceScopeFactory scopes, IWebHostEnvironment environment,
    ILogger<ImageCleanupService> logger) : BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        using var timer = new PeriodicTimer(TimeSpan.FromHours(6));
        try
        {
            while (await timer.WaitForNextTickAsync(stoppingToken))
            {
                try
                {
                    var directory = Path.Combine(environment.WebRootPath, "images");
                    if (!Directory.Exists(directory)) continue;
                    var cutoff = DateTime.UtcNow.AddDays(-1);
                    var names = Directory.EnumerateFiles(directory).Select(p => new FileInfo(p))
                        .Where(f => f.LastWriteTimeUtc < cutoff && Guid.TryParse(Path.GetFileNameWithoutExtension(f.Name), out _))
                        .OrderBy(f => f.LastWriteTimeUtc).Take(200).Select(f => f.Name).ToArray();
                    using var scope = scopes.CreateScope();
                    var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
                    var used = await db.Books.AsNoTracking().Where(b => names.Contains(b.Image))
                        .Select(b => b.Image).ToListAsync(stoppingToken);
                    var files = scope.ServiceProvider.GetRequiredService<IFileService>();
                    foreach (var name in names.Except(used)) files.DeleteFile(name);
                }
                catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested) { break; }
                catch (Exception ex) { logger.LogError(ex, "Image cleanup failed; it will retry on the next interval"); }
            }
        }
        catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested) { }
    }
}
