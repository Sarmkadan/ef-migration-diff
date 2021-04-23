// =============================================================================
// Author: Vladyslav Zaiets | https://sarmkadan.com
// CTO & Software Architect
// =============================================================================

using EfMigrationDiff.Models;

namespace EfMigrationDiff.Analysis;

/// <summary>
/// Engine for analyzing and suggesting resolutions for migration conflicts.
/// Detects conflict patterns and provides recommendations for resolution strategies.
/// </summary>
public class ConflictResolutionEngine
{
    private readonly Dictionary<ConflictType, Func<ConflictInfo, ResolutionStrategy>> _strategies = new();

    public ConflictResolutionEngine()
    {
        InitializeDefaultStrategies();
    }

    /// <summary>
    /// Initializes default resolution strategies for common conflict types.
    /// </summary>
    private void InitializeDefaultStrategies()
    {
        _strategies[ConflictType.ColumnRename] = conflict => new ResolutionStrategy
        {
            Type = ResolutionType.Manual,
            Description = "Manually merge the rename operations",
            Priority = 2
        };

        _strategies[ConflictType.ColumnDrop] = conflict => new ResolutionStrategy
        {
            Type = ResolutionType.Review,
            Description = "Review and test the column drop carefully to prevent data loss",
            Priority = 3,
            IsHighRisk = true
        };

        _strategies[ConflictType.IndexConflict] = conflict => new ResolutionStrategy
        {
            Type = ResolutionType.Automatic,
            Description = "Can be safely merged - index operations are usually idempotent",
            Priority = 1
        };
    }

    /// <summary>
    /// Analyzes a conflict and returns resolution suggestions.
    /// </summary>
    public ConflictResolution ResolveConflict(ConflictInfo conflict)
    {
        var resolution = new ConflictResolution
        {
            ConflictId = conflict.Id,
            ConflictType = conflict.Type,
            AnalyzedAt = DateTime.UtcNow
        };

        // Determine conflict type and get strategy
        if (_strategies.TryGetValue(conflict.Type, out var strategyFunc))
        {
            resolution.RecommendedStrategy = strategyFunc(conflict);
        }
        else
        {
            resolution.RecommendedStrategy = GetDefaultStrategy(conflict);
        }

        // Analyze severity
        resolution.Severity = AnalyzeSeverity(conflict);

        // Generate recommendations
        resolution.Recommendations = GenerateRecommendations(conflict);

        return resolution;
    }

    /// <summary>
    /// Analyzes multiple conflicts and generates a batch resolution report.
    /// </summary>
    public ConflictResolutionReport ResolveBatch(IEnumerable<ConflictInfo> conflicts)
    {
        var report = new ConflictResolutionReport
        {
            AnalyzedAt = DateTime.UtcNow
        };

        var conflictList = conflicts.ToList();
        foreach (var conflict in conflictList)
        {
            var resolution = ResolveConflict(conflict);
            report.Resolutions.Add(resolution);
        }

        // Calculate summary
        report.TotalConflicts = conflictList.Count;
        report.CriticalCount = report.Resolutions.Count(r => r.Severity == ConflictSeverity.Critical);
        report.HighCount = report.Resolutions.Count(r => r.Severity == ConflictSeverity.High);
        report.CanAutoResolve = report.Resolutions.Count(r => r.RecommendedStrategy.Type == ResolutionType.Automatic);

        return report;
    }

    /// <summary>
    /// Gets the default resolution strategy for unknown conflict types.
    /// </summary>
    private ResolutionStrategy GetDefaultStrategy(ConflictInfo conflict)
    {
        return new ResolutionStrategy
        {
            Type = ResolutionType.Manual,
            Description = "Requires manual review and resolution",
            Priority = 2
        };
    }

    /// <summary>
    /// Analyzes the severity of a conflict.
    /// </summary>
    private ConflictSeverity AnalyzeSeverity(ConflictInfo conflict)
    {
        // Check for data loss potential
        if (conflict.Type == ConflictType.ColumnDrop)
            return ConflictSeverity.Critical;

        // Check for blocking conflicts
        if (conflict.IsBlocking)
            return ConflictSeverity.High;

        return ConflictSeverity.Medium;
    }

    /// <summary>
    /// Generates specific recommendations for resolving a conflict.
    /// </summary>
    private List<string> GenerateRecommendations(ConflictInfo conflict)
    {
        var recommendations = new List<string>();

        switch (conflict.Type)
        {
            case ConflictType.ColumnRename:
                recommendations.Add("Verify column rename doesn't break existing data");
                recommendations.Add("Update any stored procedures or views that reference the column");
                recommendations.Add("Test application code that uses the renamed column");
                break;

            case ConflictType.ColumnDrop:
                recommendations.Add("Backup database before applying this migration");
                recommendations.Add("Verify no application code depends on the dropped column");
                recommendations.Add("Document the reason for column removal");
                break;

            case ConflictType.IndexConflict:
                recommendations.Add("Verify index names don't conflict");
                recommendations.Add("Consider merging index definitions if possible");
                break;

            case ConflictType.ConstraintViolation:
                recommendations.Add("Review foreign key constraints");
                recommendations.Add("Ensure referential integrity is maintained");
                recommendations.Add("Check for circular dependencies");
                break;

            default:
                recommendations.Add("Review conflict manually");
                recommendations.Add("Run unit tests after resolution");
                break;
        }

        recommendations.Add("Run comprehensive integration tests");

        return recommendations;
    }

    /// <summary>
    /// Registers a custom resolution strategy for a conflict type.
    /// </summary>
    public void RegisterStrategy(ConflictType type, Func<ConflictInfo, ResolutionStrategy> strategy)
    {
        _strategies[type] = strategy;
    }
}

/// <summary>
/// Resolution strategy for a conflict.
/// </summary>
public class ResolutionStrategy
{
    public ResolutionType Type { get; set; }
    public string Description { get; set; } = string.Empty;
    public int Priority { get; set; } // 1 = highest priority
    public bool IsHighRisk { get; set; }
}

/// <summary>
/// Complete resolution analysis for a conflict.
/// </summary>
public class ConflictResolution
{
    public string ConflictId { get; set; } = string.Empty;
    public ConflictType ConflictType { get; set; }
    public DateTime AnalyzedAt { get; set; }
    public ConflictSeverity Severity { get; set; }
    public ResolutionStrategy RecommendedStrategy { get; set; } = new();
    public List<string> Recommendations { get; set; } = new();
}

/// <summary>
/// Report of conflict resolutions for a batch.
/// </summary>
public class ConflictResolutionReport
{
    public DateTime AnalyzedAt { get; set; }
    public List<ConflictResolution> Resolutions { get; set; } = new();
    public int TotalConflicts { get; set; }
    public int CriticalCount { get; set; }
    public int HighCount { get; set; }
    public int CanAutoResolve { get; set; }

    public bool CanProceedWithoutManualIntervention =>
        !Resolutions.Any(r => r.RecommendedStrategy.Type == ResolutionType.Manual && r.Severity == ConflictSeverity.Critical);
}

public enum ConflictType
{
    ColumnRename,
    ColumnDrop,
    IndexConflict,
    ConstraintViolation,
    SchemaConflict,
    Unknown
}

public enum ConflictSeverity
{
    Low,
    Medium,
    High,
    Critical
}

public enum ResolutionType
{
    Automatic,
    Manual,
    Review
}
