#nullable enable
using EfMigrationDiff.Models;

namespace EfMigrationDiff.Models;

/// <summary>
/// Represents the complete diff result between two branches' migrations.
/// </summary>
public class MigrationDiff
{
    public string Id { get; set; } = string.Empty;
    public string SourceBranchId { get; set; } = string.Empty;
    public string TargetBranchId { get; set; } = string.Empty;
    public List<Migration> OnlyInSource { get; set; } = [];
    public List<Migration> OnlyInTarget { get; set; } = [];
    public List<Migration> InBoth { get; set; } = [];
    public List<SchemaChange> SourceSchemaChanges { get; set; } = [];
    public List<SchemaChange> TargetSchemaChanges { get; set; } = [];
    public List<ConflictInfo> Conflicts { get; set; } = [];
    public DateTime CreatedAt { get; set; }
    public ComparisonResult Result { get; set; }
    public Dictionary<string, object> Summary { get; set; } = [];

    public MigrationDiff()
    {
    }

    public MigrationDiff(string sourceBranchId, string targetBranchId)
    {
        Id = Guid.NewGuid().ToString();
        SourceBranchId = sourceBranchId;
        TargetBranchId = targetBranchId;
        CreatedAt = DateTime.UtcNow;
        Result = ComparisonResult.Identical;
    }

    /// <summary>
    /// Validates the diff has required properties.
    /// </summary>
    /// <returns>True if valid, otherwise false.</returns>
    public bool IsValid()
    {
        return !string.IsNullOrWhiteSpace(SourceBranchId) &&
               !string.IsNullOrWhiteSpace(TargetBranchId) &&
               !string.IsNullOrWhiteSpace(Id);
    }

    /// <summary>
    /// Adds a migration that exists only in the source branch.
    /// </summary>
    /// <param name="migration">The migration to add.</param>
    public void AddSourceOnlyMigration(Migration migration)
    {
        OnlyInSource.Add(migration);
        UpdateComparisonResult();
    }

    /// <summary>
    /// Adds a migration that exists only in the target branch.
    /// </summary>
    /// <param name="migration">The migration to add.</param>
    public void AddTargetOnlyMigration(Migration migration)
    {
        OnlyInTarget.Add(migration);
        UpdateComparisonResult();
    }

    /// <summary>
    /// Adds a migration that exists in both branches.
    /// </summary>
    /// <param name="migration">The migration to add.</param>
    public void AddCommonMigration(Migration migration)
    {
        InBoth.Add(migration);
    }

    /// <summary>
    /// Adds a detected conflict to the diff.
    /// </summary>
    /// <param name="conflict">The conflict to add.</param>
    public void AddConflict(ConflictInfo conflict)
    {
        if (conflict.IsValid())
        {
            Conflicts.Add(conflict);
            Result = ComparisonResult.Conflicting;
        }
    }

    /// <summary>
    /// Gets the total number of schema changes.
    /// </summary>
    /// <returns>The total count of schema changes.</returns>
    public int GetTotalSchemaChanges()
    {
        return SourceSchemaChanges.Count + TargetSchemaChanges.Count;
    }

    /// <summary>
    /// Gets the number of blocking conflicts.
    /// </summary>
    /// <returns>The count of blocking conflicts.</returns>
    public int GetBlockingConflicts()
    {
        return Conflicts.Count(c => c.IsBlocking());
    }

    /// <summary>
    /// Checks if this diff has any conflicts.
    /// </summary>
    /// <returns>True if conflicts exist, otherwise false.</returns>
    public bool HasConflicts()
    {
        return Conflicts.Count > 0;
    }

    /// <summary>
    /// Checks if this diff has any blocking conflicts.
    /// </summary>
    /// <returns>True if blocking conflicts exist, otherwise false.</returns>
    public bool HasBlockingConflicts()
    {
        return GetBlockingConflicts() > 0;
    }

    /// <summary>
    /// Gets all destructive changes across both branches.
    /// </summary>
    /// <returns>A list of destructive schema changes.</returns>
    public List<SchemaChange> GetDestructiveChanges()
    {
        var all = new List<SchemaChange>();
        all.AddRange(SourceSchemaChanges.Where(c => c.IsDestructive()));
        all.AddRange(TargetSchemaChanges.Where(c => c.IsDestructive()));
        return all;
    }

    /// <summary>
    /// Gets migrations that have conflicts.
    /// </summary>
    /// <returns>A list of conflicting migration identifiers.</returns>
    public List<string> GetConflictingMigrations()
    {
        var conflicting = new HashSet<string>();
        foreach (var conflict in Conflicts)
        {
            conflicting.Add(conflict.FirstMigrationId);
            conflicting.Add(conflict.SecondMigrationId);
        }

        return conflicting.ToList();
    }

    /// <summary>
    /// Updates the comparison result based on current state.
    /// </summary>
    private void UpdateComparisonResult()
    {
        if (HasBlockingConflicts())
        {
            Result = ComparisonResult.Conflicting;
        }
        else if (OnlyInSource.Count > 0 || OnlyInTarget.Count > 0)
        {
            Result = HasConflicts() ? ComparisonResult.Incompatible : ComparisonResult.Different;
        }
        else if (SourceSchemaChanges.Count > 0 || TargetSchemaChanges.Count > 0)
        {
            Result = ComparisonResult.Similar;
        }
        else
        {
            Result = ComparisonResult.Identical;
        }
    }

    /// <summary>
    /// Generates a summary report of the diff.
    /// </summary>
    public void GenerateSummary()
    {
        Summary["SourceOnlyCount"] = OnlyInSource.Count;
        Summary["TargetOnlyCount"] = OnlyInTarget.Count;
        Summary["CommonCount"] = InBoth.Count;
        Summary["ConflictCount"] = Conflicts.Count;
        Summary["BlockingConflictCount"] = GetBlockingConflicts();
        Summary["TotalSchemaChanges"] = GetTotalSchemaChanges();
        Summary["DestructiveChangesCount"] = GetDestructiveChanges().Count;
        Summary["Result"] = Result.ToString();
        Summary["HasBlockingIssues"] = HasBlockingConflicts();
    }

    /// <summary>
    /// Gets a human-readable description of the diff result.
    /// </summary>
    /// <returns>A string description of the comparison result.</returns>
    public string GetResultDescription()
    {
        return Result switch
        {
            ComparisonResult.Identical => "Migrations are identical across branches",
            ComparisonResult.Similar => "Migrations are similar with minor differences",
            ComparisonResult.Different => "Migrations differ significantly",
            ComparisonResult.Conflicting => "Migrations have unresolved conflicts",
            ComparisonResult.Incompatible => "Migrations are incompatible",
            _ => "Unknown comparison result"
        };
    }

    public override string ToString()
    {
        return $"Diff {SourceBranchId}..{TargetBranchId}: {GetResultDescription()}";
    }
}
