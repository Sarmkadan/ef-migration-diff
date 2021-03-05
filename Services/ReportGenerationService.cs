#nullable enable
// =============================================================================
// Author: Vladyslav Zaiets | https://sarmkadan.com
// CTO & Software Architect
// =============================================================================

using EfMigrationDiff.Models;
using System.Text;
using System.Text.Json;

namespace EfMigrationDiff.Services;

/// <summary>
/// Service for generating various reports from migration diffs.
/// </summary>
public class ReportGenerationService
{
    /// <summary>
    /// Generates a detailed text report of a migration diff.
    /// </summary>
    public string GenerateTextReport(MigrationDiff diff)
    {
        var sb = new StringBuilder();

        sb.AppendLine("╔══════════════════════════════════════════════════════════════╗");
        sb.AppendLine("║          EF Migration Diff Report                           ║");
        sb.AppendLine("╚══════════════════════════════════════════════════════════════╝");
        sb.AppendLine();

        sb.AppendLine($"Comparison Result: {diff.Result}");
        sb.AppendLine($"Generated: {DateTime.UtcNow:yyyy-MM-dd HH:mm:ss UTC}");
        sb.AppendLine();

        AppendMigrationSummary(sb, diff);
        AppendSchemaChangesSummary(sb, diff);
        AppendConflictsSummary(sb, diff);
        AppendDestructiveChangesSummary(sb, diff);

        return sb.ToString();
    }

    /// <summary>
    /// Generates a JSON report of a migration diff.
    /// </summary>
    public string GenerateJsonReport(MigrationDiff diff)
    {
        diff.GenerateSummary();

        var reportData = new
        {
            diff.Result,
            GeneratedAt = DateTime.UtcNow,
            diff.Summary,
            Migrations = new
            {
                SourceOnly = diff.OnlyInSource.Select(m => new { m.Id, m.Name }),
                TargetOnly = diff.OnlyInTarget.Select(m => new { m.Id, m.Name }),
                Common = diff.InBoth.Select(m => new { m.Id, m.Name })
            },
            Conflicts = diff.Conflicts.Select(c => new
            {
                c.Id,
                c.ConflictType,
                c.Severity,
                c.Description,
                AffectedElements = c.AffectedElements
            }),
            SchemaChanges = new
            {
                Source = diff.SourceSchemaChanges.Select(sc => new
                {
                    sc.ChangeType,
                    sc.TableName,
                    sc.ColumnName,
                    sc.LineNumber
                }),
                Target = diff.TargetSchemaChanges.Select(sc => new
                {
                    sc.ChangeType,
                    sc.TableName,
                    sc.ColumnName,
                    sc.LineNumber
                })
            }
        };

        return JsonSerializer.Serialize(reportData, new JsonSerializerOptions
        {
            WriteIndented = true,
            Converters = { new System.Text.Json.Serialization.JsonStringEnumConverter() }
        });
    }

    /// <summary>
    /// Generates an HTML report for browser viewing.
    /// </summary>
    public string GenerateHtmlReport(MigrationDiff diff)
    {
        var sb = new StringBuilder();

        sb.AppendLine("<!DOCTYPE html>");
        sb.AppendLine("<html>");
        sb.AppendLine("<head>");
        sb.AppendLine("    <meta charset=\"utf-8\" />");
        sb.AppendLine("    <title>EF Migration Diff Report</title>");
        sb.AppendLine("    <style>");
        sb.AppendLine("        body { font-family: Arial, sans-serif; margin: 20px; background: #f5f5f5; }");
        sb.AppendLine("        .container { max-width: 1200px; margin: 0 auto; background: white; padding: 20px; border-radius: 8px; }");
        sb.AppendLine("        h1 { color: #333; border-bottom: 2px solid #007bff; padding-bottom: 10px; }");
        sb.AppendLine("        .result { font-size: 18px; margin: 20px 0; padding: 15px; border-radius: 4px; }");
        sb.AppendLine("        .identical { background: #d4edda; color: #155724; }");
        sb.AppendLine("        .different { background: #fff3cd; color: #856404; }");
        sb.AppendLine("        .conflicting { background: #f8d7da; color: #721c24; }");
        sb.AppendLine("        table { width: 100%; border-collapse: collapse; margin: 20px 0; }");
        sb.AppendLine("        th, td { padding: 12px; text-align: left; border-bottom: 1px solid #ddd; }");
        sb.AppendLine("        th { background: #f8f9fa; font-weight: bold; }");
        sb.AppendLine("        tr:hover { background: #f9f9f9; }");
        sb.AppendLine("        .severity-critical { color: red; font-weight: bold; }");
        sb.AppendLine("        .severity-error { color: darkorange; }");
        sb.AppendLine("        .severity-warning { color: orange; }");
        sb.AppendLine("        .section { margin: 30px 0; }");
        sb.AppendLine("    </style>");
        sb.AppendLine("</head>");
        sb.AppendLine("<body>");
        sb.AppendLine("    <div class=\"container\">");
        sb.AppendLine("        <h1>Entity Framework Migration Diff Report</h1>");

        var resultClass = diff.Result switch
        {
            ComparisonResult.Identical => "identical",
            ComparisonResult.Conflicting => "conflicting",
            _ => "different"
        };

        sb.AppendLine($"        <div class=\"result {resultClass}\">");
        sb.AppendLine($"            <strong>Result:</strong> {diff.Result} - {diff.GetResultDescription()}");
        sb.AppendLine($"            <br/><small>Generated: {DateTime.UtcNow:yyyy-MM-dd HH:mm:ss UTC}</small>");
        sb.AppendLine("        </div>");

        AppendHtmlMigrationTable(sb, diff);
        AppendHtmlConflictsTable(sb, diff);
        AppendHtmlSchemaChangesTable(sb, diff);

        sb.AppendLine("    </div>");
        sb.AppendLine("</body>");
        sb.AppendLine("</html>");

        return sb.ToString();
    }

