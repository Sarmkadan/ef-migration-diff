# DataTableHelper

`DataTableHelper` is a static utility class that provides formatting methods for rendering structured data, statistics, durations, file sizes, and progress indicators as human-readable strings. It is designed to support console output and markdown documentation generation within the `ef-migration-diff` tooling, converting typed collections and key-value metadata into aligned text tables or summary blocks.

## API

### FormatAsConsoleTable\<T\>

```csharp
public static string FormatAsConsoleTable<T>(IEnumerable<T> rows)
```

Formats a collection of objects as a fixed-width console table using pipe-delimited columns and a header row derived from the public properties of `T`. Column widths are determined by the longest value in each column across all rows and the header.

- **Purpose:** Generate a plain-text table suitable for console output or log files.
- **Parameters:**
  - `rows` (`IEnumerable<T>`): The collection of objects to render. Each object’s public readable properties become columns.
- **Return Value:** A `string` containing the formatted table with a header separator line.
- **Exceptions:** Throws `ArgumentNullException` if `rows` is `null`. Throws `InvalidOperationException` if `T` has no public readable properties.

### FormatAsMarkdownTable\<T\>

```csharp
public static string FormatAsMarkdownTable<T>(IEnumerable<T> rows)
```

Formats a collection of objects as a GitHub-flavored markdown table. The header row uses property names, and a separator row with alignment dashes is inserted between the header and data rows.

- **Purpose:** Produce a markdown-ready table for documentation, pull requests, or reports.
- **Parameters:**
  - `rows` (`IEnumerable<T>`): The collection of objects to render.
- **Return Value:** A `string` containing the markdown table.
- **Exceptions:** Throws `ArgumentNullException` if `rows` is `null`. Throws `InvalidOperationException` if `T` exposes no public readable properties.

### FormatKeyValueTable

```csharp
public static string FormatKeyValueTable(IReadOnlyDictionary<string, object?> dictionary)
```

Formats a dictionary of string keys and nullable object values as a two-column console table with columns “Key” and “Value”. Null values are rendered as an explicit `(null)` placeholder.

- **Purpose:** Display metadata, configuration, or diagnostic key-value pairs in a readable tabular layout.
- **Parameters:**
  - `dictionary` (`IReadOnlyDictionary<string, object?>`): The key-value pairs to format.
- **Return Value:** A `string` containing the formatted table.
- **Exceptions:** Throws `ArgumentNullException` if `dictionary` is `null`.

### FormatStatistics

```csharp
public static string FormatStatistics(IReadOnlyDictionary<string, long> counters)
```

Formats a dictionary of named counters as a two-column table with columns “Metric” and “Value”. Numeric values are rendered with thousand separators for readability.

- **Purpose:** Summarise migration diff statistics (e.g., added, removed, modified counts) in a compact table.
- **Parameters:**
  - `counters` (`IReadOnlyDictionary<string, long>`): The named numeric counters.
- **Return Value:** A `string` containing the formatted statistics table.
- **Exceptions:** Throws `ArgumentNullException` if `counters` is `null`.

### CreateProgressBar

```csharp
public static string CreateProgressBar(double percentage, int width = 20, char filledChar = '█', char emptyChar = '░')
```

Builds a text-based progress bar string of a specified width, with a percentage label appended.

- **Purpose:** Visualise progress for long-running operations in console output.
- **Parameters:**
  - `percentage` (`double`): A value between 0 and 100 inclusive. Values outside this range are clamped.
  - `width` (`int`, default `20`): The total character width of the bar (excluding the label). Must be positive; non-positive values default to 20.
  - `filledChar` (`char`, default `'█'`): Character used for the completed portion.
  - `emptyChar` (`char`, default `'░'`): Character used for the remaining portion.
- **Return Value:** A `string` in the format `[████████░░░░░░░░░░░░] 40.0%`.
- **Exceptions:** Does not throw; invalid arguments are handled with fallback defaults.

### FormatDuration

```csharp
public static string FormatDuration(TimeSpan duration)
```

Converts a `TimeSpan` into a human-friendly duration string, using the largest appropriate unit (days, hours, minutes, seconds, or milliseconds) with one decimal place.

- **Purpose:** Display elapsed time in logs or summary output.
- **Parameters:**
  - `duration` (`TimeSpan`): The time interval to format.
- **Return Value:** A `string` such as `"3.2 hours"` or `"540.1 ms"`.
- **Exceptions:** Does not throw.

### FormatFileSize

```csharp
public static string FormatFileSize(long bytes)
```

Converts a byte count into a human-readable file size string using binary units (KiB, MiB, GiB, TiB) with one decimal place. Bytes are used directly for values below 1024.

- **Purpose:** Present file sizes in logs or reports.
- **Parameters:**
  - `bytes` (`long`): The number of bytes. Negative values are treated as zero.
- **Return Value:** A `string` such as `"15.3 KiB"` or `"512 bytes"`.
- **Exceptions:** Does not throw.

## Usage

### Example 1: Migration Diff Summary Report

```csharp
using System;
using System.Collections.Generic;

// Simulated migration diff results
var stats = new Dictionary<string, long>
{
    ["Tables Added"] = 3,
    ["Tables Removed"] = 1,
    ["Columns Modified"] = 12,
    ["Stored Procedures Changed"] = 2
};

var timing = TimeSpan.FromMilliseconds(8450);
long scriptSize = 20480;

Console.WriteLine("## Migration Diff Results");
Console.WriteLine(DataTableHelper.FormatStatistics(stats));
Console.WriteLine($"Duration : {DataTableHelper.FormatDuration(timing)}");
Console.WriteLine($"Script   : {DataTableHelper.FormatFileSize(scriptSize)}");
```

### Example 2: Console Progress with Entity Listing

```csharp
using System;
using System.Collections.Generic;
using System.Threading;

// Simulated entity list
var entities = new List<EntityDiff>
{
    new() { Name = "Orders", Added = 2, Removed = 0 },
    new() { Name = "Customers", Added = 0, Removed = 1 },
    new() { Name = "Products", Added = 5, Removed = 3 }
};

Console.WriteLine(DataTableHelper.FormatAsConsoleTable(entities));

for (int i = 0; i <= 100; i += 20)
{
    Console.Write($"\r{DataTableHelper.CreateProgressBar(i)}");
    Thread.Sleep(200);
}
Console.WriteLine();

// Supporting type
public class EntityDiff
{
    public string Name { get; set; } = string.Empty;
    public int Added { get; set; }
    public int Removed { get; set; }
}
```

## Notes

- **Empty Collections:** `FormatAsConsoleTable<T>` and `FormatAsMarkdownTable<T>` return a string containing only the header and separator when the input collection is empty. No data rows are rendered.
- **Null Values in KeyValueTable:** Dictionary values that are `null` are displayed as the literal string `(null)` to distinguish them from empty strings.
- **Clamping in CreateProgressBar:** Percentages below 0 are treated as 0; percentages above 100 are treated as 100. A non-positive width argument silently falls back to the default width of 20.
- **FormatDuration Precision:** The method selects the largest unit where the value is at least 1.0. Values below 1 second are displayed in milliseconds. Rounding uses one decimal place.
- **FormatFileSize Negative Input:** Negative byte counts are coerced to zero and rendered as `"0 bytes"`.
- **Thread Safety:** All methods are static and operate exclusively on their input arguments without shared mutable state. They are safe to call concurrently from multiple threads provided the input collections are not mutated during formatting.
