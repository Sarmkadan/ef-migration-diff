// =============================================================================
// Author: Vladyslav Zaiets | https://sarmkadan.com
// CTO & Software Architect
// =============================================================================
#nullable enable

using EfMigrationDiff.Models;

namespace EfMigrationDiff.Analysis;

/// <summary>
/// Engine for analyzing and suggesting resolutions for migration conflicts.
/// Detects conflict patterns and provides recommendations for resolution strategies.
/// </summary>
public sealed class ConflictResolutionEngine
{
    private readonly Dictionary<EfMigrationDiff.Models.ConflictType, Func<ConflictInfo, ResolutionStrategy>> _strategies = new();

    /// <summary>
    /// Initializes a new instance of the <see cref="ConflictResolutionEngine"/> class.
    /// </summary>
    public ConflictResolutionEngine()
    {
        InitializeDefaultStrategies();
    }

    /// <summary>
    /// Initializes default resolution strategies for common conflict types.
    /// </summary>
    private void InitializeDefaultStrategies()
    {
        _strategies[EfMigrationDiff.Models.ConflictType.TableConflict] = conflict => new ResolutionStrategy
        {
            Type = ResolutionType.Manual,
            Description = "Manually review and merge the conflicting table operations",
            Priority = 2
        };

        _strategies[EfMigrationDiff.Models.ConflictType.ColumnConflict] = conflict => new ResolutionStrategy
        {
            Type = ResolutionType.Review,
            Description = "Review and test the conflicting column changes carefully to prevent data loss",
            Priority = 3,
            IsHighRisk = true
        };

        _strategies[EfMigrationDiff.Models.ConflictType.IndexConflict] = conflict => new ResolutionStrategy
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
        ArgumentNullException.ThrowIfNull(conflict);
        var resolution = new ConflictResolution
        {
            ConflictId = conflict.Id,
            ConflictType = conflict.ConflictType,
            AnalyzedAt = DateTime.UtcNow
        };

        // Determine conflict type and get strategy
        if (_strategies.TryGetValue(conflict.ConflictType, out var strategyFunc))
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
        ArgumentNullException.ThrowIfNull(conflicts);
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
        if (conflict.ConflictType == EfMigrationDiff.Models.ConflictType.ColumnConflict)
            return ConflictSeverity.Critical;

        // Check for blocking conflicts
        if (conflict.IsBlocking())
            return ConflictSeverity.High;

        return ConflictSeverity.Medium;
    }

    /// <summary>
    /// Generates specific recommendations for resolving a conflict.
    /// </summary>
    private List<string> GenerateRecommendations(ConflictInfo conflict)
    {
        var recommendations = new List<string>();

        switch (conflict.ConflictType)
        {
            case EfMigrationDiff.Models.ConflictType.TableConflict:
                recommendations.Add("Review competing table changes side by side before merging");
                recommendations.Add("Validate the final table definition against both branches");
                recommendations.Add("Test any dependent queries or procedures after resolution");
                break;

            case EfMigrationDiff.Models.ConflictType.ColumnConflict:
                recommendations.Add("Backup database before applying this migration");
                recommendations.Add("Verify no application code depends on the conflicting column definition");
                recommendations.Add("Document the final column contract after resolution");
                break;

            case EfMigrationDiff.Models.ConflictType.IndexConflict:
                recommendations.Add("Verify index names don't conflict");
                recommendations.Add("Consider merging index definitions if possible");
                break;

            case EfMigrationDiff.Models.ConflictType.ConstraintConflict:
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
    public void RegisterStrategy(EfMigrationDiff.Models.ConflictType type, Func<ConflictInfo, ResolutionStrategy> strategy)
    {
        ArgumentNullException.ThrowIfNull(strategy);
        _strategies[type] = strategy;
    }
}

/// <summary>
/// Resolution strategy for a conflict.
/// </summary>
public class ResolutionStrategy
{
    /// <summary>
    /// Gets or sets the recommended resolution type.
    /// </summary>
    public ResolutionType Type { get; set; }

    /// <summary>
    /// Gets or sets the human-readable strategy description.
    /// </summary>
    public string Description { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the strategy priority where lower values indicate higher priority.
    /// </summary>
    public int Priority { get; set; }

    /// <summary>
    /// Gets or sets a value indicating whether the strategy carries elevated risk.
    /// </summary>
    public bool IsHighRisk { get; set; }
}

/// <summary>
/// Complete resolution analysis for a conflict.
/// </summary>
public class ConflictResolution
{
    /// <summary>
    /// Gets or sets the identifier of the analyzed conflict.
    /// </summary>
    public string ConflictId { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the original conflict type.
    /// </summary>
    public EfMigrationDiff.Models.ConflictType ConflictType { get; set; }

    /// <summary>
    /// Gets or sets the UTC timestamp when analysis completed.
    /// </summary>
    public DateTime AnalyzedAt { get; set; }

    /// <summary>
    /// Gets or sets the derived resolution severity.
    /// </summary>
    public ConflictSeverity Severity { get; set; }

    /// <summary>
    /// Gets or sets the recommended strategy.
    /// </summary>
    public ResolutionStrategy RecommendedStrategy { get; set; } = new();

    /// <summary>
    /// Gets or sets the generated recommendations.
    /// </summary>
    public List<string> Recommendations { get; set; } = new();
}

/// <summary>
/// Report of conflict resolutions for a batch.
/// </summary>
public class ConflictResolutionReport
{
    /// <summary>
    /// Gets or sets the UTC timestamp when the batch analysis completed.
    /// </summary>
    public DateTime AnalyzedAt { get; set; }

    /// <summary>
    /// Gets or sets the per-conflict resolution details.
    /// </summary>
    public List<ConflictResolution> Resolutions { get; set; } = new();

    /// <summary>
    /// Gets or sets the total number of analyzed conflicts.
    /// </summary>
    public int TotalConflicts { get; set; }

    /// <summary>
    /// Gets or sets the number of critical conflicts.
    /// </summary>
    public int CriticalCount { get; set; }

    /// <summary>
    /// Gets or sets the number of high-severity conflicts.
    /// </summary>
    public int HighCount { get; set; }

    /// <summary>
    /// Gets or sets the number of conflicts that can be automatically resolved.
    /// </summary>
    public int CanAutoResolve { get; set; }

    /// <summary>
    /// Gets a value indicating whether the batch can proceed without manual intervention.
    /// </summary>
    public bool CanProceedWithoutManualIntervention =>
        !Resolutions.Any(r => r.RecommendedStrategy.Type == ResolutionType.Manual && r.Severity == ConflictSeverity.Critical);
}

/// <summary>
/// Severity levels used by the conflict resolution analysis.
/// </summary>
public enum ConflictSeverity
{
    /// <summary>
    /// Low severity.
    /// </summary>
    Low,

    /// <summary>
    /// Medium severity.
    /// </summary>
    Medium,

    /// <summary>
    /// High severity.
    /// </summary>
    High,

    /// <summary>
    /// Critical severity.
    /// </summary>
    Critical
}

/// <summary>
/// Resolution modes produced by the conflict analysis engine.
/// </summary>
public enum ResolutionType
{
    /// <summary>
    /// The conflict can be resolved automatically.
    /// </summary>
    Automatic,

    /// <summary>
    /// The conflict requires manual intervention.
    /// </summary>
    Manual,

    /// <summary>
    /// The conflict requires explicit review before proceeding.
    /// </summary>
    Review
}
