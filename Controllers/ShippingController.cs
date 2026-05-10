using Datn.PcStore.Services;
using Microsoft.AspNetCore.Mvc;
using System.Text.Json.Serialization;

namespace Datn.PcStore.Controllers;

[ApiController]
[Route("api/shipping")]
public class ShippingController : ControllerBase
{
    private readonly IShippingService _shippingService;
    private readonly IMapProvider _mapProvider;
    private readonly ILogger<ShippingController> _logger;

    public ShippingController(
        IShippingService shippingService,
        IMapProvider mapProvider,
        ILogger<ShippingController> logger)
    {
        _shippingService = shippingService;
        _mapProvider = mapProvider;
        _logger = logger;
    }

    [HttpPost("calculate")]
    public async Task<IActionResult> Calculate(
        [FromBody] ShippingCalculateRequest? request,
        CancellationToken cancellationToken)
    {
        if (request == null)
        {
            return Ok(new { success = false, message = "Payload không hợp lệ." });
        }

        request.NormalizeAliases();
        _logger.LogInformation("Shipping calculate payload {@Request}", request);

        if (string.IsNullOrWhiteSpace(request.Address)
            && string.IsNullOrWhiteSpace(request.FullAddress)
            && string.IsNullOrWhiteSpace(request.AddressDetail))
        {
            return Ok(new { success = false, message = "Địa chỉ giao hàng là bắt buộc." });
        }

        var fullAddress = AddressQueryHelper.BuildNormalizedAddress(request.FullAddress, request.WardName, request.ProvinceName);
        if (string.IsNullOrWhiteSpace(request.FullAddress))
        {
            fullAddress = AddressQueryHelper.BuildNormalizedAddress(request.AddressDetail, request.WardName, request.ProvinceName);
        }

        var hasCoordinates = request.Latitude.HasValue && request.Longitude.HasValue;
        _logger.LogInformation(
            "Shipping calculate hasCoordinates={HasCoordinates} fallbackAddress='{Address}'",
            hasCoordinates,
            fullAddress);

        if (hasCoordinates && (request.Latitude is < -90 or > 90 || request.Longitude is < -180 or > 180))
            return Ok(new { success = false, message = "Tọa độ không hợp lệ." });

        var provinceFold = AddressQueryHelper.Fold(request.ProvinceName);
        var fullFold = AddressQueryHelper.Fold(request.FullAddress ?? request.Address ?? string.Empty);
        if (provinceFold.Contains("ha noi") && fullFold.Contains("ho chi minh"))
            return Ok(new { success = false, message = "Không tìm thấy địa chỉ phù hợp trong khu vực đã chọn" });

        try
        {
            var quote = await _shippingService.CalculateAsync(
                AddressQueryHelper.NormalizeSegment(fullAddress ?? request.Address ?? string.Empty),
                request.ProvinceName,
                request.WardName,
                request.Latitude,
                request.Longitude,
                cancellationToken);

            _logger.LogInformation(
                "Shipping calculate success distanceKm={DistanceKm} durationMinutes={DurationMinutes} fee={Fee}",
                quote.DistanceKm,
                quote.DurationMinutes,
                quote.ShippingFee);

            return Ok(new
            {
                success = true,
                message = "Tính phí giao hàng thành công.",
                destinationLatitude = quote.DestinationLatitude,
                destinationLongitude = quote.DestinationLongitude,
                distanceKm = quote.DistanceKm,
                durationMinutes = quote.DurationMinutes,
                shippingFee = quote.ShippingFee,
                shippingProvider = quote.Provider,
                formula = quote.FormulaSnapshot,
            });
        }
        catch (InvalidOperationException ex)
        {
            _logger.LogWarning(ex, "Shipping calculate failed due to invalid operation. request={@Request}", request);
            return Ok(new { success = false, message = ex.Message });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Shipping calculate unexpected error.");
            return Ok(new { success = false, message = "Không thể tính phí giao hàng lúc này. Vui lòng thử lại." });
        }
    }

    [HttpGet("autocomplete")]
    public async Task<IActionResult> Autocomplete(
        [FromQuery] string query,
        [FromQuery] string? wardName,
        [FromQuery] string? provinceName,
        CancellationToken cancellationToken)
    {
        var normalizedQuery = string.Join(
            " ",
            (query ?? string.Empty)
                .Trim()
                .Split(' ', StringSplitOptions.RemoveEmptyEntries));

        _logger.LogInformation(
            "Shipping autocomplete frontend_query_received='{Query}' ward='{Ward}' province='{Province}'",
            normalizedQuery,
            wardName,
            provinceName);

        if (string.IsNullOrWhiteSpace(normalizedQuery) || normalizedQuery.Length < 3)
        {
            return Ok(new { success = true, suggestions = Array.Empty<object>() });
        }

        try
        {
            var composedQuery = AddressQueryHelper.BuildNormalizedAddress(normalizedQuery, wardName, provinceName);

            _logger.LogInformation(
                "Shipping autocomplete backend query='{Query}' ward='{Ward}' province='{Province}'",
                composedQuery,
                wardName,
                provinceName);

            var searchResult = await _mapProvider.SearchAddressesAsync(composedQuery, provinceName, cancellationToken);
            var mapped = searchResult.Suggestions
                .Select(x => new { label = x.DisplayName, latitude = x.Latitude, longitude = x.Longitude })
                .ToList();

            _logger.LogInformation(
                "Shipping autocomplete suggestions_count={Count} query_used='{QueryUsed}'",
                mapped.Count,
                searchResult.QueryUsed);

            return Ok(new { success = true, suggestions = mapped });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Shipping autocomplete failed for query='{Query}'", normalizedQuery);
            return Ok(new
            {
                success = false,
                message = "Không thể gợi ý địa chỉ lúc này. Vui lòng thử lại.",
                suggestions = Array.Empty<object>(),
            });
        }
    }

    private static string NormalizeAddress(string raw)
    {
        return System.Text.RegularExpressions.Regex
            .Replace((raw ?? string.Empty).Replace(",,", ","), "\\s*,\\s*", ", ")
            .Trim(',', ' ');
    }
}

public class ShippingCalculateRequest
{
    public string? Address { get; set; }

    public string? AddressDetail { get; set; }

    public string? FullAddress { get; set; }

    public string? ProvinceName { get; set; }

    public string? WardName { get; set; }

    [JsonPropertyName("ward")]
    public string? Ward { get; set; }

    [JsonPropertyName("provinceCity")]
    public string? ProvinceCity { get; set; }

    public double? Latitude { get; set; }

    public double? Longitude { get; set; }

    public void NormalizeAliases()
    {
        WardName ??= Ward;
        ProvinceName ??= ProvinceCity;
    }
}
