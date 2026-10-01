#nullable enable

using System.Text;
using EfMigrationDiff.Models;
using Microsoft.Extensions.Logging;

namespace EfMigrationDiff.Formatters;

/// <summary>
/// Formats migration diff results and schema changes as human-readable
/// plain-text script output with configurable verbosity.
/// </summary>
public class ScriptFormatter : IDisposable
{
    private readonly ILogger<ScriptFormatter> _logger;
    private readonly StringBuilder _sb;
    private bool _disposed;

    public ScriptFormatter(ILogger<ScriptFormatter> logger)
    {
        ArgumentNullException.ThrowIfNull(logger);
        _logger = logger;
        _sb = new StringBuilder(4096);
    }

    /// <summary>
    /// Formats a list of schema changes as a structured text report.
    /// </summary>
    /// <param name="changes">The schema changes to format.</param>
    /// <param name="title">Optional report title.</param>
    /// <returns>A formatted plain-text report string.</returns>
    public string FormatChanges(IReadOnlyList<SchemaChange> changes, string? title = null)
    {
        ArgumentNullException.ThrowIfNull(changes);

        _sb.Clear();
        _sb.AppendLine(new string('=', 60));
        _sb.AppendLine(title ?? "Schema Changes Report");
        _sb.AppendLine(new string('=', 60));
        _sb.AppendLine($"Generated: {DateTime.UtcNow:yyyy-MM-dd HH:mm:ss} UTC");
        _sb.AppendLine($"Total changes: {changes.Count}");
        _sb.AppendLine();

        var grouped = changes.GroupBy(c => c.TableName);
        foreach (var group in grouped)
        {
            _sb.AppendLine($"  Table: {group.Key}");
            _sb.AppendLine($"  {new string('-', 40)}");

            foreach (var change in group)
            {
                _sb.AppendLine($"    [{change.ChangeType}] {FormatChangeDetail(change)}");
            }

            _sb.AppendLine();
        }

        _logger.LogInformation("Formatted {Count} changes into text report", changes.Count);
        return _sb.ToString();
    }

    /// <summary>
    /// Formats a migration diff result as a side-by-side comparison in plain text.
    /// </summary>
    /// <param name="diff">The migration diff to format.</param>
    /// <returns>A plain-text side-by-side comparison.</returns>
    public string FormatDiff(MigrationDiff diff)
    {
        ArgumentNullException.ThrowIfNull(diff);

        _sb.Clear();
        _sb.AppendLine($"Migration Diff: {diff.SourceBranchId} -> {diff.TargetBranchId}");
        _sb.AppendLine(new string('=', 60));

        if (diff.OnlyInSource.Count > 0)
        {
            _sb.AppendLine($"\n  Source-only ({diff.OnlyInSource.Count}):");
            foreach (var m in diff.OnlyInSource)
                _sb.AppendLine($"    - {m.Name}");
        }

        if (diff.OnlyInTarget.Count > 0)
        {
            _sb.AppendLine($"\n  Target-only ({diff.OnlyInTarget.Count}):");
            foreach (var m in diff.OnlyInTarget)
                _sb.AppendLine($"    + {m.Name}");
        }

        if (diff.InBoth.Count > 0)
        {
            _sb.AppendLine($"\n  Shared ({diff.InBoth.Count}):");
            foreach (var m in diff.InBoth)
                _sb.AppendLine($"    = {m.Name}");
        }

        return _sb.ToString();
    }

    /// <summary>
    /// Formats a conflict summary for display to the user.
    /// </summary>
    /// <param name="conflicts">The conflicts to summarize.</param>
    /// <returns>A plain-text conflict summary.</returns>
    public string FormatConflicts(IReadOnlyList<ConflictInfo> conflicts)
    {
        ArgumentNullException.ThrowIfNull(conflicts);

        _sb.Clear();
        _sb.AppendLine("Conflict Summary");
        _sb.AppendLine(new string('=', 60));
        _sb.AppendLine($"Total conflicts: {conflicts.Count}");

        var bySeverity = conflicts.GroupBy(c => c.Severity).OrderByDescending(g => g.Key);
        foreach (var group in bySeverity)
        {
            _sb.AppendLine($"\n  [{group.Key}] ({group.Count()}):");
            foreach (var conflict in group)
            {
                _sb.AppendLine($"    - {conflict.Description}");
                _sb.AppendLine($"      Migrations: {conflict.FirstMigrationId} <-> {conflict.SecondMigrationId}");
            }
        }

        return _sb.ToString();
    }

    /// <summary>
    /// Produces a compact one-line summary of a schema change for log output.
    /// </summary>
    /// <param name="change">The change to summarize.</param>
    /// <returns>A single-line summary string.</returns>
    public string FormatOneLiner(SchemaChange change)
    {
        ArgumentNullException.ThrowIfNull(change);
        return $"[{change.ChangeType}] {change.TableName}.{change.ColumnName}: {change.OldValue ?? "(null)"} -> {change.NewValue ?? "(null)"}";
    }

    private static string FormatChangeDetail(SchemaChange change)
    {
        if (!string.IsNullOrEmpty(change.ColumnName))
            return $"{change.ColumnName}: {change.OldValue ?? "(new)"} -> {change.NewValue ?? "(dropped)"}";

        return !string.IsNullOrEmpty(change.Sql)
            ? change.Sql.Length > 80 ? change.Sql[..77] + "..." : change.Sql
            : change.ChangeType.ToString();
    }

    public void Dispose()
    {
        if (!_disposed)
        {
            _sb.Clear();
            _disposed = true;
        }
        GC.SuppressFinalize(this);
    }
}
