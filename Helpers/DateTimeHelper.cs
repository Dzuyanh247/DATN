namespace Datn.PcStore.Helpers;

public static class DateTimeHelper
{
    private static readonly TimeZoneInfo VietnamTimeZone = ResolveVietnamTimeZone();

    public static DateTime UtcNow() => DateTime.UtcNow;

    public static DateTime GetVietnamNow() => TimeZoneInfo.ConvertTimeFromUtc(UtcNow(), VietnamTimeZone);

    public static DateTime ToVietnamTime(DateTime utcDateTime)
    {
        var normalizedUtc = utcDateTime.Kind == DateTimeKind.Utc
            ? utcDateTime
            : DateTime.SpecifyKind(utcDateTime, DateTimeKind.Utc);

        return TimeZoneInfo.ConvertTimeFromUtc(normalizedUtc, VietnamTimeZone);
    }

    public static string FormatVietnam(DateTime? utcDateTime, string format = "dd/MM/yyyy HH:mm:ss")
        => utcDateTime.HasValue ? FormatVietnam(utcDateTime.Value, format) : "-";

    public static string FormatVietnam(DateTime utcDateTime, string format = "dd/MM/yyyy HH:mm:ss")
        => ToVietnamTime(utcDateTime).ToString(format);

    private static TimeZoneInfo ResolveVietnamTimeZone()
    {
        try
        {
            return TimeZoneInfo.FindSystemTimeZoneById("SE Asia Standard Time");
        }
        catch (TimeZoneNotFoundException)
        {
            return TimeZoneInfo.FindSystemTimeZoneById("Asia/Ho_Chi_Minh");
        }
        catch (InvalidTimeZoneException)
        {
            return TimeZoneInfo.FindSystemTimeZoneById("Asia/Ho_Chi_Minh");
        }
    }
}
