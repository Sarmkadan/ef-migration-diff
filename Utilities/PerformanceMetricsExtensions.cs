#nullable enable
using System.Globalization;

namespace EfMigrationDiff.Utilities;

/// <summary>
/// Formatting extension methods for the values reported by <see cref="PerformanceMetrics"/>.
/// </summary>
public static class PerformanceMetricsExtensions
{
    private const int BytesPerKilobyte = 1024;
    private const int TicksPerMicrosecond = 10;
    private const int MinutesPerHour = 60;

    private static readonly string[] ByteUnits = ["B", "KB", "MB", "GB", "TB"];

    /// <summary>
    /// Formats a duration as a short human-readable string, e.g. "850 µs", "12.34 ms", "3.50 s", "2m 05s", "1h 02m 03s".
    /// </summary>
    /// <param name="duration">The duration to format.</param>
    /// <returns>A human-readable representation of the duration.</returns>
    public static string ToHumanReadable(this TimeSpan duration)
    {
        var sign = duration < TimeSpan.Zero ? "-" : string.Empty;
        var absolute = duration.Duration();

        if (absolute.TotalMilliseconds < 1)
            return $"{sign}{absolute.Ticks / TicksPerMicrosecond} µs";

        if (absolute.TotalSeconds < 1)
            return $"{sign}{absolute.TotalMilliseconds.ToString("F2", CultureInfo.InvariantCulture)} ms";

        if (absolute.TotalMinutes < 1)
            return $"{sign}{absolute.TotalSeconds.ToString("F2", CultureInfo.InvariantCulture)} s";

        if (absolute.TotalMinutes < MinutesPerHour)
            return $"{sign}{absolute.Minutes}m {absolute.Seconds:D2}s";

        return $"{sign}{(int)absolute.TotalHours}h {absolute.Minutes:D2}m {absolute.Seconds:D2}s";
    }

    /// <summary>
    /// Formats a byte count using binary units (B, KB, MB, GB, TB), e.g. "512 B", "1.50 KB", "3.25 MB".
    /// </summary>
    /// <param name="bytes">The number of bytes to format. Negative values are formatted with a leading minus sign.</param>
    /// <returns>A human-readable representation of the byte count.</returns>
    public static string ToHumanReadableBytes(this long bytes)
    {
        var sign = bytes < 0 ? "-" : string.Empty;
        var value = Math.Abs((double)bytes);
        var unit = 0;

        while (value >= BytesPerKilobyte && unit < ByteUnits.Length - 1)
        {
            value /= BytesPerKilobyte;
            unit++;
        }

        if (unit == 0)
            return $"{sign}{(long)value} {ByteUnits[unit]}";

        return $"{sign}{value.ToString("F2", CultureInfo.InvariantCulture)} {ByteUnits[unit]}";
    }
}
