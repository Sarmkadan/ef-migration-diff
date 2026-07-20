#nullable enable

using System.Text;
using EfMigrationDiff.Models;

namespace EfMigrationDiff.Formatters;

/// <summary>
/// Markdown formatter for generating detailed migration diff reports.
/// Produces tables for added/removed/changed migrations, conflicts section, and comprehensive summaries.
/// </summary>
public class MarkdownFormatter : IOutputFormatter
{
    private readonly bool _includeDestructiveWarnings;
    private readonly int _maxTableWidth;

    /// <summary>
    /// Initializes a new instance of the <see cref="MarkdownFormatter"/> class.
    /// </summary>
    /// <param name="includeDestructiveWarnings">Whether to include warnings about destructive changes.</param>
    /// <param name="maxTableWidth">Maximum width for table columns (0 for no limit).</param>
    public MarkdownFormatter(bool includeDestructiveWarnings = true, int maxTableWidth = 0)
    {
        _includeDestructiveWarnings = includeDestructiveWarnings;
        _maxTableWidth = maxTableWidth;
    }

    /// <summary>
    /// Formats a MigrationDiff object as a Markdown report.
    /// </summary>
    public string Format(object? obj)
    {
        if (obj is not MigrationDiff diff)
        {
            throw new FormattingException($"Expected {nameof(MigrationDiff)} but got {obj?.GetType().Name ?? "null"}");
        }

        return GenerateMarkdownReport(diff);
    }

