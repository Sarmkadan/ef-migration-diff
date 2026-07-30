#nullable enable
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using EfMigrationDiff.Models;
using EfMigrationDiff.Formatters;

namespace EfMigrationDiff.Reports;

/// <summary>
/// Interface for report generation engine supporting multiple formats and templates.
/// </summary>
public interface IReportEngine
{
    /// <summary>
    /// Generates a report in the specified format.
    /// </summary>
    string GenerateReport(MigrationDiff diff, string format = "html");

    /// <summary>
    /// Generates a JSON report with complete migration data.
    /// </>
    string GenerateJsonReport(MigrationDiff diff);

    /// <summary>
    /// Generates a CSV report of migrations and changes.
    /// </summary>
    string GenerateCsvReport(MigrationDiff diff);

    /// <summary>
    /// Generates a text-based report suitable for console output.
    /// </summary>
    string GenerateTextReport(MigrationDiff diff);

    /// <summary>
    /// Generates an HTML report with styling and navigation.
    /// </summary>
    string GenerateHtmlReport(MigrationDiff diff);

    /// <summary>
    /// Generates a Markdown report using the MarkdownFormatter.
    /// </summary>
    string GenerateMarkdownReport(MigrationDiff diff);

    /// <summary>
    /// Registers a custom report template.
    /// </summary>
    void RegisterTemplate(string name, IReportTemplate template);

    /// <summary>
    /// Gets a custom report template.
    /// </summary>
    IReportTemplate? GetTemplate(string name);
}

/// <summary>
/// Advanced report generation engine supporting multiple formats and templates.
/// Generates comprehensive migration analysis reports with charts, summaries, and recommendations.
/// </summary>
public class ReportEngine : IReportEngine
{
    private readonly JsonFormatter _jsonFormatter;
    private readonly CsvFormatter _csvFormatter;
    private readonly HtmlFormatter _htmlFormatter;
    private readonly MarkdownFormatter _markdownFormatter;
    private readonly Dictionary<string, IReportTemplate> _templates = new();

    public ReportEngine()
    {
        _jsonFormatter = new JsonFormatter(prettyPrint: true);
        _csvFormatter = new CsvFormatter();
        _htmlFormatter = new HtmlFormatter();
        _markdownFormatter = new MarkdownFormatter();
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
            "markdown" => GenerateMarkdownReport(diff),
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
                migrationCount = diff.OnlyInSource.Count + diff.OnlyInTarget.Count + diff.InBoth.Count
            },
            conflicts = diff.Conflicts,
            schemaChanges = new
            {
                source = diff.SourceSchemaChanges,
                target = diff.TargetSchemaChanges
            },
            migrations = new
            {
                source = diff.OnlyInSource,
                target = diff.OnlyInTarget,
                common = diff.InBoth
            }
        };

        return _jsonFormatter.Format(report);
    }

    /// <summary>
    /// Generates a CSV report of migrations and changes.
    /// </summary>
    public string GenerateCsvReport(MigrationDiff diff)
    {
        var changes = diff.SourceSchemaChanges
            .Select(sc => new
            {
                Side = "Source",
                MigrationName = sc.MigrationId,
                ChangeType = sc.ChangeType,
                TableName = sc.TableName,
                ColumnName = sc.ColumnName,
                Description = sc.GetDescription(),
                Destructive = sc.IsDestructive()
            })
            .Concat(diff.TargetSchemaChanges.Select(sc => new
            {
                Side = "Target",
                MigrationName = sc.MigrationId,
                ChangeType = sc.ChangeType,
                TableName = sc.TableName,
                ColumnName = sc.ColumnName,
                Description = sc.GetDescription(),
                Destructive = sc.IsDestructive()
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
                sb.AppendLine($"  • [{conflict.Severity}] {conflict.GetTitle()}: {conflict.FirstMigrationId} ↔ {conflict.SecondMigrationId}");
            }
            sb.AppendLine();
        }

        // Schema Changes
        var allChanges = diff.SourceSchemaChanges.Concat(diff.TargetSchemaChanges).ToList();
        if (allChanges.Any())
        {
            sb.AppendLine("SCHEMA CHANGES:");
            foreach (var change in allChanges)
            {
                sb.AppendLine($"  - {change.GetDescription()} [Migration: {change.MigrationId}]");
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
                Type     = c.ConflictType.ToString(),
                Source   = c.FirstMigrationId,
                Target   = c.SecondMigrationId,
                Blocking = c.IsBlocking() ? "Yes" : "No"
            });
            bodyContent.AppendLine(_htmlFormatter.GenerateTable(conflictData));
        }

        // Schema Changes section
        var allSchemaChanges = diff.SourceSchemaChanges.Concat(diff.TargetSchemaChanges).ToList();
        if (allSchemaChanges.Any())
        {
            bodyContent.AppendLine(_htmlFormatter.CreateHeading("Schema Changes", 2));
            var changeData = allSchemaChanges.Select(sc => new
            {
                Migration   = sc.MigrationId,
                Description = sc.GetDescription(),
                Table       = sc.TableName,
                Destructive = sc.IsDestructive() ? "Yes" : "No"
            });
            bodyContent.AppendLine(_htmlFormatter.GenerateTable(changeData));
        }

        return _htmlFormatter.CreateDocument("Migration Diff Report", bodyContent.ToString());
    }

    /// <summary>
    /// Generates a Markdown report using the MarkdownFormatter.
    /// </summary>
    public string GenerateMarkdownReport(MigrationDiff diff)
    {
        return _markdownFormatter.Format(diff);
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
