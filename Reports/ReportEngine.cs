// =============================================================================
// Author: Vladyslav Zaiets | https://sarmkadan.com
// CTO & Software Architect
// =============================================================================

using EfMigrationDiff.Models;
using EfMigrationDiff.Formatters;

namespace EfMigrationDiff.Reports;

/// <summary>
/// Advanced report generation engine supporting multiple formats and templates.
/// Generates comprehensive migration analysis reports with charts, summaries, and recommendations.
/// </summary>
public class ReportEngine
{
    private readonly JsonFormatter _jsonFormatter;
    private readonly CsvFormatter _csvFormatter;
    private readonly HtmlFormatter _htmlFormatter;
    private readonly Dictionary<string, IReportTemplate> _templates = new();

    public ReportEngine()
    {
        _jsonFormatter = new JsonFormatter(prettyPrint: true);
        _csvFormatter = new CsvFormatter();
        _htmlFormatter = new HtmlFormatter();
    }

    /// <summary>
    /// Generates a report in the specified format.
    /// </summary>
    public string GenerateReport(MigrationDiff diff, string format = "html")
    {
        return format.ToLowerInvariant() switch
        {
            "json" => GenerateJsonReport(diff),
            "csv" => GenerateCsvReport(diff),
            "text" => GenerateTextReport(diff),
            "html" => GenerateHtmlReport(diff),
            _ => GenerateTextReport(diff)
        };
    }

    /// <summary>
    /// Generates a JSON report with complete migration data.
    /// </summary>
    public string GenerateJsonReport(MigrationDiff diff)
    {
        var report = new
        {
            timestamp = DateTime.UtcNow,
            summary = new
            {
                result = diff.Result.ToString(),
                conflictCount = diff.Conflicts.Count,
                schemaChanges = diff.GetTotalSchemaChanges(),
                migrationCount = diff.SourceMigrations.Count + diff.TargetMigrations.Count
            },
            conflicts = diff.Conflicts,
            schemaChanges = diff.SchemaChanges,
            migrations = new
            {
                source = diff.SourceMigrations,
                target = diff.TargetMigrations
            }
        };

        return _jsonFormatter.Format(report);
    }

    /// <summary>
    /// Generates a CSV report of migrations and changes.
    /// </summary>
    public string GenerateCsvReport(MigrationDiff diff)
    {
        var changes = diff.SchemaChanges
            .SelectMany(kv => kv.Value.Select(sc => new
            {
                MigrationName = kv.Key,
                ChangeType = sc.Type,
                ObjectName = sc.ObjectName,
                ObjectType = sc.ObjectType,
                Impact = sc.ImpactLevel
            }));

        return _csvFormatter.Format(changes);
    }

    /// <summary>
    /// Generates a text-based report suitable for console output.
    /// </summary>
    public string GenerateTextReport(MigrationDiff diff)
    {
        var sb = new System.Text.StringBuilder();

        sb.AppendLine("\n╔════════════════════════════════════════════════════════════╗");
        sb.AppendLine("║              Migration Diff Report                         ║");
        sb.AppendLine("╚════════════════════════════════════════════════════════════╝\n");

        // Summary
        sb.AppendLine("SUMMARY:");
        sb.AppendLine($"  Result: {diff.Result}");
        sb.AppendLine($"  Conflicts: {diff.Conflicts.Count}");
        sb.AppendLine($"  Schema Changes: {diff.GetTotalSchemaChanges()}");
        sb.AppendLine($"  Generated: {DateTime.UtcNow:O}\n");

        // Conflicts
        if (diff.Conflicts.Any())
        {
            sb.AppendLine("CONFLICTS:");
            foreach (var conflict in diff.Conflicts)
            {
                sb.AppendLine($"  • {conflict.Type}: {conflict.SourceFile} ↔ {conflict.TargetFile}");
            }
            sb.AppendLine();
        }

        // Schema Changes
        if (diff.SchemaChanges.Any())
        {
            sb.AppendLine("SCHEMA CHANGES:");
            foreach (var migration in diff.SchemaChanges.Keys)
            {
                sb.AppendLine($"  Migration: {migration}");
                foreach (var change in diff.SchemaChanges[migration])
                {
                    sb.AppendLine($"    - {change.Type} on {change.ObjectName} ({change.ObjectType})");
                }
            }
            sb.AppendLine();
        }

        return sb.ToString();
    }

    /// <summary>
    /// Generates an HTML report with styling and navigation.
    /// </summary>
    public string GenerateHtmlReport(MigrationDiff diff)
    {
        var bodyContent = new System.Text.StringBuilder();

        // Title
        bodyContent.AppendLine(_htmlFormatter.CreateHeading("Migration Diff Report", 1));
        bodyContent.AppendLine(_htmlFormatter.CreateParagraph($"Generated: {DateTime.UtcNow:g}"));

        // Summary section
        bodyContent.AppendLine(_htmlFormatter.CreateHeading("Summary", 2));
        bodyContent.AppendLine($"<p><strong>Result:</strong> {diff.Result}</p>");
        bodyContent.AppendLine($"<p><strong>Conflicts:</strong> {diff.Conflicts.Count}</p>");
        bodyContent.AppendLine($"<p><strong>Schema Changes:</strong> {diff.GetTotalSchemaChanges()}</p>");

        // Conflicts section
        if (diff.Conflicts.Any())
        {
            bodyContent.AppendLine(_htmlFormatter.CreateHeading("Conflicts", 2));
            var conflictData = diff.Conflicts.Select(c => new
            {
                Type = c.Type.ToString(),
                Source = c.SourceFile,
                Target = c.TargetFile,
                Blocking = c.IsBlockingConflict ? "Yes" : "No"
            });
            bodyContent.AppendLine(_htmlFormatter.GenerateTable(conflictData));
        }

        // Schema Changes section
        if (diff.SchemaChanges.Any())
        {
            bodyContent.AppendLine(_htmlFormatter.CreateHeading("Schema Changes", 2));
            var changeData = diff.SchemaChanges
                .SelectMany(kv => kv.Value.Select(sc => new
                {
                    Migration = kv.Key,
                    Type = sc.Type,
                    Object = sc.ObjectName,
                    ObjectType = sc.ObjectType
                }));
            bodyContent.AppendLine(_htmlFormatter.GenerateTable(changeData));
        }

        return _htmlFormatter.CreateDocument("Migration Diff Report", bodyContent.ToString());
    }

    /// <summary>
    /// Registers a custom report template.
    /// </summary>
    public void RegisterTemplate(string name, IReportTemplate template)
    {
        _templates[name] = template;
    }

    /// <summary>
    /// Gets a custom report template.
    /// </summary>
    public IReportTemplate? GetTemplate(string name)
    {
        return _templates.TryGetValue(name, out var template) ? template : null;
    }
}

/// <summary>
/// Interface for custom report templates.
/// </summary>
public interface IReportTemplate
{
    string Name { get; }
    string GenerateReport(MigrationDiff diff);
}

/// <summary>
/// Custom report template implementation.
/// </summary>
public abstract class ReportTemplateBase : IReportTemplate
{
    public abstract string Name { get; }
    public abstract string GenerateReport(MigrationDiff diff);
}
