using Datn.PcStore.Services;
using Microsoft.AspNetCore.Mvc;

namespace Datn.PcStore.Controllers;

[ApiController]
[Route("api/shipping")]
public class ShippingController : ControllerBase
{
    private readonly IShippingService _shippingService;

    public ShippingController(IShippingService shippingService)
    {
        _shippingService = shippingService;
    }

    [HttpPost("calculate")]
    public async Task<IActionResult> Calculate([FromBody] ShippingCalculateRequest request, CancellationToken cancellationToken)
    {
        if (request == null || string.IsNullOrWhiteSpace(request.Address))
            return BadRequest(new { message = "Địa chỉ giao hàng là bắt buộc." });

        try
        {
            var quote = await _shippingService.CalculateAsync(request.Address, request.Latitude, request.Longitude, cancellationToken);
            return Ok(new
            {
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
            return BadRequest(new { message = ex.Message });
        }
    }
}

public class ShippingCalculateRequest
{
    public string Address { get; set; } = string.Empty;
    public string? FullAddress { get; set; }
    public double? Latitude { get; set; }
    public double? Longitude { get; set; }
}
