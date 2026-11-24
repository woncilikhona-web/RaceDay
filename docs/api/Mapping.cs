using System.Globalization;

namespace RaceDay.Api.Services;

public static class Mapping
{
    public static string FormatTime(TimeSpan? t) =>
        t.HasValue ? t.Value.ToString(@"hh\:mm\:ss", CultureInfo.InvariantCulture) : null;

    public static bool TryParseTime(string s, out TimeSpan value) =>
        TimeSpan.TryParseExact(s?.Trim(), new[] { @"h\:mm\:ss", @"hh\:mm\:ss" }, CultureInfo.InvariantCulture, out value);
}
