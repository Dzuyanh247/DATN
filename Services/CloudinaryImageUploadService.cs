using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
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
    private readonly HttpClient _httpClient;

    public CloudinaryImageUploadService(IOptions<CloudinaryOptions> options, HttpClient httpClient)
    {
        _options = options.Value;
        _httpClient = httpClient;
    }

    public int MaxFileSizeBytes => Math.Max(1, _options.MaxFileSizeMb) * 1024 * 1024;

    public async Task<string> UploadAsync(IFormFile file, CancellationToken cancellationToken = default)
    {
        if (file.Length <= 0) throw new InvalidOperationException("File ảnh không hợp lệ.");
        if (file.Length > MaxFileSizeBytes) throw new InvalidOperationException($"Dung lượng ảnh tối đa {_options.MaxFileSizeMb}MB.");
        if (!AllowedContentTypes.Contains(file.ContentType)) throw new InvalidOperationException("Chỉ hỗ trợ file ảnh jpg, png, webp, gif hoặc avif.");
        if (string.IsNullOrWhiteSpace(_options.CloudName) || string.IsNullOrWhiteSpace(_options.ApiKey) || string.IsNullOrWhiteSpace(_options.ApiSecret))
            throw new InvalidOperationException("Chưa cấu hình Cloudinary. Vui lòng khai báo Cloudinary:CloudName, ApiKey và ApiSecret.");

        var timestamp = DateTimeOffset.UtcNow.ToUnixTimeSeconds().ToString();
        var folder = string.IsNullOrWhiteSpace(_options.Folder) ? "kkshop/admin" : _options.Folder.Trim('/');
        var signaturePayload = $"folder={folder}&timestamp={timestamp}{_options.ApiSecret}";
        var signature = Convert.ToHexString(SHA1.HashData(Encoding.UTF8.GetBytes(signaturePayload))).ToLowerInvariant();

        using var content = new MultipartFormDataContent();
        await using var stream = file.OpenReadStream();
        using var fileContent = new StreamContent(stream);
        fileContent.Headers.ContentType = new System.Net.Http.Headers.MediaTypeHeaderValue(file.ContentType);
        content.Add(fileContent, "file", file.FileName);
        content.Add(new StringContent(_options.ApiKey), "api_key");
        content.Add(new StringContent(timestamp), "timestamp");
        content.Add(new StringContent(folder), "folder");
        content.Add(new StringContent(signature), "signature");

        var endpoint = $"https://api.cloudinary.com/v1_1/{Uri.EscapeDataString(_options.CloudName)}/image/upload";
        using var response = await _httpClient.PostAsync(endpoint, content, cancellationToken);
        var json = await response.Content.ReadAsStringAsync(cancellationToken);
        if (!response.IsSuccessStatusCode) throw new InvalidOperationException("Upload Cloudinary thất bại.");
        using var doc = JsonDocument.Parse(json);
        return doc.RootElement.GetProperty("secure_url").GetString() ?? throw new InvalidOperationException("Cloudinary không trả về URL ảnh.");
    }
}
