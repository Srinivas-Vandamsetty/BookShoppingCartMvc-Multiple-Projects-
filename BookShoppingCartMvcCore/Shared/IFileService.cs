using Microsoft.AspNetCore.Http;

namespace BookStoreCore.Shared
{
    public interface IFileService
    {
        void DeleteFile(string fileName);
        Task<string> SaveFile(IFormFile file, IEnumerable<string> allowedExtensions);
    }
}