    /// <summary>
    /// Generates a summary for conflict review.
    /// </summary>
    public string GenerateConflictSummary(MigrationDiff diff)
    {
        var sb = new StringBuilder();

        sb.AppendLine("CONFLICT ANALYSIS");
        sb.AppendLine("=================");
        sb.AppendLine();

        if (!diff.HasConflicts())
        {
            sb.AppendLine("No conflicts detected.");
            return sb.ToString();
        }

        sb.AppendLine($"Total Conflicts: {diff.Conflicts.Count}");
        sb.AppendLine($"Blocking Conflicts: {diff.GetBlockingConflicts()}");
        sb.AppendLine($"Can Deploy: {(diff.HasBlockingConflicts() ? "NO" : "YES")}");
        sb.AppendLine();

        var conflictsByType = diff.Conflicts.GroupBy(c => c.ConflictType);

        foreach (var group in conflictsByType)
        {
            sb.AppendLine($"[{group.Key}] - {group.Count()} conflict(s):");

            foreach (var conflict in group)
            {
                sb.AppendLine($"  • [{conflict.Severity}] {conflict.Description}");
                if (conflict.AffectedElements.Count > 0)
                {
                    sb.AppendLine($"    Affected: {string.Join(", ", conflict.AffectedElements)}");
                }
            }
            sb.AppendLine();
        }

        return sb.ToString();
    }

    private void AppendMigrationSummary(StringBuilder sb, MigrationDiff diff)
    {
        sb.AppendLine("MIGRATION SUMMARY");
        sb.AppendLine("─────────────────");
        sb.AppendLine($"Source Only: {diff.OnlyInSource.Count} migration(s)");

        foreach (var migration in diff.OnlyInSource)
        {
            sb.AppendLine($"  • {migration.Name}");
        }

        sb.AppendLine($"Target Only: {diff.OnlyInTarget.Count} migration(s)");

        foreach (var migration in diff.OnlyInTarget)
        {
            sb.AppendLine($"  • {migration.Name}");
        }

        sb.AppendLine($"Common: {diff.InBoth.Count} migration(s)");

        foreach (var migration in diff.InBoth)
        {
            sb.AppendLine($"  • {migration.Name}");
        }

        sb.AppendLine();
    }

    private void AppendSchemaChangesSummary(StringBuilder sb, MigrationDiff diff)
    {
        sb.AppendLine("SCHEMA CHANGES");
        sb.AppendLine("──────────────");
        sb.AppendLine($"Total Schema Changes: {diff.GetTotalSchemaChanges()}");
        sb.AppendLine($"  Source: {diff.SourceSchemaChanges.Count} change(s)");

        foreach (var change in diff.SourceSchemaChanges)
        {
            sb.AppendLine($"    • {change.ChangeType}: {change.GetDescription()}");
        }

        sb.AppendLine($"  Target: {diff.TargetSchemaChanges.Count} change(s)");

        foreach (var change in diff.TargetSchemaChanges)
        {
            sb.AppendLine($"    • {change.ChangeType}: {change.GetDescription()}");
        }

        sb.AppendLine();
    }

