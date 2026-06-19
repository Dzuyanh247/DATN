using System.Net.Http.Json;
using System.Text.Json;
using Microsoft.Extensions.Options;

namespace Datn.PcStore.Services;

public interface IGhnShippingService
{
    Task<GhnShippingFeeResult> CalculateFeeAsync(int toDistrictId, string toWardCode, int weight, int length, int width, int height, CancellationToken cancellationToken = default);
}

public class GhnShippingService : IGhnShippingService
{
    private readonly HttpClient _httpClient;
    private readonly GhnOptions _options;
    private readonly ILogger<GhnShippingService> _logger;

    public GhnShippingService(HttpClient httpClient, IOptions<GhnOptions> options, ILogger<GhnShippingService> logger)
    {
        _httpClient = httpClient;
        _options = options.Value;
        _logger = logger;
    }

    public async Task<GhnShippingFeeResult> CalculateFeeAsync(int toDistrictId, string toWardCode, int weight, int length, int width, int height, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(_options.Token) || string.IsNullOrWhiteSpace(_options.ShopId))
        {
            _logger.LogError("GHN configuration missing Token or ShopId. tokenConfigured={TokenConfigured} shopIdConfigured={ShopIdConfigured}", !string.IsNullOrWhiteSpace(_options.Token), !string.IsNullOrWhiteSpace(_options.ShopId));
            return GhnShippingFeeResult.Fail("Thiếu cấu hình GHN (Token/ShopId).");
        }

        var body = new Dictionary<string, object?>
        {
            ["to_district_id"] = toDistrictId,
            ["to_ward_code"] = toWardCode,
            ["weight"] = weight,
            ["length"] = length,
            ["width"] = width,
            ["height"] = height,
            ["insurance_value"] = Math.Max(0, _options.InsuranceValue)
        };
        if (_options.ServiceId.HasValue && _options.ServiceId.Value > 0)
        {
            body["service_id"] = _options.ServiceId.Value;
        }
        else
        {
            body["service_type_id"] = _options.ServiceTypeId > 0 ? _options.ServiceTypeId : 2;
        }
        if (_options.FromDistrictId.HasValue && _options.FromDistrictId.Value > 0)
        {
            body["from_district_id"] = _options.FromDistrictId.Value;
        }
        if (!string.IsNullOrWhiteSpace(_options.FromWardCode))
        {
            body["from_ward_code"] = _options.FromWardCode;
        }
        var endpoint = "v2/shipping-order/fee";
        var requestUri = new Uri(_httpClient.BaseAddress!, endpoint);
        var jsonBody = JsonSerializer.Serialize(body);

        _logger.LogWarning("GHN fee request url={Url} endpoint={Endpoint} shopIdConfigured={ShopIdConfigured} tokenConfigured={TokenConfigured} tokenPrefix={TokenPrefix} fromDistrictId={FromDistrictId} fromWardCode={FromWardCode} serviceId={ServiceId} serviceTypeId={ServiceTypeId} districtId={DistrictId} wardCode={WardCode} weight={Weight} size={Length}x{Width}x{Height} insuranceValue={InsuranceValue} body={Body}", requestUri, requestUri.AbsolutePath, !string.IsNullOrWhiteSpace(_options.ShopId), !string.IsNullOrWhiteSpace(_options.Token), MaskTokenPrefix(_options.Token), _options.FromDistrictId, _options.FromWardCode, _options.ServiceId, _options.ServiceTypeId, toDistrictId, toWardCode, weight, length, width, height, _options.InsuranceValue, jsonBody);
        using var request = new HttpRequestMessage(HttpMethod.Post, endpoint);
        request.Headers.Remove("Token");
        request.Headers.Remove("ShopId");
        request.Headers.Add("Token", _options.Token);
        request.Headers.Add("ShopId", _options.ShopId);
        _logger.LogWarning("GHN fee request headers tokenPresent={TokenPresent} shopIdPresent={ShopIdPresent} shopIdHeaderName={ShopIdHeaderName}", request.Headers.Contains("Token"), request.Headers.Contains("ShopId"), "ShopId");
        request.Content = JsonContent.Create(body);

        using var response = await _httpClient.SendAsync(request, cancellationToken);
        var raw = await response.Content.ReadAsStringAsync(cancellationToken);
        _logger.LogWarning("GHN fee raw response status={StatusCode} body={Body}", (int)response.StatusCode, raw);
        if (!response.IsSuccessStatusCode)
        {
            _logger.LogWarning("GHN fee API HTTP failed status={StatusCode} reason={ReasonPhrase} body={Body}", (int)response.StatusCode, response.ReasonPhrase, raw);
            return GhnShippingFeeResult.Fail($"GHN HTTP {(int)response.StatusCode}: {ExtractMessage(raw) ?? response.ReasonPhrase ?? "Không phản hồi hợp lệ"}");
        }

        using var doc = JsonDocument.Parse(raw);
        var root = doc.RootElement;
        var code = root.TryGetProperty("code", out var c) ? c.GetInt32() : 0;
        if (code != 200)
        {
            var message = root.TryGetProperty("message", out var m) ? m.GetString() : "GHN fee failed";
            _logger.LogWarning("GHN fee business failed code={Code} message={Message} body={Body}", code, message, raw);
            return GhnShippingFeeResult.Fail(message ?? "GHN fee failed");
        }

        var data = root.GetProperty("data");
        var total = data.TryGetProperty("total", out var totalEl)
            ? totalEl.GetDecimal()
            : data.TryGetProperty("service_fee", out var serviceFeeEl)
                ? serviceFeeEl.GetDecimal()
                : 0m;
        var result = new GhnShippingFeeResult
        {
            Success = true,
            ShippingFee = total,
            Total = total,
            ServiceFee = data.TryGetProperty("service_fee", out var s) ? s.GetDecimal() : null,
            InsuranceFee = data.TryGetProperty("insurance_fee", out var i) ? i.GetDecimal() : null,
            LeadTime = data.TryGetProperty("leadtime", out var l) ? l.GetInt64() : null
        };
        _logger.LogInformation("GHN fee response total={Total} serviceFee={ServiceFee} insuranceFee={InsuranceFee} shippingFee={ShippingFee}", result.Total, result.ServiceFee, result.InsuranceFee, result.ShippingFee);
        return result;
    }

    private static string? ExtractMessage(string raw)
    {
        try
        {
            using var doc = JsonDocument.Parse(raw);
            return doc.RootElement.TryGetProperty("message", out var message) ? message.GetString() : null;
        }
        catch (JsonException)
        {
            return null;
        }
    }

    private static string MaskTokenPrefix(string? token)
    {
        if (string.IsNullOrWhiteSpace(token)) return string.Empty;
        return token.Length <= 8 ? "***" : token[..8] + "***";
    }
}

public class GhnShippingFeeResult
{
    public bool Success { get; set; }
    public decimal ShippingFee { get; set; }
    public decimal? Total { get; set; }
    public decimal? ServiceFee { get; set; }
    public decimal? InsuranceFee { get; set; }
    public long? LeadTime { get; set; }
    public string? ErrorMessage { get; set; }

    public static GhnShippingFeeResult Fail(string error) => new() { Success = false, ErrorMessage = error };
}
