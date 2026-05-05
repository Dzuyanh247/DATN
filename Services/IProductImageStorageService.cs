using Microsoft.AspNetCore.Http;

namespace Datn.PcStore.Services;

public interface IProductImageStorageService
{
    Task<string> SaveImageAsync(IFormFile file, CancellationToken cancellationToken = default);
    void DeleteImage(string relativePath);
    bool IsValidImage(IFormFile file, out string errorMessage);
}
