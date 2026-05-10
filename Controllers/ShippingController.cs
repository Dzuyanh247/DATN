using Datn.PcStore.Services;
using Microsoft.AspNetCore.Mvc;

namespace Datn.PcStore.Controllers;

[ApiController]
[Route("api/shipping")]
public class ShippingController : ControllerBase
{
    private readonly IShippingService _shippingService;
    private readonly IGhnAddressService _ghnAddressService;
    private readonly ILogger<ShippingController> _logger;

    public ShippingController(IShippingService shippingService, IGhnAddressService ghnAddressService, ILogger<ShippingController> logger)
    {
        _shippingService = shippingService;
        _ghnAddressService = ghnAddressService;
        _logger = logger;
    }

    [HttpGet("provinces")]
    public async Task<IActionResult> GetProvinces(CancellationToken cancellationToken)
    {
        try { return Ok(new { success = true, data = await _ghnAddressService.GetProvincesAsync(cancellationToken) }); }
        catch (Exception ex) { return Ok(new { success = false, message = ex.Message }); }
    }

    [HttpGet("districts")]
    public async Task<IActionResult> GetDistricts([FromQuery] int provinceId, CancellationToken cancellationToken)
    {
        if (provinceId <= 0) return Ok(new { success = false, message = "provinceId is required." });
        try { return Ok(new { success = true, data = await _ghnAddressService.GetDistrictsAsync(provinceId, cancellationToken) }); }
        catch (Exception ex) { return Ok(new { success = false, message = ex.Message }); }
    }

    [HttpGet("wards")]
    public async Task<IActionResult> GetWards([FromQuery] int districtId, CancellationToken cancellationToken)
    {
        if (districtId <= 0) return Ok(new { success = false, message = "districtId is required." });
        try { return Ok(new { success = true, data = await _ghnAddressService.GetWardsAsync(districtId, cancellationToken) }); }
        catch (Exception ex) { return Ok(new { success = false, message = ex.Message }); }
    }

    [HttpPost("calculate")]
    public async Task<IActionResult> Calculate([FromBody] ShippingCalculateRequest? request, CancellationToken cancellationToken)
    {
        if (request == null) return Ok(new { success = false, message = "Payload không hợp lệ." });
        if (request.ProvinceId <= 0) return Ok(new { success = false, message = "provinceId is required." });
        if (request.DistrictId <= 0) return Ok(new { success = false, message = "districtId is required." });
        if (string.IsNullOrWhiteSpace(request.WardCode)) return Ok(new { success = false, message = "wardCode is required." });
        if (string.IsNullOrWhiteSpace(request.AddressDetail)) return Ok(new { success = false, message = "addressDetail is required." });

        var fullAddress = $"{request.AddressDetail.Trim()}, {request.WardName}, {request.DistrictName}, {request.ProvinceName}, Vietnam";
        _logger.LogInformation("Shipping calculate input provinceId={ProvinceId} districtId={DistrictId} wardCode={WardCode}", request.ProvinceId, request.DistrictId, request.WardCode);

        try
        {
            var quote = await _shippingService.CalculateAsync(fullAddress, request.ProvinceName ?? string.Empty, request.DistrictName ?? string.Empty, request.WardName ?? string.Empty, cancellationToken);
            return Ok(new { success = true, message = "Tính phí giao hàng thành công.", distanceKm = quote.DistanceKm, durationMinutes = quote.DurationMinutes, shippingFee = quote.ShippingFee, shippingProvider = quote.Provider, formula = quote.FormulaSnapshot });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Shipping calculate failed");
            return Ok(new { success = false, message = ex.Message });
        }
    }
}

public class ShippingCalculateRequest
{
    public string? FullName { get; set; }
    public string? Phone { get; set; }
    public string? Email { get; set; }
    public int ProvinceId { get; set; }
    public string? ProvinceName { get; set; }
    public int DistrictId { get; set; }
    public string? DistrictName { get; set; }
    public string? WardCode { get; set; }
    public string? WardName { get; set; }
    public string? AddressDetail { get; set; }
}