    /// <summary>
    /// Formats a MigrationDiff object as a Markdown report.
    /// </summary>
    public string GenerateMarkdownReport(MigrationDiff diff)
    {
        var sb = new StringBuilder();

        // Header with metadata
        sb.AppendLine("# Migration Diff Report");
        sb.AppendLine();
        sb.AppendLine($"**Generated:** {DateTime.UtcNow:yyyy-MM-dd HH:mm:ss UTC}");
        sb.AppendLine($"**Source Branch:** `{diff.SourceBranchId}`");
        sb.AppendLine($"**Target Branch:** `{diff.TargetBranchId}`");
        sb.AppendLine();

        // Summary section
        sb.AppendLine("## Summary");
        sb.AppendLine();
        sb.AppendLine("| Metric | Value |")
           .AppendLine("|--------|-------|");

        sb.AppendLine($"| Result | `{diff.Result}` |")
           .AppendLine($"| Total Migrations (Source) | `{diff.OnlyInSource.Count}` |")
           .AppendLine($"| Total Migrations (Target) | `{diff.OnlyInTarget.Count}` |")
           .AppendLine($"| Common Migrations | `{diff.InBoth.Count}` |")
           .AppendLine($"| Total Schema Changes | `{diff.GetTotalSchemaChanges()}` |")
           .AppendLine($"| Conflicts | `{diff.Conflicts.Count}` |")
           .AppendLine($"| Blocking Conflicts | `{diff.GetBlockingConflicts()}` |")
           .AppendLine($"| Destructive Changes | `{diff.GetDestructiveChanges().Count}` |");

        // Add warnings if there are destructive changes
        var destructiveChanges = diff.GetDestructiveChanges();
        if (_includeDestructiveWarnings && destructiveChanges.Count > 0)
        {
            sb.AppendLine();
            sb.AppendLine("> ⚠️ **Warning:** This diff contains destructive operations that may affect data integrity.");
            sb.AppendLine("> Review these changes carefully before merging.");
        }

        sb.AppendLine();

        // Migration comparison table
        sb.AppendLine("## Migration Comparison");
        sb.AppendLine();
        sb.AppendLine("| Category | Count | Description |")
           .AppendLine("|----------|-------|-------------|");

        sb.AppendLine($"| Source Only | `{diff.OnlyInSource.Count}` | Migrations only in source branch |")
           .AppendLine($"| Target Only | `{diff.OnlyInTarget.Count}` | Migrations only in target branch |")
           .AppendLine($"| Common | `{diff.InBoth.Count}` | Migrations present in both branches |")
           .AppendLine();

        // Conflicts section
        if (diff.Conflicts.Count > 0)
        {
            sb.AppendLine("## Conflicts");
            sb.AppendLine();
            sb.AppendLine("| Severity | Type | Migration 1 | Migration 2 | Description |")
               .AppendLine("|----------|------|-------------|-------------|-------------|");

            foreach (var conflict in diff.Conflicts.OrderByDescending(c => c.Severity).ThenBy(c => c.ConflictType))
            {
                var severityEmoji = conflict.Severity switch
                {
                    ConflictSeverity.Critical => "🔴",
                    ConflictSeverity.Error => "🟠",
                    ConflictSeverity.Warning => "🟡",
                    _ => "⚪"
                };

                var type = conflict.GetTitle();
                var migration1 = TruncateMigrationId(conflict.FirstMigrationId);
                var migration2 = TruncateMigrationId(conflict.SecondMigrationId);
                var description = Truncate(conflict.Description, 50);

                sb.AppendLine($"| {severityEmoji} {conflict.Severity} | {type} | `{migration1}` | `{migration2}` | {description} |");
            }
            sb.AppendLine();
        }

        // Schema Changes by type
        var allSchemaChanges = diff.SourceSchemaChanges.Concat(diff.TargetSchemaChanges).ToList();
        if (allSchemaChanges.Count > 0)
        {
            sb.AppendLine("## Schema Changes");
            sb.AppendLine();

            // Group by change type
            var changesByType = allSchemaChanges
                .GroupBy(sc => sc.ChangeType)
                .OrderByDescending(g => g.Count())
                .ThenBy(g => g.Key.ToString());

            foreach (var group in changesByType)
            {
                var changeType = group.Key;
                var count = group.Count();
                sb.AppendLine($"### {changeType}");
                sb.AppendLine();

                // Create table based on change type
                if (changeType is SqlChangeType.AddColumn or SqlChangeType.DropColumn or SqlChangeType.ModifyColumn)
                {
                    // Column-specific table
                    sb.AppendLine("| Table | Column | Migration | SQL Preview | Destructive |")
                       .AppendLine("|-------|--------|-----------|-------------|-------------|");

                    foreach (var change in group.OrderBy(sc => sc.TableName).ThenBy(sc => sc.ColumnName))
                    {
                        var table = Truncate(change.TableName, 20);
                        var column = Truncate(change.ColumnName, 20);
                        var migration = TruncateMigrationId(change.MigrationId);
                        var sqlPreview = TruncateSql(change.Sql, 30);
                        var destructive = change.IsDestructive() ? "✅ Yes" : "❌ No";

                        sb.AppendLine($"| {table} | {column} | `{migration}` | `{sqlPreview}` | {destructive} |");
                    }
                }
                else if (changeType is SqlChangeType.CreateTable or SqlChangeType.DropTable)
                {
                    // Table-specific table
                    sb.AppendLine("| Table | Migration | SQL Preview | Destructive |")
                       .AppendLine("|-------|-----------|-------------|-------------|");

                    foreach (var change in group.OrderBy(sc => sc.TableName))
                    {
                        var table = Truncate(change.TableName, 20);
                        var migration = TruncateMigrationId(change.MigrationId);
                        var sqlPreview = TruncateSql(change.Sql, 30);
                        var destructive = change.IsDestructive() ? "✅ Yes" : "❌ No";

                        sb.AppendLine($"| {table} | `{migration}` | `{sqlPreview}` | {destructive} |");
                    }
                }
                else
                {
                    // Generic table
                    sb.AppendLine("| Object | Migration | SQL Preview | Destructive |")
                       .AppendLine("|--------|-----------|-------------|-------------|");

                    foreach (var change in group.OrderBy(sc => sc.TableName).ThenBy(sc => sc.ChangeType))
                    {
                        var objName = !string.IsNullOrEmpty(change.TableName)
                            ? Truncate(change.TableName, 20)
                            : "(unknown)";
                        var migration = TruncateMigrationId(change.MigrationId);
                        var sqlPreview = TruncateSql(change.Sql, 30);
                        var destructive = change.IsDestructive() ? "✅ Yes" : "❌ No";

                        sb.AppendLine($"| {objName} | `{migration}` | `{sqlPreview}` | {destructive} |");
                    }
                }
                sb.AppendLine();
            }
        }

        // Migration details sections
        if (diff.OnlyInSource.Count > 0)
        {
            sb.AppendLine("## Migrations Only in Source Branch");
            sb.AppendLine();
            sb.AppendLine("| Migration ID | Description |")
               .AppendLine("|-------------|-------------|");

            foreach (var migration in diff.OnlyInSource.OrderBy(m => m.Id))
            {
                var id = TruncateMigrationId(migration.Id);
                var description = Truncate(migration.Description ?? "No description", 50);
                sb.AppendLine($"| `{id}` | {description} |");
            }
            sb.AppendLine();
        }

        if (diff.OnlyInTarget.Count > 0)
        {
            sb.AppendLine("## Migrations Only in Target Branch");
            sb.AppendLine();
            sb.AppendLine("| Migration ID | Description |")
               .AppendLine("|-------------|-------------|");

            foreach (var migration in diff.OnlyInTarget.OrderBy(m => m.Id))
            {
                var id = TruncateMigrationId(migration.Id);
                var description = Truncate(migration.Description ?? "No description", 50);
                sb.AppendLine($"| `{id}` | {description} |");
            }
            sb.AppendLine();
        }

        if (diff.InBoth.Count > 0)
        {
            sb.AppendLine("## Common Migrations");
            sb.AppendLine();
            sb.AppendLine("| Migration ID | Description |")
               .AppendLine("|-------------|-------------|");

            foreach (var migration in diff.InBoth.OrderBy(m => m.Id))
            {
                var id = TruncateMigrationId(migration.Id);
                var description = Truncate(migration.Description ?? "No description", 50);
                sb.AppendLine($"| `{id}` | {description} |");
            }
            sb.AppendLine();
        }

        // Recommendations
        sb.AppendLine("## Recommendations");
        sb.AppendLine();

        var recommendations = new List<string>();

        if (diff.Result == ComparisonResult.Identical)
        {
            recommendations.Add("✅ Branches are identical. No action required.");
        }
        else if (diff.Result == ComparisonResult.Similar)
        {
            recommendations.Add("🟢 Branches are similar with minor differences. Review schema changes.");
        }
        else if (diff.HasBlockingConflicts())
        {
            recommendations.Add("🔴 **Critical:** This diff has blocking conflicts that must be resolved before merging.");
            recommendations.AddRange(diff.Conflicts
                .Where(c => c.IsBlocking())
                .Select(c => $"- Resolve conflict between `{c.FirstMigrationId}` and `{c.SecondMigrationId}` ({c.GetTitle()})"));
        }
        else if (diff.Conflicts.Count > 0)
        {
            recommendations.Add("🟡 This diff has conflicts that should be reviewed and resolved.");
        }
        else if (diff.OnlyInSource.Count > 0 && diff.OnlyInTarget.Count > 0)
        {
            recommendations.Add("🟡 Both branches have unique migrations. Consider rebase strategy.");
        }
        else if (diff.OnlyInSource.Count > 0)
        {
            recommendations.Add("➡️ Target branch should be rebased with source branch migrations.");
        }
        else if (diff.OnlyInTarget.Count > 0)
        {
            recommendations.Add("⬅️ Source branch should be rebased with target branch migrations.");
        }

        if (destructiveChanges.Count > 0)
        {
            recommendations.Add("⚠️ **Warning:** Destructive changes detected. Ensure you have backups before merging.");
        }

        if (diff.GetTotalSchemaChanges() > 10)
        {
            recommendations.Add("📊 Large number of schema changes detected. Consider breaking into smaller migrations.");
        }

        foreach (var recommendation in recommendations)
        {
            sb.AppendLine($"- {recommendation}");
        }

        sb.AppendLine();
        sb.AppendLine("---");
        sb.AppendLine($"*Report generated by EF Migration Diff Tool*");

        return sb.ToString();
    }

