#nullable enable
namespace EfMigrationDiff.Utilities;

/// <summary>
/// Helper class for formatting and displaying data in table format.
/// Supports console tables, markdown, and text formatting.
/// </summary>
public class DataTableHelper
{
    /// <summary>
    /// Formats a collection of objects as a console table.
    /// </summary>
    public static string FormatAsConsoleTable<T>(IEnumerable<T> items, params string[] columnNames)
    {
        var itemList = items.ToList();
        if (!itemList.Any())
            return "No data to display";

        var properties = typeof(T).GetProperties(System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Instance);

        // Use provided column names or property names
        var columns = columnNames.Any() ? columnNames : properties.Select(p => p.Name).ToArray();

        // Calculate column widths
        var columnWidths = new Dictionary<string, int>();
        foreach (var col in columns)
        {
            columnWidths[col] = col.Length;
        }

        // Update widths based on data
        foreach (var item in itemList)
        {
            for (int i = 0; i < columns.Length && i < properties.Length; i++)
            {
                var value = properties[i].GetValue(item)?.ToString() ?? string.Empty;
                columnWidths[columns[i]] = Math.Max(columnWidths[columns[i]], value.Length);
            }
        }

        // Build table
        var sb = new System.Text.StringBuilder();

        // Header
        var headerRow = string.Join(" | ", columns.Select(c => c.PadRight(columnWidths[c])));
        sb.AppendLine(headerRow);
        sb.AppendLine(new string('─', headerRow.Length));

        // Rows
        foreach (var item in itemList)
        {
            var values = new List<string>();
            for (int i = 0; i < columns.Length && i < properties.Length; i++)
            {
                var value = properties[i].GetValue(item)?.ToString() ?? string.Empty;
                values.Add(value.PadRight(columnWidths[columns[i]]));
            }
            sb.AppendLine(string.Join(" | ", values));
        }

        return sb.ToString();
    }

    /// <summary>
    /// Formats a collection as a Markdown table.
    /// </summary>
    public static string FormatAsMarkdownTable<T>(IEnumerable<T> items, params string[] columnNames)
    {
        var itemList = items.ToList();
        if (!itemList.Any())
            return "No data to display\n";

        var properties = typeof(T).GetProperties(System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Instance);
        var columns = columnNames.Any() ? columnNames : properties.Select(p => p.Name).ToArray();

        var sb = new System.Text.StringBuilder();

        // Header
        sb.AppendLine("| " + string.Join(" | ", columns) + " |");
        sb.AppendLine("|" + string.Concat(columns.Select(_ => " --- |")) + "");

        // Rows
        foreach (var item in itemList)
        {
            var values = new List<string>();
            for (int i = 0; i < columns.Length && i < properties.Length; i++)
            {
                var value = properties[i].GetValue(item)?.ToString() ?? string.Empty;
                values.Add(value.Replace("|", "\\|"));
            }
            sb.AppendLine("| " + string.Join(" | ", values) + " |");
        }

        return sb.ToString();
    }

    /// <summary>
    /// Formats a key-value collection as a display table.
    /// </summary>
    public static string FormatKeyValueTable(Dictionary<string, object?> data, string keyHeader = "Key", string valueHeader = "Value")
    {
        var sb = new System.Text.StringBuilder();

        var maxKeyLength = Math.Max(keyHeader.Length, data.Keys.Max(k => k.Length));
        var maxValueLength = Math.Max(valueHeader.Length, data.Values.Max(v => (v?.ToString() ?? string.Empty).Length));

        // Header
        var headerRow = $"{keyHeader.PadRight(maxKeyLength)} : {valueHeader.PadRight(maxValueLength)}";
        sb.AppendLine(headerRow);
        sb.AppendLine(new string('─', headerRow.Length));

        // Rows
        foreach (var kvp in data)
        {
            var valueStr = kvp.Value?.ToString() ?? "[null]";
            sb.AppendLine($"{kvp.Key.PadRight(maxKeyLength)} : {valueStr.PadRight(maxValueLength)}");
        }

        return sb.ToString();
    }

    /// <summary>
    /// Creates a summary statistics display.
    /// </summary>
    public static string FormatStatistics(Dictionary<string, long> stats)
    {
        var sb = new System.Text.StringBuilder();

        sb.AppendLine("\n╔════════════════════════════════════════╗");
        sb.AppendLine("║            Statistics                  ║");
        sb.AppendLine("╚════════════════════════════════════════╝");

        foreach (var stat in stats)
        {
            var paddedKey = stat.Key.PadRight(30);
            sb.AppendLine($"  {paddedKey} : {stat.Value:N0}");
        }

        return sb.ToString();
    }

    /// <summary>
    /// Creates a progress bar display.
    /// </summary>
    public static string CreateProgressBar(int current, int total, int width = 30)
    {
        if (total == 0)
            return string.Empty;

        var percentage = (double)current / total;
        var filledWidth = (int)(width * percentage);

        var bar = new string('█', filledWidth) + new string('░', width - filledWidth);
        var percentDisplay = Math.Round(percentage * 100).ToString("N0");

        return $"[{bar}] {percentDisplay}%";
    }

    /// <summary>
    /// Formats a time span as a human-readable string.
    /// </summary>
    public static string FormatDuration(TimeSpan duration)
    {
        if (duration.TotalSeconds < 1)
            return $"{duration.TotalMilliseconds:F0}ms";

        if (duration.TotalMinutes < 1)
            return $"{duration.TotalSeconds:F1}s";

        if (duration.TotalHours < 1)
            return $"{duration.TotalMinutes:F1}m";

        return $"{duration.TotalHours:F1}h";
    }

    /// <summary>
    /// Formats bytes as a human-readable size.
    /// </summary>
    public static string FormatFileSize(long bytes)
    {
        string[] sizes = { "B", "KB", "MB", "GB" };
        double len = bytes;
        int order = 0;

        while (len >= 1024 && order < sizes.Length - 1)
        {
            order++;
            len = len / 1024;
        }

        return $"{len:F2} {sizes[order]}";
    }
}
