using CloudinaryDotNet;
using CloudinaryDotNet.Actions;
using Microsoft.Extensions.Options;

namespace Datn.PcStore.Services;

public sealed class CloudinaryOptions
{
    public string CloudName { get; set; } = string.Empty;
    public string ApiKey { get; set; } = string.Empty;
    public string ApiSecret { get; set; } = string.Empty;
    public string Folder { get; set; } = "kkshop/admin";
    public int MaxFileSizeMb { get; set; } = 10;
}

public interface ICloudinaryImageUploadService
{
    int MaxFileSizeBytes { get; }
    Task<string> UploadAsync(IFormFile file, CancellationToken cancellationToken = default);
}

public sealed class CloudinaryImageUploadService : ICloudinaryImageUploadService
{
    private static readonly HashSet<string> AllowedContentTypes = new(StringComparer.OrdinalIgnoreCase)
    {
        "image/jpeg", "image/png", "image/webp", "image/gif", "image/avif"
    };

    private readonly CloudinaryOptions _options;
    private readonly Cloudinary _cloudinary;

    public CloudinaryImageUploadService(IOptions<CloudinaryOptions> options)
    {
        _options = options.Value;

        if (string.IsNullOrWhiteSpace(_options.CloudName) ||
            string.IsNullOrWhiteSpace(_options.ApiKey) ||
            string.IsNullOrWhiteSpace(_options.ApiSecret))
        {
            throw new InvalidOperationException("Chưa cấu hình Cloudinary.");
        }

        var account = new Account(
            _options.CloudName,
            _options.ApiKey,
            _options.ApiSecret
        );

        _cloudinary = new Cloudinary(account)
        {
            Api = { Secure = true }
        };
    }

    public int MaxFileSizeBytes => Math.Max(1, _options.MaxFileSizeMb) * 1024 * 1024;

    public async Task<string> UploadAsync(IFormFile file, CancellationToken cancellationToken = default)
    {
        if (file.Length <= 0)
            throw new InvalidOperationException("File ảnh không hợp lệ.");

        if (file.Length > MaxFileSizeBytes)
            throw new InvalidOperationException($"Dung lượng ảnh tối đa {_options.MaxFileSizeMb}MB.");

        if (!AllowedContentTypes.Contains(file.ContentType))
            throw new InvalidOperationException("Chỉ hỗ trợ file ảnh jpg, png, webp, gif hoặc avif.");

        var folder = string.IsNullOrWhiteSpace(_options.Folder)
            ? "kkshop/admin"
            : _options.Folder.Trim('/');

        await using var stream = file.OpenReadStream();

        var uploadParams = new ImageUploadParams
        {
            File = new FileDescription(file.FileName, stream),
            Folder = folder,
            UseFilename = true,
            UniqueFilename = true,
            Overwrite = false
        };

        var result = await _cloudinary.UploadAsync(uploadParams, cancellationToken);

        if (result.Error != null)
            throw new InvalidOperationException($"Upload Cloudinary thất bại: {result.Error.Message}");

        return result.SecureUrl?.ToString()
            ?? throw new InvalidOperationException("Cloudinary không trả về URL ảnh.");
    }
}
