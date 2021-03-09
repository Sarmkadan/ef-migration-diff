#nullable enable
using System.Reflection;
using EfMigrationDiff.Extensions;

namespace EfMigrationDiff.Formatters;

/// <summary>
/// CSV formatter for exporting data in comma-separated values format.
/// Handles complex objects through reflection and property extraction.
/// Properly escapes values containing special characters.
/// </summary>
public class CsvFormatter
{
    private readonly string _delimiter;
    private readonly string _lineSeparator;
    private readonly bool _includeHeaders;

    public CsvFormatter(string delimiter = ",", bool includeHeaders = true, string? lineSeparator = null)
    {
        _delimiter = delimiter;
        _includeHeaders = includeHeaders;
        _lineSeparator = lineSeparator ?? Environment.NewLine;
    }

    /// <summary>
    /// Formats a collection of objects as CSV.
    /// </summary>
    public string Format<T>(IEnumerable<T> items)
    {
        var itemList = items.ToList();
        if (!itemList.Any())
            return string.Empty;

        var type = typeof(T);
        var properties = type.GetPublicProperties();

        var lines = new List<string>();

        // Add headers if requested
        if (_includeHeaders)
        {
            var headers = properties.Select(p => EscapeCsvValue(p.Name));
            lines.Add(string.Join(_delimiter, headers));
        }

        // Add data rows
        foreach (var item in itemList)
        {
            var values = properties.Select(p => EscapeCsvValue(GetPropertyValueAsString(item, p)));
            lines.Add(string.Join(_delimiter, values));
        }

        return string.Join(_lineSeparator, lines);
    }

    /// <summary>
    /// Formats a single object as a CSV row.
    /// </summary>
    public string FormatRow<T>(T item)
    {
        var type = typeof(T);
        var properties = type.GetPublicProperties();
        var values = properties.Select(p => EscapeCsvValue(GetPropertyValueAsString(item, p)));
        return string.Join(_delimiter, values);
    }

    /// <summary>
    /// Writes CSV data to a file.
    /// </summary>
    public void WriteToFile<T>(string filePath, IEnumerable<T> items)
    {
        var csv = Format(items);
        File.WriteAllText(filePath, csv);
    }

    /// <summary>
    /// Escapes a CSV value by wrapping in quotes if necessary and doubling internal quotes.
    /// </summary>
    private string EscapeCsvValue(string? value)
    {
        if (string.IsNullOrEmpty(value))
            return string.Empty;

        // If value contains delimiter, quotes, or line breaks, wrap in quotes and escape quotes
        if (value.Contains(_delimiter) || value.Contains("\"") || value.Contains('\n') || value.Contains('\r'))
        {
            return $"\"{value.Replace("\"", "\"\"")}\"";
        }

        return value;
    }

    /// <summary>
    /// Gets the string representation of a property value.
    /// </summary>
    private string GetPropertyValueAsString<T>(T item, PropertyInfo property)
    {
        try
        {
            var value = property.GetValue(item);
            if (value is null)
                return string.Empty;

            // Format special types
            if (value is DateTime dt)
                return dt.ToString("O");

            if (value is bool b)
                return b ? "true" : "false";

            if (value is IEnumerable<object> enumerable && !(value is string))
                return string.Join(";", enumerable);

            return value.ToString() ?? string.Empty;
        }
        catch
        {
            return "[Error]";
        }
    }
}
