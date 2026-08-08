#nullable enable
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
        ArgumentNullException.ThrowIfNull(diff);
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
    /// Represents a breaking change in the migration comparison output.
    /// </summary>
    /// <param name="Id">Unique identifier for the breaking change.</param>
    /// <param name="Type">Human-readable type of breaking change.</param>
    /// <param name="Severity">Severity level of the breaking change.</param>
    /// <param name="Description">Description of the breaking change.</param>
    /// <param name="FirstMigrationId">First migration involved.</param>
    /// <param name="SecondMigrationId">Second migration involved.</param>
    /// <param name="AffectedElements">List of affected schema elements.</param>
    /// <param name="Details">Additional details about the breaking change.</param>
    /// <param name="DetectedAt">When the breaking change was detected.</param>
    private sealed record BreakingChangeJson(
        string Id,
        string Type,
        string Severity,
        string Description,
        string FirstMigrationId,
        string SecondMigrationId,
        List<string> AffectedElements,
        Dictionary<string, string> Details,
        DateTime DetectedAt);

    /// <summary>
    /// Represents a conflict in the migration comparison output.
    /// </summary>
    /// <param name="Id">Unique identifier for the conflict.</param>
    /// <param name="Type">Type of conflict.</param>
    /// <param name="Severity">Severity level.</param>
    /// <param name="Description">Description of the conflict.</param>
    /// <param name="FirstMigrationId">First migration involved.</param>
    /// <param name="SecondMigrationId">Second migration involved.</param>
    /// <param name="AffectedElements">List of affected schema elements.</param>
    /// <param name="Details">Additional details about the conflict.</param>
    /// <param name="IsBlocking">Whether this conflict blocks deployment.</param>
    /// <param name="IsResolved">Whether this conflict has been resolved.</param>
    /// <param name="ResolutionStrategy">How the conflict was resolved (if applicable).</param>
    /// <param name="DetectedAt">When the conflict was detected.</param>
    private sealed record ConflictJson(
        string Id,
        string Type,
        string Severity,
        string Description,
        string FirstMigrationId,
        string SecondMigrationId,
        List<string> AffectedElements,
        Dictionary<string, string> Details,
        bool IsBlocking,
        bool IsResolved,
        string? ResolutionStrategy,
        DateTime DetectedAt);

    /// <summary>
    /// Represents a schema change in the migration comparison output.
    /// </summary>
    /// <param name="Id">Unique identifier for the schema change.</param>
    /// <param name="MigrationId">Migration that contains this change.</param>
    /// <param name="Type">Type of schema change.</param>
    /// <param name="TableName">Table affected by the change.</param>
    /// <param name="ColumnName">Column affected by the change (if applicable).</param>
    /// <param name="Sql">The SQL statement.</param>
    /// <param name="LineNumber">Line number in migration file.</param>
    /// <param name="IsDestructive">Whether this is a destructive operation.</param>
    /// <param name="Metadata">Additional metadata about the change.</param>
    private sealed record SchemaChangeJson(
        string Id,
        string MigrationId,
        string Type,
        string TableName,
        string? ColumnName,
        string Sql,
        int LineNumber,
        bool IsDestructive,
        Dictionary<string, object?> Metadata);

    /// <summary>
    /// Represents a migration summary in the output.
    /// </summary>
    /// <param name="Id">Migration identifier.</param>
    /// <param name="Name">Human-readable name.</param>
    /// <param name="DbContextName">DbContext this migration belongs to.</param>
    /// <param name="Sequence">Sequence number.</param>
    private sealed record MigrationSummaryJson(
        string Id,
        string Name,
        string DbContextName,
        int Sequence);

    /// <summary>
    /// Represents a cycle/path in the migration dependency graph.
    /// </summary>
    /// <param name="Migrations">List of migrations in the cycle.</param>
    /// <param name="Severity">Severity level.</param>
    /// <param name="Description">Description of the cycle.</param>
    private sealed record CycleJson(
        string[] Migrations,
        string Severity,
        string Description);

    /// <summary>
    /// Summary statistics for the migration comparison.
    /// </summary>
    /// <param name="Result">Comparison result.</param>
    /// <param name="TotalMigrations">Total number of migrations.</param>
    /// <param name="SourceOnlyCount">Migrations only in source.</param>
    /// <param name="TargetOnlyCount">Migrations only in target.</param>
    /// <param name="CommonCount">Migrations in both branches.</param>
    /// <param name="TotalSchemaChanges">Total schema changes.</param>
    /// <param name="SourceSchemaChanges">Schema changes in source.</param>
    /// <param name="TargetSchemaChanges">Schema changes in target.</param>
    /// <param name="TotalConflicts">Total conflicts detected.</param>
    /// <param name="BlockingConflicts">Conflicts that block deployment.</param>
    /// <param name="HasBlockingConflicts">Whether blocking conflicts exist.</param>
    /// <param name="DestructiveChanges">Number of destructive changes.</param>
    /// <param name="CanDeploy">Whether deployment is possible.</param>
    private sealed record ComparisonSummaryJson(
        string Result,
        int TotalMigrations,
        int SourceOnlyCount,
        int TargetOnlyCount,
        int CommonCount,
        int TotalSchemaChanges,
        int SourceSchemaChanges,
        int TargetSchemaChanges,
        int TotalConflicts,
        int BlockingConflicts,
        bool HasBlockingConflicts,
        int DestructiveChanges,
        bool CanDeploy);

    /// <summary>
    /// Schema changes grouped by source/target.
    /// </summary>
    /// <param name="Source">Changes in source branch.</param>
    /// <param name="Target">Changes in target branch.</param>
    private sealed record SchemaChangesJson(
        List<SchemaChangeJson> Source,
        List<SchemaChangeJson> Target);

    /// <summary>
    /// Migrations grouped by their presence in branches.
    /// </summary>
    /// <param name="SourceOnly">Migrations only in source.</param>
    /// <param name="TargetOnly">Migrations only in target.</param>
    /// <param name="Common">Migrations in both branches.</param>
    private sealed record MigrationsJson(
        List<MigrationSummaryJson> SourceOnly,
        List<MigrationSummaryJson> TargetOnly,
        List<MigrationSummaryJson> Common);

    /// <summary>
    /// The complete migration comparison report with versioned schema.
    /// </summary>
    /// <param name="SchemaVersion">Version of the output schema.</param>
    /// <param name="GeneratedAt">When the report was generated.</param>
    /// <param name="SourceBranch">Source branch name.</param>
    /// <param name="TargetBranch">Target branch name.</param>
    /// <param name="Summary">Summary statistics.</param>
    /// <param name="BreakingChanges">List of breaking changes.</param>
    /// <param name="Conflicts">List of all conflicts.</param>
    /// <param name="SchemaChanges">Schema changes by branch.</param>
    /// <param name="Migrations">Migrations by presence.</param>
    /// <param name="Cycles">Cycle information.</param>
    private sealed record MigrationComparisonReportJson(
        string SchemaVersion,
        DateTime GeneratedAt,
        string SourceBranch,
        string TargetBranch,
        ComparisonSummaryJson Summary,
        List<BreakingChangeJson> BreakingChanges,
        List<ConflictJson> Conflicts,
        SchemaChangesJson SchemaChanges,
        MigrationsJson Migrations,
        List<CycleJson> Cycles);

    /// <summary>
    /// Generates a JSON report of a migration diff with a stable, versioned schema.
    /// Output is pipeable and suitable for CI/CD pipelines.
    /// </summary>
    /// <param name="diff">The migration diff to serialize.</param>
    /// <returns>A JSON string with stable ordering and versioned schema.</returns>
    public string GenerateJsonReport(MigrationDiff diff)
    {
        ArgumentNullException.ThrowIfNull(diff);

        diff.GenerateSummary();

        // Build breaking changes from conflicts with Critical or Error severity
        var breakingChanges = diff.Conflicts
            .Where(c => c.IsBlocking())
            .Select(c => new BreakingChangeJson(
                c.Id,
                c.GetTitle(),
                c.Severity.ToString(),
                c.Description,
                c.FirstMigrationId,
                c.SecondMigrationId,
                c.AffectedElements.OrderBy(e => e, StringComparer.Ordinal).ToList(),
                c.Details.OrderBy(kv => kv.Key, StringComparer.Ordinal).ToDictionary(kv => kv.Key, kv => kv.Value),
                c.DetectedAt
            ))
            .OrderBy(bc => bc.Severity, StringComparer.Ordinal)
            .ThenBy(bc => bc.Description, StringComparer.Ordinal)
            .ToList();

        // Build conflicts array with stable ordering
        var conflicts = diff.Conflicts
            .Select(c => new ConflictJson(
                c.Id,
                c.ConflictType.ToString(),
                c.Severity.ToString(),
                c.Description,
                c.FirstMigrationId,
                c.SecondMigrationId,
                c.AffectedElements.OrderBy(e => e, StringComparer.Ordinal).ToList(),
                c.Details.OrderBy(kv => kv.Key, StringComparer.Ordinal).ToDictionary(kv => kv.Key, kv => kv.Value),
                c.IsBlocking(),
                c.IsResolved,
                c.IsResolved ? c.ResolutionStrategy : null,
                c.DetectedAt
            ))
            .OrderByDescending(c => c.IsBlocking)
            .ThenBy(c => c.Severity, StringComparer.Ordinal)
            .ThenBy(c => c.Description, StringComparer.Ordinal)
            .ToList();

        // Build schema changes with stable ordering
        var schemaChanges = new SchemaChangesJson(
            Source: diff.SourceSchemaChanges
                .Select(sc => new SchemaChangeJson(
                    sc.Id,
                    sc.MigrationId,
                    sc.ChangeType.ToString(),
                    sc.TableName,
                    sc.ColumnName,
                    sc.Sql,
                    sc.LineNumber,
                    sc.IsDestructive(),
                    sc.Metadata.OrderBy(kv => kv.Key, StringComparer.Ordinal).ToDictionary(kv => kv.Key, kv => (object?)kv.Value)
                ))
                .OrderBy(sc => sc.MigrationId, StringComparer.Ordinal)
                .ThenBy(sc => sc.LineNumber)
                .ToList(),
            Target: diff.TargetSchemaChanges
                .Select(sc => new SchemaChangeJson(
                    sc.Id,
                    sc.MigrationId,
                    sc.ChangeType.ToString(),
                    sc.TableName,
                    sc.ColumnName,
                    sc.Sql,
                    sc.LineNumber,
                    sc.IsDestructive(),
                    sc.Metadata.OrderBy(kv => kv.Key, StringComparer.Ordinal).ToDictionary(kv => kv.Key, kv => (object?)kv.Value)
                ))
                .OrderBy(sc => sc.MigrationId, StringComparer.Ordinal)
                .ThenBy(sc => sc.LineNumber)
                .ToList()
        );

        // Build migrations with stable ordering
        var migrations = new MigrationsJson(
            SourceOnly: diff.OnlyInSource
                .Select(m => new MigrationSummaryJson(
                    m.Id,
                    m.Name,
                    m.DbContextName,
                    m.Sequence
                ))
                .OrderBy(m => m.Sequence)
                .ThenBy(m => m.Name, StringComparer.Ordinal)
                .ToList(),
            TargetOnly: diff.OnlyInTarget
                .Select(m => new MigrationSummaryJson(
                    m.Id,
                    m.Name,
                    m.DbContextName,
                    m.Sequence
                ))
                .OrderBy(m => m.Sequence)
                .ThenBy(m => m.Name, StringComparer.Ordinal)
                .ToList(),
            Common: diff.InBoth
                .Select(m => new MigrationSummaryJson(
                    m.Id,
                    m.Name,
                    m.DbContextName,
                    m.Sequence
                ))
                .OrderBy(m => m.Sequence)
                .ThenBy(m => m.Name, StringComparer.Ordinal)
                .ToList()
        );

        // Build cycle information from blocking conflicts
        var cycles = diff.Conflicts
            .Where(c => c.IsBlocking())
            .Select(c => new CycleJson(
                Migrations: new[] { c.FirstMigrationId, c.SecondMigrationId },
                Severity: c.Severity.ToString(),
                Description: c.Description
            ))
            .OrderBy(c => c.Severity, StringComparer.Ordinal)
            .ThenBy(c => string.Join("-", c.Migrations), StringComparer.Ordinal)
            .ToList();

        // Build the final report with versioned schema
        var report = new MigrationComparisonReportJson(
            SchemaVersion: "1.0",
            GeneratedAt: DateTime.UtcNow,
            SourceBranch: diff.SourceBranchId,
            TargetBranch: diff.TargetBranchId,
            Summary: new ComparisonSummaryJson(
                Result: diff.Result.ToString(),
                TotalMigrations: diff.InBoth.Count + diff.OnlyInSource.Count + diff.OnlyInTarget.Count,
                SourceOnlyCount: diff.OnlyInSource.Count,
                TargetOnlyCount: diff.OnlyInTarget.Count,
                CommonCount: diff.InBoth.Count,
                TotalSchemaChanges: diff.GetTotalSchemaChanges(),
                SourceSchemaChanges: diff.SourceSchemaChanges.Count,
                TargetSchemaChanges: diff.TargetSchemaChanges.Count,
                TotalConflicts: diff.Conflicts.Count,
                BlockingConflicts: diff.GetBlockingConflicts(),
                HasBlockingConflicts: diff.HasBlockingConflicts(),
                DestructiveChanges: diff.GetDestructiveChanges().Count,
                CanDeploy: !diff.HasBlockingConflicts()
            ),
            BreakingChanges: breakingChanges,
            Conflicts: conflicts,
            SchemaChanges: schemaChanges,
            Migrations: migrations,
            Cycles: cycles
        );

        return JsonSerializer.Serialize(report, new JsonSerializerOptions
        {
            WriteIndented = true,
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
            Converters = { new System.Text.Json.Serialization.JsonStringEnumConverter(JsonNamingPolicy.CamelCase) }
        });
    }

    /// <summary>
    /// Generates an HTML report for browser viewing.
    /// </summary>
    public string GenerateHtmlReport(MigrationDiff diff)
    {
        ArgumentNullException.ThrowIfNull(diff);
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
        ArgumentNullException.ThrowIfNull(diff);
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
