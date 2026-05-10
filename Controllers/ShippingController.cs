using Datn.PcStore.Services;
using Microsoft.AspNetCore.Mvc;

namespace Datn.PcStore.Controllers;

[ApiController]
[Route("api/shipping")]
public class ShippingController : ControllerBase
{
    private readonly IShippingService _shippingService;
    private readonly IMapProvider _mapProvider;

    public ShippingController(IShippingService shippingService, IMapProvider mapProvider)
    {
        _shippingService = shippingService;
        _mapProvider = mapProvider;
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
        if (string.IsNullOrWhiteSpace(query) || query.Trim().Length < 3)
            return Ok(new { success = true, items = Array.Empty<object>() });

        try
        {
            var items = await _mapProvider.SearchAddressesAsync(query, provinceName, cancellationToken);
            return Ok(new
            {
                success = true,
                items = items.Select(x => new { display_name = x.DisplayName, lat = x.Latitude, lon = x.Longitude, full_address = x.FullAddress })
            });
        }
        catch
        {
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
