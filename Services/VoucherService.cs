using Datn.PcStore.Data;
using Datn.PcStore.Models;
using Microsoft.EntityFrameworkCore;

namespace Datn.PcStore.Services;

public record VoucherValidationResult(bool Success, string Message, Voucher? Voucher, decimal DiscountAmount);

public interface IVoucherService
{
    Task<VoucherValidationResult> ValidateAsync(string? code, decimal subtotal, decimal shippingFee, int? userId, CancellationToken cancellationToken = default);
    Task<List<VoucherValidationResult>> GetAvailableAsync(decimal subtotal, decimal shippingFee, int? userId, CancellationToken cancellationToken = default);
}

public class VoucherService : IVoucherService
{
    private readonly ApplicationDbContext _db;
    public VoucherService(ApplicationDbContext db) => _db = db;

    public async Task<List<VoucherValidationResult>> GetAvailableAsync(decimal subtotal, decimal shippingFee, int? userId, CancellationToken cancellationToken = default)
    {
        var now = DateTime.Now;
        var vouchers = await _db.Vouchers
            .Where(x => x.IsActive && x.StartDate <= now && x.EndDate >= now)
            .OrderBy(x => x.EndDate)
            .ToListAsync(cancellationToken);
        var results = new List<VoucherValidationResult>();
        foreach (var voucher in vouchers)
        {
            var result = await ValidateVoucherAsync(voucher, subtotal, shippingFee, userId, cancellationToken);
            if (result.Success) results.Add(result);
        }
        return results;
    }

    public async Task<VoucherValidationResult> ValidateAsync(string? code, decimal subtotal, decimal shippingFee, int? userId, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(code)) return new(false, "Vui lòng nhập mã voucher.", null, 0m);
        var normalized = NormalizeCode(code);
        var voucher = await _db.Vouchers.FirstOrDefaultAsync(x => x.Code.Trim().ToUpper() == normalized, cancellationToken);
        if (voucher == null) return new(false, "Mã voucher không tồn tại.", null, 0m);
        return await ValidateVoucherAsync(voucher, subtotal, shippingFee, userId, cancellationToken);
    }

    private async Task<VoucherValidationResult> ValidateVoucherAsync(Voucher voucher, decimal subtotal, decimal shippingFee, int? userId, CancellationToken cancellationToken)
    {
        var now = DateTime.Now;
        if (!voucher.IsActive) return new(false, "Voucher đang tạm tắt.", voucher, 0m);
        if (voucher.StartDate > now) return new(false, "Voucher chưa đến thời gian sử dụng.", voucher, 0m);
        if (voucher.EndDate < now) return new(false, "Voucher đã hết hạn.", voucher, 0m);
        if (voucher.Quantity != int.MaxValue && (voucher.Quantity <= 0 || voucher.UsedCount >= voucher.Quantity)) return new(false, "Voucher đã hết lượt sử dụng.", voucher, 0m);
        if (voucher.MinimumOrderAmount > 0 && subtotal < voucher.MinimumOrderAmount) return new(false, "Đơn hàng chưa đạt giá trị tối thiểu để dùng voucher.", voucher, 0m);
        if (userId.HasValue && voucher.MaxUsagePerUser.GetValueOrDefault() > 0)
        {
            var usedByUser = await _db.VoucherUsages.CountAsync(x => x.VoucherId == voucher.Id && x.UserId == userId.Value, cancellationToken);
            if (usedByUser >= voucher.MaxUsagePerUser!.Value) return new(false, "Bạn đã dùng voucher này quá số lần cho phép.", voucher, 0m);
        }
        var baseAmount = Math.Max(subtotal + shippingFee, 0m);
        var discount = voucher.DiscountType == VoucherDiscountType.Percent ? subtotal * voucher.DiscountValue / 100m : voucher.DiscountValue;
        if (voucher.DiscountType == VoucherDiscountType.Percent && voucher.MaxDiscountAmount.HasValue) discount = Math.Min(discount, voucher.MaxDiscountAmount.Value);
        discount = Math.Min(Math.Max(discount, 0m), baseAmount);
        return new(true, "Áp dụng voucher thành công.", voucher, discount);
    }

    private static string NormalizeCode(string code) => code.Trim().ToUpperInvariant();
}