    private void AppendConflictsSummary(StringBuilder sb, MigrationDiff diff)
    {
        if (diff.HasConflicts())
        {
            sb.AppendLine("CONFLICTS DETECTED");
            sb.AppendLine("──────────────────");
            sb.AppendLine($"Total: {diff.Conflicts.Count}");
            sb.AppendLine($"Blocking: {diff.GetBlockingConflicts()}");

            foreach (var conflict in diff.Conflicts.Take(5))
            {
                sb.AppendLine($"  • [{conflict.Severity}] {conflict.Description}");
            }

            if (diff.Conflicts.Count > 5)
            {
                sb.AppendLine($"  ... and {diff.Conflicts.Count - 5} more");
            }

            sb.AppendLine();
        }
    }

    private void AppendDestructiveChangesSummary(StringBuilder sb, MigrationDiff diff)
    {
        var destructive = diff.GetDestructiveChanges();

        if (destructive.Count > 0)
        {
            sb.AppendLine("⚠️  DESTRUCTIVE OPERATIONS DETECTED");
            sb.AppendLine("───────────────────────────────────");
            sb.AppendLine($"Total Destructive Changes: {destructive.Count}");

            foreach (var change in destructive.Take(5))
            {
                sb.AppendLine($"  • {change.GetDescription()}");
            }

            if (destructive.Count > 5)
            {
                sb.AppendLine($"  ... and {destructive.Count - 5} more");
            }

            sb.AppendLine();
        }
    }

    private void AppendHtmlMigrationTable(StringBuilder sb, MigrationDiff diff)
    {
        sb.AppendLine("    <div class=\"section\">");
        sb.AppendLine("        <h2>Migrations</h2>");
        sb.AppendLine("        <table>");
        sb.AppendLine("            <tr><th>Category</th><th>Count</th><th>Details</th></tr>");
        sb.AppendLine($"            <tr><td>Source Only</td><td>{diff.OnlyInSource.Count}</td><td>{string.Join(", ", diff.OnlyInSource.Select(m => m.Name))}</td></tr>");
        sb.AppendLine($"            <tr><td>Target Only</td><td>{diff.OnlyInTarget.Count}</td><td>{string.Join(", ", diff.OnlyInTarget.Select(m => m.Name))}</td></tr>");
        sb.AppendLine($"            <tr><td>Common</td><td>{diff.InBoth.Count}</td><td>{string.Join(", ", diff.InBoth.Select(m => m.Name))}</td></tr>");
        sb.AppendLine("        </table>");
        sb.AppendLine("    </div>");
    }

    private void AppendHtmlConflictsTable(StringBuilder sb, MigrationDiff diff)
    {
        if (!diff.HasConflicts())
            return;

        sb.AppendLine("    <div class=\"section\">");
        sb.AppendLine("        <h2>Conflicts</h2>");
        sb.AppendLine("        <table>");
        sb.AppendLine("            <tr><th>Type</th><th>Severity</th><th>Description</th></tr>");

        foreach (var conflict in diff.Conflicts)
        {
            var severityClass = $"severity-{conflict.Severity.ToString().ToLowerInvariant()}";
            sb.AppendLine($"            <tr><td>{conflict.ConflictType}</td><td class=\"{severityClass}\">{conflict.Severity}</td><td>{conflict.Description}</td></tr>");
        }

        sb.AppendLine("        </table>");
        sb.AppendLine("    </div>");
    }

    private void AppendHtmlSchemaChangesTable(StringBuilder sb, MigrationDiff diff)
    {
        if (diff.GetTotalSchemaChanges() == 0)
            return;

        sb.AppendLine("    <div class=\"section\">");
        sb.AppendLine("        <h2>Schema Changes</h2>");
        sb.AppendLine("        <table>");
        sb.AppendLine("            <tr><th>Type</th><th>Table</th><th>Column</th><th>Source</th></tr>");

        foreach (var change in diff.SourceSchemaChanges.Take(10))
        {
            sb.AppendLine($"            <tr><td>{change.ChangeType}</td><td>{change.TableName}</td><td>{change.ColumnName}</td><td>Source</td></tr>");
        }

        foreach (var change in diff.TargetSchemaChanges.Take(10))
        {
            sb.AppendLine($"            <tr><td>{change.ChangeType}</td><td>{change.TableName}</td><td>{change.ColumnName}</td><td>Target</td></tr>");
        }

        sb.AppendLine("        </table>");
        sb.AppendLine("    </div>");
    }
}
