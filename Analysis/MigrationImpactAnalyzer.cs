// =============================================================================
// Author: Vladyslav Zaiets | https://sarmkadan.com
// CTO & Software Architect
// =============================================================================

using EfMigrationDiff.Models;

namespace EfMigrationDiff.Analysis;

/// <summary>
/// Analyzes the impact of migrations on database schema and application compatibility.
/// Detects breaking changes, data loss scenarios, and compatibility issues.
/// </summary>
public class MigrationImpactAnalyzer
{
    private readonly Dictionary<string, double> _riskScores = new();

    /// <summary>
    /// Analyzes migration impact and returns a comprehensive impact report.
    /// </summary>
    public MigrationImpactReport AnalyzeMigration(Migration migration)
    {
        var report = new MigrationImpactReport
        {
            MigrationName = migration.Name,
            AnalyzedAt = DateTime.UtcNow
        };

        // Analyze migration content for risky operations
        AnalyzeSchemaChanges(migration, report);
        CalculateRiskScore(migration, report);

        return report;
    }

    /// <summary>
    /// Analyzes schema changes for potential issues.
    /// </summary>
    private void AnalyzeSchemaChanges(Migration migration, MigrationImpactReport report)
    {
        var content = migration.Content ?? string.Empty;

        // Check for column drops (potential data loss)
        if (content.Contains("DropColumn", StringComparison.OrdinalIgnoreCase))
        {
            report.IssuesDetected.Add(new MigrationIssue
            {
                Severity = IssueSeverity.Critical,
                Message = "Column drop detected - potential data loss",
                LineNumber = FindLineWithContent(content, "DropColumn")
            });
        }

        // Check for table drops
        if (content.Contains("DropTable", StringComparison.OrdinalIgnoreCase))
        {
            report.IssuesDetected.Add(new MigrationIssue
            {
                Severity = IssueSeverity.Critical,
                Message = "Table drop detected - potential data loss",
                LineNumber = FindLineWithContent(content, "DropTable")
            });
        }

        // Check for index changes
        if (content.Contains("CreateIndex", StringComparison.OrdinalIgnoreCase))
        {
            report.IssuesDetected.Add(new MigrationIssue
            {
                Severity = IssueSeverity.Warning,
                Message = "Index creation detected - may impact performance during migration",
                LineNumber = FindLineWithContent(content, "CreateIndex")
            });
        }

        // Check for nullable column changes
        if (content.Contains("IsNullable", StringComparison.OrdinalIgnoreCase))
        {
            report.IssuesDetected.Add(new MigrationIssue
            {
                Severity = IssueSeverity.Info,
                Message = "Nullable constraint change detected",
                LineNumber = FindLineWithContent(content, "IsNullable")
            });
        }
    }

    /// <summary>
    /// Calculates overall risk score for a migration.
    /// </summary>
    private void CalculateRiskScore(Migration migration, MigrationImpactReport report)
    {
        double score = 0;

        // Base score from issue count
        var criticalIssues = report.IssuesDetected.Count(i => i.Severity == IssueSeverity.Critical);
        var warningIssues = report.IssuesDetected.Count(i => i.Severity == IssueSeverity.Warning);

        score += criticalIssues * 25;
        score += warningIssues * 10;

        // Adjust by migration name (newer migrations have lower baseline risk)
        if (migration.Timestamp > DateTime.UtcNow.AddDays(-7))
            score -= 5;

        report.RiskScore = Math.Max(0, Math.Min(100, score));
        report.RiskLevel = report.RiskScore switch
        {
            >= 75 => RiskLevel.Critical,
            >= 50 => RiskLevel.High,
            >= 25 => RiskLevel.Medium,
            _ => RiskLevel.Low
        };
    }

    /// <summary>
    /// Finds the line number containing specific text.
    /// </summary>
    private int FindLineWithContent(string content, string searchText)
    {
        var lines = content.Split(new[] { Environment.NewLine }, StringSplitOptions.None);
        for (int i = 0; i < lines.Length; i++)
        {
            if (lines[i].Contains(searchText, StringComparison.OrdinalIgnoreCase))
                return i + 1;
        }
        return 0;
    }

    /// <summary>
    /// Analyzes multiple migrations and identifies dependencies and risks.
    /// </summary>
    public MigrationChainAnalysis AnalyzeMigrationChain(IEnumerable<Migration> migrations)
    {
        var analysis = new MigrationChainAnalysis();
        var migrationList = migrations.OrderBy(m => m.Timestamp).ToList();

        foreach (var migration in migrationList)
        {
            var report = AnalyzeMigration(migration);
            analysis.MigrationReports.Add(report);

            if (report.RiskLevel == RiskLevel.Critical)
                analysis.HasCriticalRisks = true;
        }

        analysis.TotalMigrations = migrationList.Count;
        analysis.HighRiskCount = analysis.MigrationReports.Count(r => r.RiskLevel >= RiskLevel.High);

        return analysis;
    }
}

/// <summary>
/// Report of migration impact analysis.
/// </summary>
public class MigrationImpactReport
{
    public string MigrationName { get; set; } = string.Empty;
    public DateTime AnalyzedAt { get; set; }
    public List<MigrationIssue> IssuesDetected { get; set; } = new();
    public double RiskScore { get; set; }
    public RiskLevel RiskLevel { get; set; }

    public bool HasCriticalIssues => IssuesDetected.Any(i => i.Severity == IssueSeverity.Critical);
}

/// <summary>
/// Individual issue detected in migration analysis.
/// </summary>
public class MigrationIssue
{
    public IssueSeverity Severity { get; set; }
    public string Message { get; set; } = string.Empty;
    public int LineNumber { get; set; }
}

/// <summary>
/// Analysis of a chain of migrations.
/// </summary>
public class MigrationChainAnalysis
{
    public List<MigrationImpactReport> MigrationReports { get; set; } = new();
    public int TotalMigrations { get; set; }
    public int HighRiskCount { get; set; }
    public bool HasCriticalRisks { get; set; }

    public double GetAverageRiskScore() => MigrationReports.Any() ? MigrationReports.Average(r => r.RiskScore) : 0;
}

public enum IssueSeverity { Info, Warning, Critical }
public enum RiskLevel { Low, Medium, High, Critical }
