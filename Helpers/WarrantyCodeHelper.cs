namespace Datn.PcStore.Helpers;

public static class WarrantyCodeHelper
{
    public static string BuildWarrantyCode(int orderId, int orderDetailId) =>
        $"BH-DH{orderId:D6}-CT{orderDetailId:D6}";

    public static string BuildRequestCode(int requestId) => $"YCBH{requestId:D6}";

    public static int? ParseWarrantyDetailId(string? value)
    {
        if (string.IsNullOrWhiteSpace(value)) return null;
        var markerIndex = value.LastIndexOf("-CT", StringComparison.OrdinalIgnoreCase);
        return markerIndex >= 0 && int.TryParse(value[(markerIndex + 3)..], out var id) ? id : null;
    }
}
