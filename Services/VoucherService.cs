using Datn.PcStore.Data;
using Datn.PcStore.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace Datn.PcStore.Services;

public record VoucherValidationResult(bool Success, string Message, Voucher? Voucher, decimal DiscountAmount, string? FailureCode = null);

public interface IVoucherService
{
    Task<VoucherValidationResult> ValidateAsync(string? code, decimal subtotal, decimal shippingFee, int? userId, CancellationToken cancellationToken = default);
    Task<List<VoucherValidationResult>> GetAvailableAsync(decimal subtotal, decimal shippingFee, int? userId, CancellationToken cancellationToken = default);
}

public class VoucherService : IVoucherService
{
    private readonly ApplicationDbContext _db;
    private readonly ILogger<VoucherService> _logger;

    public VoucherService(ApplicationDbContext db, ILogger<VoucherService> logger)
    {
        _db = db;
        _logger = logger;
    }

    public async Task<List<VoucherValidationResult>> GetAvailableAsync(decimal subtotal, decimal shippingFee, int? userId, CancellationToken cancellationToken = default)
    {
        var vouchers = await _db.Vouchers
            .OrderBy(x => x.EndDate)
            .ToListAsync(cancellationToken);
        var results = new List<VoucherValidationResult>();
        foreach (var voucher in vouchers)
        {
            NormalizeStoredCode(voucher);
            var result = await ValidateVoucherAsync(voucher, subtotal, shippingFee, userId, cancellationToken);
            LogVoucherDecision(result, subtotal, shippingFee, userId);
            if (result.Success) results.Add(result);
        }
        return results;
    }

    public async Task<VoucherValidationResult> ValidateAsync(string? code, decimal subtotal, decimal shippingFee, int? userId, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(code)) return new(false, "Vui lòng nhập mã voucher.", null, 0m, "EMPTY_CODE");
        var normalized = NormalizeCode(code);
        var vouchers = await _db.Vouchers.ToListAsync(cancellationToken);
        var voucher = vouchers.FirstOrDefault(x => NormalizeCode(x.Code) == normalized);
        if (voucher == null)
        {
            var missingResult = new VoucherValidationResult(false, "Không tồn tại mã voucher.", null, 0m, "CODE_NOT_FOUND");
            LogVoucherDecision(missingResult, subtotal, shippingFee, userId, normalized);
            return missingResult;
        }

        NormalizeStoredCode(voucher);
        var result = await ValidateVoucherAsync(voucher, subtotal, shippingFee, userId, cancellationToken);
        LogVoucherDecision(result, subtotal, shippingFee, userId, normalized);
        return result;
    }

    private async Task<VoucherValidationResult> ValidateVoucherAsync(Voucher voucher, decimal subtotal, decimal shippingFee, int? userId, CancellationToken cancellationToken)
    {
        var now = DateTime.Now;
        if (!voucher.IsActive) return new(false, "Voucher chưa hoạt động.", voucher, 0m, "INACTIVE");
        if (voucher.StartDate > now) return new(false, "Voucher chưa đến thời gian sử dụng.", voucher, 0m, "NOT_STARTED");
        if (voucher.EndDate < now) return new(false, "Voucher đã hết hạn.", voucher, 0m, "EXPIRED");
        if (voucher.Quantity != int.MaxValue && (voucher.Quantity <= 0 || voucher.UsedCount >= voucher.Quantity)) return new(false, "Voucher đã hết lượt.", voucher, 0m, "OUT_OF_QUANTITY");
        if (voucher.MinimumOrderAmount > 0 && subtotal < voucher.MinimumOrderAmount) return new(false, $"Đơn chưa đạt tối thiểu {voucher.MinimumOrderAmount:N0} VNĐ.", voucher, 0m, "MIN_ORDER_NOT_MET");
        if (userId.HasValue && voucher.MaxUsagePerUser.GetValueOrDefault() > 0)
        {
            var usedByUser = await _db.VoucherUsages.CountAsync(x => x.VoucherId == voucher.Id && x.UserId == userId.Value, cancellationToken);
            if (usedByUser >= voucher.MaxUsagePerUser!.Value) return new(false, "Đã vượt giới hạn sử dụng.", voucher, 0m, "USER_USAGE_LIMIT");
        }

        var baseAmount = Math.Max(subtotal + shippingFee, 0m);
        if (baseAmount <= 0m) return new(false, "Tổng tiền đơn hàng không hợp lệ.", voucher, 0m, "INVALID_TOTAL");
        var discount = voucher.DiscountType == VoucherDiscountType.Percent ? subtotal * voucher.DiscountValue / 100m : voucher.DiscountValue;
        if (voucher.DiscountType == VoucherDiscountType.Percent && voucher.MaxDiscountAmount.HasValue) discount = Math.Min(discount, voucher.MaxDiscountAmount.Value);
        discount = Math.Min(Math.Max(discount, 0m), baseAmount);
        return new(true, "Áp dụng voucher thành công.", voucher, discount);
    }

    private void NormalizeStoredCode(Voucher voucher)
    {
        var normalized = NormalizeCode(voucher.Code);
        if (voucher.Code != normalized) voucher.Code = normalized;
    }

    private void LogVoucherDecision(VoucherValidationResult result, decimal subtotal, decimal shippingFee, int? userId, string? requestedCode = null)
    {
        _logger.LogInformation("Voucher decision: success={Success}, failure={FailureCode}, requestedCode={RequestedCode}, voucherId={VoucherId}, voucherCode={VoucherCode}, isActive={IsActive}, startDate={StartDate}, endDate={EndDate}, minimumOrderAmount={MinimumOrderAmount}, subtotal={Subtotal}, shippingFee={ShippingFee}, quantity={Quantity}, usedCount={UsedCount}, maxUsagePerUser={MaxUsagePerUser}, userId={UserId}, discountType={DiscountType}, discountValue={DiscountValue}",
            result.Success,
            result.FailureCode,
            requestedCode,
            result.Voucher?.Id,
            result.Voucher?.Code,
            result.Voucher?.IsActive,
            result.Voucher?.StartDate,
            result.Voucher?.EndDate,
            result.Voucher?.MinimumOrderAmount,
            subtotal,
            shippingFee,
            result.Voucher?.Quantity,
            result.Voucher?.UsedCount,
            result.Voucher?.MaxUsagePerUser,
            userId,
            result.Voucher?.DiscountType,
            result.Voucher?.DiscountValue);
    }

    private static string NormalizeCode(string? code) => (code ?? string.Empty).Trim().ToUpperInvariant();
}
