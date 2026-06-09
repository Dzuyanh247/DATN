using Datn.PcStore.Models;

namespace Datn.PcStore.Helpers;

public static class ProductPromotionHelper
{
    public const int MaxStoredLength = 2000;

    public static readonly IReadOnlyList<string> PresetTexts = new[]
    {
        "Tặng kèm key Windows 10 bản quyền",
        "Tặng kèm bộ phím chuột gaming",
        "Tặng kèm lót chuột gaming",
        "Upgrade SSD 512GB NVMe Gen4 thêm 1 triệu",
        "Tặng gói vệ sinh, bảo dưỡng miễn phí 12 tháng",
        "Hỗ trợ cài đặt phần mềm miễn phí"
    };

    public static readonly IReadOnlyList<string> DefaultTexts = new[]
    {
        PresetTexts[0],
        PresetTexts[5]
    };

    public static List<string> GetPromotionLines(Product product)
    {
        var lines = SplitLines(product.PromotionText);
        return lines.Count > 0 ? lines : DefaultTexts.ToList();
    }

    public static string BuildStoredText(IEnumerable<string>? selectedPresetTexts, string? customText)
    {
        var selected = (selectedPresetTexts ?? Array.Empty<string>())
            .Where(text => PresetTexts.Contains(text, StringComparer.Ordinal))
            .ToList();

        return string.Join(Environment.NewLine, selected
            .Concat(SplitLines(customText))
            .Distinct(StringComparer.OrdinalIgnoreCase));
    }

    public static List<string> GetSelectedPresetTexts(string? promotionText)
    {
        return SplitLines(promotionText)
            .Where(text => PresetTexts.Contains(text, StringComparer.Ordinal))
            .ToList();
    }

    public static string GetCustomText(string? promotionText)
    {
        return string.Join(Environment.NewLine, SplitLines(promotionText)
            .Where(text => !PresetTexts.Contains(text, StringComparer.Ordinal)));
    }

    public static List<string> SplitLines(string? text)
    {
        if (string.IsNullOrWhiteSpace(text)) return new List<string>();

        return text
            .Replace("||", "\n", StringComparison.Ordinal)
            .Split(new[] { '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
            .Select(line => line.Trim().TrimStart('-', '•', '*', '✓', '✔'))
            .Where(line => !string.IsNullOrWhiteSpace(line))
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToList();
    }
}
