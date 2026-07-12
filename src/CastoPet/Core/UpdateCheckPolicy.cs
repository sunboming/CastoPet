using System.Globalization;

namespace CastoPet.Core;

public static class UpdateCheckPolicy
{
    private const string DateFormat = "yyyy-MM-dd";

    public static bool ShouldCheckAutomatically(string? lastCheckDate, DateOnly today)
    {
        return !DateOnly.TryParseExact(
                lastCheckDate,
                DateFormat,
                CultureInfo.InvariantCulture,
                DateTimeStyles.None,
                out var parsed)
            || parsed != today;
    }

    public static bool ShouldCheck(bool manual, string? lastCheckDate, DateOnly today)
    {
        return manual || ShouldCheckAutomatically(lastCheckDate, today);
    }

    public static string FormatDate(DateOnly date)
    {
        return date.ToString(DateFormat, CultureInfo.InvariantCulture);
    }
}
