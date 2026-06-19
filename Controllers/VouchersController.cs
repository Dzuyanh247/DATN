using System.Security.Claims;
using Datn.PcStore.Services;
using Microsoft.AspNetCore.Mvc;

namespace Datn.PcStore.Controllers;

[ApiController]
[Route("api/vouchers")]
public class VouchersController : ControllerBase
{
    private readonly IVoucherService _voucherService;
    public VouchersController(IVoucherService voucherService) => _voucherService = voucherService;

    [HttpPost("validate")]
    public async Task<IActionResult> Validate([FromBody] VoucherRequest request, CancellationToken cancellationToken)
    {
        var userId = User.Identity?.IsAuthenticated == true ? int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!) : (int?)null;
        var result = await _voucherService.ValidateAsync(request.Code, request.Subtotal, request.ShippingFee, userId, cancellationToken);
        return Ok(new { success = result.Success, message = result.Message, code = result.Voucher?.Code, name = result.Voucher?.Name, discountAmount = result.DiscountAmount });
    }

    [HttpGet("available")]
    public async Task<IActionResult> Available([FromQuery] decimal subtotal, [FromQuery] decimal shippingFee, CancellationToken cancellationToken)
    {
        var userId = User.Identity?.IsAuthenticated == true ? int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!) : (int?)null;
        var results = await _voucherService.GetAvailableAsync(subtotal, shippingFee, userId, cancellationToken);
        return Ok(new { success = true, data = results.Select(x => new { code = x.Voucher!.Code, name = x.Voucher.Name, discountType = x.Voucher.DiscountType.ToString(), discountValue = x.Voucher.DiscountValue, maxDiscountAmount = x.Voucher.MaxDiscountAmount, minimumOrderAmount = x.Voucher.MinimumOrderAmount, endDate = x.Voucher.EndDate, discountAmount = x.DiscountAmount }) });
    }
}

public class VoucherRequest
{
    public string? Code { get; set; }
    public decimal Subtotal { get; set; }
    public decimal ShippingFee { get; set; }
}
