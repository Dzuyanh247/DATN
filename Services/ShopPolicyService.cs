using System.Globalization;

namespace Datn.PcStore.Services;

public record ShopPolicyAnswer(bool IsPolicyQuestion, string Reply);

public interface IShopPolicyService
{
    ShopPolicyAnswer Answer(string message);
    string BuildKnowledgePrompt();
}

public partial class ShopPolicyService : IShopPolicyService
{
    public const string UnknownPolicyReply = "Hiện tại KKSHOP chưa hỗ trợ hoặc chưa có thông tin xác nhận về nội dung này.";

    private static readonly IReadOnlyDictionary<string, string> Policies = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
    {
        ["installment"] = "Hiện tại KKSHOP chưa hỗ trợ trả góp.",
        ["free_shipping"] = UnknownPolicyReply,
        ["return_exchange"] = UnknownPolicyReply,
        ["express_delivery"] = UnknownPolicyReply,
        ["onsite_warranty"] = UnknownPolicyReply,
        ["payment"] = "KKSHOP hỗ trợ thanh toán theo các phương thức đang hiển thị ở bước thanh toán đơn hàng. Nếu cần kiểm tra đơn cụ thể, vui lòng dùng chức năng Hỗ trợ thanh toán hoặc Gặp nhân viên."
    };

    public ShopPolicyAnswer Answer(string message)
    {
        var normalized = Normalize(message);
        var key = DetectPolicyKey(normalized);
        return key == null ? new(false, string.Empty) : new(true, Policies.GetValueOrDefault(key, UnknownPolicyReply));
    }

    public string BuildKnowledgePrompt() => string.Join("\n", Policies.Select(item => $"- {item.Key}: {item.Value}"));

    private static string? DetectPolicyKey(string normalized)
    {
        if (ContainsAny(normalized, "tra gop", "tragop", "installment", "mua gop")) return "installment";
        if (ContainsAny(normalized, "freeship", "free ship", "mien phi ship", "mien phi van chuyen")) return "free_shipping";
        if (ContainsAny(normalized, "doi tra", "hoan tra", "return", "refund")) return "return_exchange";
        if (ContainsAny(normalized, "hoa toc", "giao nhanh", "express")) return "express_delivery";
        if (ContainsAny(normalized, "bao hanh tan noi", "tan noi")) return "onsite_warranty";
        if (ContainsAny(normalized, "thanh toan", "chuyen khoan", "cod")) return "payment";
        return null;
    }

    private static bool ContainsAny(string source, params string[] values) => values.Any(source.Contains);

    private static string Normalize(string value)
    {
        var normalized = (value ?? string.Empty).Replace('đ', 'd').Replace('Đ', 'D').Normalize(System.Text.NormalizationForm.FormD);
        return new string(normalized.Where(c => CharUnicodeInfo.GetUnicodeCategory(c) != UnicodeCategory.NonSpacingMark).ToArray()).Normalize(System.Text.NormalizationForm.FormC).ToLowerInvariant();
    }
}
