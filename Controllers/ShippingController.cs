using Datn.PcStore.Services;
using Microsoft.AspNetCore.Mvc;

namespace Datn.PcStore.Controllers;

[ApiController]
[Route("api/shipping")]
public class ShippingController : ControllerBase
{
    private readonly IShippingService _shippingService;
    private readonly IMapProvider _mapProvider;
    private readonly ILogger<ShippingController> _logger;

    public ShippingController(IShippingService shippingService, IMapProvider mapProvider, ILogger<ShippingController> logger)
    {
        _shippingService = shippingService;
        _mapProvider = mapProvider;
        _logger = logger;
    }

    [HttpPost("calculate")]
    public async Task<IActionResult> Calculate([FromBody] ShippingCalculateRequest request, CancellationToken cancellationToken)
    {
        if (request == null || string.IsNullOrWhiteSpace(request.Address))
            return BadRequest(new { success = false, message = "Địa chỉ giao hàng là bắt buộc." });

        try
        {
            var fallbackAddress = request.FullAddress;
            if (!request.Latitude.HasValue || !request.Longitude.HasValue)
            {
                fallbackAddress = string.IsNullOrWhiteSpace(request.FullAddress)
                    ? string.Join(", ", new[] { request.AddressDetail, request.WardName, request.ProvinceName, "Việt Nam" }.Where(x => !string.IsNullOrWhiteSpace(x)))
                    : request.FullAddress;
            }

            var quote = await _shippingService.CalculateAsync(fallbackAddress ?? request.Address, request.Latitude, request.Longitude, cancellationToken);
            return Ok(new
            {
                success = true,
                destination_latitude = quote.DestinationLatitude,
                destination_longitude = quote.DestinationLongitude,
                distance_km = quote.DistanceKm,
                duration_minutes = quote.DurationMinutes,
                shipping_fee = quote.ShippingFee,
                shipping_provider = quote.Provider,
                shipping_formula_snapshot = quote.FormulaSnapshot
            });
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { success = false, message = ex.Message });
        }
        catch (Exception)
        {
            return StatusCode(500, new { success = false, message = "Không thể tính phí giao hàng lúc này. Vui lòng thử lại." });
        }
    }

    [HttpGet("autocomplete")]
    public async Task<IActionResult> Autocomplete([FromQuery] string query, [FromQuery] string? provinceName, CancellationToken cancellationToken)
    {
        var normalizedQuery = string.Join(" ", (query ?? string.Empty).Trim().Split(' ', StringSplitOptions.RemoveEmptyEntries));
        if (string.IsNullOrWhiteSpace(normalizedQuery) || normalizedQuery.Length < 3)
            return Ok(new { success = true, items = Array.Empty<object>() });

        try
        {
            _logger.LogInformation("Shipping autocomplete query='{Query}' province='{Province}'", normalizedQuery, provinceName);
            var items = await _mapProvider.SearchAddressesAsync(normalizedQuery, provinceName, cancellationToken);
            var mapped = items.Select(x => new { label = x.DisplayName, latitude = x.Latitude, longitude = x.Longitude }).ToList();
            _logger.LogInformation("Shipping autocomplete mapped_suggestions_count={Count}", mapped.Count);

            return Ok(new
            {
                success = true,
                items = mapped
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Shipping autocomplete failed for query='{Query}'", normalizedQuery);
            return StatusCode(500, new { success = false, message = "Không thể tính phí giao hàng lúc này. Vui lòng thử lại." });
        }
    }
}

public class ShippingCalculateRequest
{
    public string Address { get; set; } = string.Empty;
    public string? AddressDetail { get; set; }
    public string? FullAddress { get; set; }
    public string? ProvinceName { get; set; }
    public string? WardName { get; set; }
    public double? Latitude { get; set; }
    public double? Longitude { get; set; }
}
