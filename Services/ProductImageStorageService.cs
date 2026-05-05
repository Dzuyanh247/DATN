using System.Globalization;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;

namespace Datn.PcStore.Services;

public class ProductImageStorageService : IProductImageStorageService
{
    private const long MaxFileSizeBytes = 5 * 1024 * 1024;
    private static readonly HashSet<string> AllowedExtensions = new(StringComparer.OrdinalIgnoreCase)
    {
        ".jpg", ".jpeg", ".png", ".webp"
    };

    private readonly IWebHostEnvironment _environment;

    public ProductImageStorageService(IWebHostEnvironment environment)
    {
        _environment = environment;
    }

    public bool IsValidImage(IFormFile file, out string errorMessage)
    {
        errorMessage = string.Empty;
        var extension = Path.GetExtension(file.FileName);

        if (!AllowedExtensions.Contains(extension))
        {
            errorMessage = "Chỉ chấp nhận file ảnh .jpg, .jpeg, .png, .webp.";
            return false;
        }

        if (file.Length <= 0)
        {
            errorMessage = "File upload không hợp lệ.";
            return false;
        }

        if (file.Length > MaxFileSizeBytes)
        {
            errorMessage = string.Format(CultureInfo.CurrentCulture, "File {0} vượt quá dung lượng 5MB.", file.FileName);
            return false;
        }

        return true;
    }

    public async Task<string> SaveImageAsync(IFormFile file, CancellationToken cancellationToken = default)
    {
        var uploadsFolder = Path.Combine(_environment.WebRootPath, "uploads", "products");
        Directory.CreateDirectory(uploadsFolder);

        var extension = Path.GetExtension(file.FileName).ToLowerInvariant();
        var fileName = $"{Guid.NewGuid():N}{extension}";
        var fullPath = Path.Combine(uploadsFolder, fileName);

        await using var stream = new FileStream(fullPath, FileMode.Create);
        await file.CopyToAsync(stream, cancellationToken);

        return $"/uploads/products/{fileName}";
    }

    public void DeleteImage(string relativePath)
    {
        if (string.IsNullOrWhiteSpace(relativePath))
        {
            return;
        }

        var normalizedPath = relativePath.TrimStart('/').Replace('/', Path.DirectorySeparatorChar);
        var fullPath = Path.Combine(_environment.WebRootPath, normalizedPath);

        if (File.Exists(fullPath))
        {
            File.Delete(fullPath);
        }
    }
}
