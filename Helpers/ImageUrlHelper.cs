namespace Datn.PcStore.Helpers;

public static class ImageUrlHelper
{
    public static string ResolveImageUrl(string? imageUrl, string fallback)
    {
        if (string.IsNullOrWhiteSpace(imageUrl)) return fallback;

        var trimmed = imageUrl.Trim();
        if (trimmed.StartsWith("http://", StringComparison.OrdinalIgnoreCase) ||
            trimmed.StartsWith("https://", StringComparison.OrdinalIgnoreCase))
        {
            return trimmed;
        }

        return trimmed.StartsWith("/")
            ? trimmed
            : "/" + trimmed.TrimStart('~', '/');
    }
}
