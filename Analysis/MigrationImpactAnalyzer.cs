#nullable enable
using System.Globalization;
using EfMigrationDiff.Models;

namespace EfMigrationDiff.Analysis;

/// <summary>
/// Analyzes the impact of migrations on database schema and application compatibility.
/// Detects breaking changes, data loss scenarios, and compatibility issues.
/// </summary>
public class MigrationImpactAnalyzer
{
    // Migration content markers (matched case-insensitively)
    private const string DropColumnMarker = "DropColumn";
    private const string DropTableMarker = "DropTable";
    private const string CreateIndexMarker = "CreateIndex";
    private const string IsNullableMarker = "IsNullable";

    // Issue messages
    private const string DropColumnMessage = "Column drop detected - potential data loss";
    private const string DropTableMessage = "Table drop detected - potential data loss";
    private const string CreateIndexMessage = "Index creation detected - may impact performance during migration";
    private const string IsNullableMessage = "Nullable constraint change detected";

    // Risk scoring weights
    private const double CriticalIssueScoreWeight = 25;
    private const double WarningIssueScoreWeight = 10;
    private const double RecentMigrationScoreReduction = 5;
    private const double MinRiskScore = 0;
    private const double MaxRiskScore = 100;

    // Risk level thresholds (inclusive lower bounds)
    private const double CriticalRiskThreshold = 75;
    private const double HighRiskThreshold = 50;
    private const double MediumRiskThreshold = 25;

    // Recent migration window and timestamp format
    private const int RecentMigrationWindowDays = 7;
    private const string MigrationTimestampFormat = "yyyyMMddHHmmss";

    // Line numbers are 1-based; FindLineWithContent returns this when not found
    private const int FirstLineNumber = 1;
    private const int LineNotFound = 0;

    public override string ToString() => "MigrationImpactAnalyzer { }";

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
        if (content.Contains(DropColumnMarker, StringComparison.OrdinalIgnoreCase))
        {
            report.IssuesDetected.Add(new MigrationIssue
            {
                Severity = IssueSeverity.Critical,
                Message = DropColumnMessage,
                LineNumber = FindLineWithContent(content, DropColumnMarker)
            });
        }

        // Check for table drops
        if (content.Contains(DropTableMarker, StringComparison.OrdinalIgnoreCase))
        {
            report.IssuesDetected.Add(new MigrationIssue
            {
                Severity = IssueSeverity.Critical,
                Message = DropTableMessage,
                LineNumber = FindLineWithContent(content, DropTableMarker)
            });
        }

        // Check for index changes
        if (content.Contains(CreateIndexMarker, StringComparison.OrdinalIgnoreCase))
        {
            report.IssuesDetected.Add(new MigrationIssue
            {
                Severity = IssueSeverity.Warning,
                Message = CreateIndexMessage,
                LineNumber = FindLineWithContent(content, CreateIndexMarker)
            });
        }

        // Check for nullable column changes
        if (content.Contains(IsNullableMarker, StringComparison.OrdinalIgnoreCase))
        {
            report.IssuesDetected.Add(new MigrationIssue
            {
                Severity = IssueSeverity.Info,
                Message = IsNullableMessage,
                LineNumber = FindLineWithContent(content, IsNullableMarker)
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

        score += criticalIssues * CriticalIssueScoreWeight;
        score += warningIssues * WarningIssueScoreWeight;

        // Adjust by migration timestamp (newer migrations have lower baseline risk)
        if (DateTime.TryParseExact(
                migration.Timestamp,
                MigrationTimestampFormat,
                CultureInfo.InvariantCulture,
                DateTimeStyles.AssumeUniversal | DateTimeStyles.AdjustToUniversal,
                out var migrationTimestamp) &&
            migrationTimestamp > DateTime.UtcNow.AddDays(-RecentMigrationWindowDays))
        {
            score -= RecentMigrationScoreReduction;
        }

        report.RiskScore = Math.Max(MinRiskScore, Math.Min(MaxRiskScore, score));
        report.RiskLevel = report.RiskScore switch
        {
            >= CriticalRiskThreshold => RiskLevel.Critical,
            >= HighRiskThreshold => RiskLevel.High,
            >= MediumRiskThreshold => RiskLevel.Medium,
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
                return i + FirstLineNumber;
        }
        return LineNotFound;
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
    public override string ToString() => $"MigrationImpactReport {{ MigrationName = {MigrationName}, AnalyzedAt = {AnalyzedAt}, IssuesDetected = {IssuesDetected.Count}, RiskScore = {RiskScore}, RiskLevel = {RiskLevel}, HasCriticalIssues = {HasCriticalIssues} }}";

    /// <summary>
    /// Gets or sets the migration name.
    /// </summary>
    public string MigrationName { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the UTC timestamp when analysis completed.
    /// </summary>
    public DateTime AnalyzedAt { get; set; }

    /// <summary>
    /// Gets or sets the detected issues.
    /// </summary>
    public List<MigrationIssue> IssuesDetected { get; set; } = new();

    /// <summary>
    /// Gets or sets the calculated numeric risk score.
    /// </summary>
    public double RiskScore { get; set; }

    /// <summary>
    /// Gets or sets the derived risk level.
    /// </summary>
    public RiskLevel RiskLevel { get; set; }

    /// <summary>
    /// Gets a value indicating whether any critical issues were detected.
    /// </summary>
    public bool HasCriticalIssues => IssuesDetected.Any(i => i.Severity == IssueSeverity.Critical);
}

/// <summary>
/// Individual issue detected in migration analysis.
/// </summary>
public class MigrationIssue
{
    /// <summary>
    /// Gets or sets the issue severity.
    /// </summary>
    public IssueSeverity Severity { get; set; }

    /// <summary>
    /// Gets or sets the issue message.
    /// </summary>
    public string Message { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the source line number associated with the issue.
    /// </summary>
    public int LineNumber { get; set; }
}

/// <summary>
/// Analysis of a chain of migrations.
/// </summary>
public class MigrationChainAnalysis
{
    /// <summary>
    /// Gets or sets the per-migration reports in the analyzed chain.
    /// </summary>
    public List<MigrationImpactReport> MigrationReports { get; set; } = new();

    /// <summary>
    /// Gets or sets the total number of analyzed migrations.
    /// </summary>
    public int TotalMigrations { get; set; }

    /// <summary>
    /// Gets or sets the count of high-risk migrations.
    /// </summary>
    public int HighRiskCount { get; set; }

    /// <summary>
    /// Gets or sets a value indicating whether critical risks were detected.
    /// </summary>
    public bool HasCriticalRisks { get; set; }

    /// <summary>
    /// Gets the average risk score across all analyzed migrations.
    /// </summary>
    public double GetAverageRiskScore() => MigrationReports.Any() ? MigrationReports.Average(r => r.RiskScore) : 0;
}

/// <summary>
/// Severity levels for migration analysis issues.
/// </summary>
public enum IssueSeverity
{
    /// <summary>
    /// Informational issue.
    /// </summary>
    Info,

    /// <summary>
    /// Warning issue.
    /// </summary>
    Warning,

    /// <summary>
    /// Critical issue.
    /// </summary>
    Critical
}

/// <summary>
/// Overall risk levels assigned to a migration.
/// </summary>
public enum RiskLevel
{
    /// <summary>
    /// Low risk.
    /// </summary>
    Low,

    /// <summary>
    /// Medium risk.
    /// </summary>
    Medium,

    /// <summary>
    /// High risk.
    /// </summary>
    High,

    /// <summary>
    /// Critical risk.
    /// </summary>
    Critical
}