    /// <summary>
    /// Writes Markdown report to a file.
    /// </summary>
    public void WriteToFile(string filePath, object? obj)
    {
        try
        {
            var markdown = Format(obj);
            File.WriteAllText(filePath, markdown);
        }
        catch (Exception ex)
        {
            throw new FormattingException($"Failed to write Markdown to file: {ex.Message}", ex);
        }
    }

    /// <summary>
    /// Deserializes Markdown (not supported for Markdown format).
    /// </summary>
    public T? Deserialize<T>(string markdown)
    {
        throw new NotSupportedException("Markdown format does not support deserialization");
    }

    /// <summary>
    /// Deserializes Markdown to object (not supported for Markdown format).
    /// </summary>
    public object? Deserialize(string markdown, Type type)
    {
        throw new NotSupportedException("Markdown format does not support deserialization");
    }

    #region Helper Methods

    private string Truncate(string text, int maxLength)
    {
        if (string.IsNullOrEmpty(text))
            return "";

        if (text.Length <= maxLength)
            return text;

        return text.Substring(0, maxLength - 3) + "...";
    }

    private string TruncateMigrationId(string migrationId)
    {
        if (string.IsNullOrEmpty(migrationId))
            return "";

        // Migration IDs are typically like "20240101123456_AddUserTable"
        // Show first 15 chars + last 15 chars
        if (migrationId.Length > 30)
        {
            return migrationId[..15] + "..." + migrationId[^15..];
        }

        return migrationId;
    }

    private string TruncateSql(string sql, int maxLength)
    {
        if (string.IsNullOrEmpty(sql))
            return "";

        var cleaned = sql.Replace("\n", " ").Replace("\r", " ").Trim();

        if (cleaned.Length <= maxLength)
            return cleaned;

        return cleaned.Substring(0, maxLength - 3) + "...";
    }

    #endregion
}

/// <summary>
/// Exception for Markdown formatting operations.
/// </summary>
public class MarkdownFormattingException : FormattingException
{
    public MarkdownFormattingException(string message) : base(message) { }
    public MarkdownFormattingException(string message, Exception innerException) : base(message, innerException) { }
}