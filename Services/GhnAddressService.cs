using System.Net.Http.Json;
using System.Text.Json;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Options;

namespace Datn.PcStore.Services;

public class GhnAddressService : IGhnAddressService
{
    private static readonly TimeSpan CacheTtl = TimeSpan.FromHours(12);
    private readonly HttpClient _httpClient;
    private readonly IMemoryCache _cache;
    private readonly GhnOptions _options;
    private readonly ILogger<GhnAddressService> _logger;

    public GhnAddressService(HttpClient httpClient, IMemoryCache cache, IOptions<GhnOptions> options, ILogger<GhnAddressService> logger)
    {
        _httpClient = httpClient;
        _cache = cache;
        _options = options.Value;
        _logger = logger;
    }

    public Task<IReadOnlyList<ProvinceDto>> GetProvincesAsync(CancellationToken cancellationToken = default)
        => _cache.GetOrCreateAsync("ghn:provinces", _ => FetchProvincesAsync(cancellationToken))!;

    public Task<IReadOnlyList<DistrictDto>> GetDistrictsAsync(int provinceId, CancellationToken cancellationToken = default)
        => _cache.GetOrCreateAsync($"ghn:districts:{provinceId}", _ => FetchDistrictsAsync(provinceId, cancellationToken))!;

    public Task<IReadOnlyList<WardDto>> GetWardsAsync(int districtId, CancellationToken cancellationToken = default)
        => _cache.GetOrCreateAsync($"ghn:wards:{districtId}", _ => FetchWardsAsync(districtId, cancellationToken))!;

    private void EnsureToken()
    {
        if (string.IsNullOrWhiteSpace(_options.Token))
            throw new InvalidOperationException("GHN token is missing. Configure GHN:Token in appsettings.json");
    }

    private async Task<IReadOnlyList<ProvinceDto>> FetchProvincesAsync(CancellationToken cancellationToken)
    {
        EnsureToken();
        _logger.LogInformation("GHN request provinces");
        var data = await SendAsync("master-data/province", HttpMethod.Get, null, cancellationToken);
        return data.EnumerateArray().Select(x => new ProvinceDto(x.GetProperty("ProvinceID").GetInt32(), x.GetProperty("ProvinceName").GetString() ?? string.Empty)).ToList();
    }

    private async Task<IReadOnlyList<DistrictDto>> FetchDistrictsAsync(int provinceId, CancellationToken cancellationToken)
    {
        EnsureToken();
        _logger.LogInformation("GHN request districts provinceId={ProvinceId}", provinceId);
        var data = await SendAsync("master-data/district", HttpMethod.Post, new { province_id = provinceId }, cancellationToken);
        return data.EnumerateArray().Select(x => new DistrictDto(x.GetProperty("DistrictID").GetInt32(), x.GetProperty("DistrictName").GetString() ?? string.Empty, x.GetProperty("ProvinceID").GetInt32())).ToList();
    }

    private async Task<IReadOnlyList<WardDto>> FetchWardsAsync(int districtId, CancellationToken cancellationToken)
    {
        EnsureToken();
        _logger.LogInformation("GHN request wards districtId={DistrictId}", districtId);
        var data = await SendAsync($"master-data/ward?district_id={districtId}", HttpMethod.Get, null, cancellationToken);
        return data.EnumerateArray().Select(x => new WardDto(x.GetProperty("WardCode").GetString() ?? string.Empty, x.GetProperty("WardName").GetString() ?? string.Empty, districtId)).ToList();
    }

    private async Task<JsonElement> SendAsync(string path, HttpMethod method, object? body, CancellationToken cancellationToken)
    {
        using var request = new HttpRequestMessage(method, path);
        if (body != null) request.Content = JsonContent.Create(body);
        using var response = await _httpClient.SendAsync(request, cancellationToken);
        var raw = await response.Content.ReadAsStringAsync(cancellationToken);
        if (!response.IsSuccessStatusCode)
        {
            _logger.LogWarning("GHN API failed status={StatusCode}", (int)response.StatusCode);
            throw new InvalidOperationException("Không thể tải dữ liệu địa chỉ GHN lúc này. Vui lòng thử lại.");
        }

        using var doc = JsonDocument.Parse(raw);
        if (doc.RootElement.TryGetProperty("code", out var code) && code.GetInt32() != 200)
            throw new InvalidOperationException("Không thể tải dữ liệu địa chỉ GHN lúc này. Vui lòng thử lại.");

        return doc.RootElement.GetProperty("data").Clone();
    }
}
