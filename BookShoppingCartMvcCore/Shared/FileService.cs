using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;

namespace BookStoreCore.Shared;

public class FileService : IFileService
{
    private readonly string _imagePath;

    public FileService(IWebHostEnvironment environment)
    {
        _imagePath = Path.Combine(environment.WebRootPath, "images");
        Directory.CreateDirectory(_imagePath);
    }

    public async Task<string> SaveFile(IFormFile file, IEnumerable<string> allowedExtensions)
    {
        string extension = Path.GetExtension(file.FileName);
        if (!allowedExtensions.Contains(extension, StringComparer.OrdinalIgnoreCase))
        {
            throw new InvalidOperationException($"Only {string.Join(", ", allowedExtensions)} files allowed");
        }

        string fileName = $"{Guid.NewGuid()}{extension}";
        string filePath = Path.Combine(_imagePath, fileName);

        await using var stream = new FileStream(filePath, FileMode.Create);
        await file.CopyToAsync(stream);

        return fileName;
    }

    public void DeleteFile(string fileName)
    {
        string filePath = Path.Combine(_imagePath, fileName);
        if (File.Exists(filePath))
        {
            File.Delete(filePath);
        }
    }
}
