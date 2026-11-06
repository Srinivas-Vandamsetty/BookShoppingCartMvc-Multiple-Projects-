using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;

namespace BookStoreCore.Shared;
public class FileService : IFileService
{
    private readonly string _root;
    private readonly ILogger<FileService> _logger;
    public FileService(IWebHostEnvironment environment, ILogger<FileService> logger)
    {
        _root = Path.GetFullPath(Path.Combine(environment.WebRootPath, "images"));
        Directory.CreateDirectory(_root);
        _logger = logger;
    }
    private string Resolve(string name)
    {
        if (string.IsNullOrWhiteSpace(name) || Path.IsPathRooted(name) ||
            name.Contains('/') || name.Contains((char)92) || name != Path.GetFileName(name))
            throw new ArgumentException("Invalid image name.");
        var target = Path.GetFullPath(Path.Combine(_root, name));
        if (!target.StartsWith(_root + Path.DirectorySeparatorChar, StringComparison.OrdinalIgnoreCase))
            throw new ArgumentException("Invalid image name.");
        return target;
    }
    public async Task<string> SaveFile(IFormFile file, IEnumerable<string> allowedExtensions)
    {
        if (file.Length is <= 0 or > 1048576) throw new ArgumentException("Image size must be between 1 byte and 1 MB.");
        var ext = Path.GetExtension(file.FileName).ToLowerInvariant();
        if (!allowedExtensions.Contains(ext, StringComparer.OrdinalIgnoreCase))
            throw new ArgumentException("Only JPEG and PNG images are supported.");
        await using var input = file.OpenReadStream();
        var header = new byte[8];
        var count = await input.ReadAtLeastAsync(header, 8, throwOnEndOfStream: false);
        var png = count == 8 && header.AsSpan().SequenceEqual(new byte[] {137,80,78,71,13,10,26,10});
        var jpeg = count >= 3 && header[0] == 255 && header[1] == 216 && header[2] == 255;
        if (!(ext == ".png" ? png : jpeg)) throw new ArgumentException("The uploaded file is not a supported image.");
        var name = Guid.NewGuid().ToString("N") + ext;
        try
        {
            await using var output = new FileStream(Resolve(name), FileMode.CreateNew, FileAccess.Write, FileShare.None, 81920, true);
            await output.WriteAsync(header.AsMemory(0, count));
            await input.CopyToAsync(output);
        }
        catch { DeleteFile(name); throw; }
        return name;
    }
    public void DeleteFile(string name)
    {
        var target = Resolve(name);
        try { File.Delete(target); }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException)
        {
            // A cleanup failure must not turn a committed book update into a failed request.
            _logger.LogError(ex, "Image cleanup requires retry for {ImageName}", name);
        }
    }
}
