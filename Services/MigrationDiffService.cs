#nullable enable
// =============================================================================
// Author: Vladyslav Zaiets | https://sarmkadan.com
// CTO & Software Architect
// =============================================================================

using EfMigrationDiff.Models;
using EfMigrationDiff.Repositories;

namespace EfMigrationDiff.Services;

/// <summary>
/// Service for comparing migrations between branches and generating diff reports.
/// </summary>
public class MigrationDiffService
{
    private readonly MigrationRepository _migrationRepository;
    private readonly ConflictDetectionService _conflictDetectionService;
    private readonly SchemaChangeDetectorService _schemaChangeDetectorService;

    public MigrationDiffService(
        MigrationRepository migrationRepository,
        ConflictDetectionService conflictDetectionService,
        SchemaChangeDetectorService schemaChangeDetectorService)
    {
        _migrationRepository = migrationRepository;
        _conflictDetectionService = conflictDetectionService;
        _schemaChangeDetectorService = schemaChangeDetectorService;
    }

    /// <summary>
    /// Compares migrations between two branches and generates a diff report
    /// containing source-only, target-only, and common migrations along with
    /// detected schema conflicts. Retrieves all migrations from both branches,
    /// categorizes them, detects schema changes, and identifies conflicts.
    /// </summary>
    /// <param name="sourceBranch">The source (feature) branch to compare.</param>
    /// <param name="targetBranch">The target (base) branch to compare against.</param>
    /// <returns>
    /// A <see cref="MigrationDiff"/> containing categorized migrations, schema changes,
    /// and any detected conflicts between the two branches.
    /// </returns>
    /// <exception cref="ArgumentNullException">
    /// Thrown when <paramref name="sourceBranch"/> or <paramref name="targetBranch"/> is null.
    /// </exception>
    public MigrationDiff CompareBranches(BranchInfo sourceBranch, BranchInfo targetBranch)
    {
        ArgumentNullException.ThrowIfNull(sourceBranch);
        ArgumentNullException.ThrowIfNull(targetBranch);

        var diff = new MigrationDiff(sourceBranch.Id, targetBranch.Id);

        // Get all migrations for each branch
        var sourceMigrations = GetBranchMigrations(sourceBranch);
        var targetMigrations = GetBranchMigrations(targetBranch);

        // Categorize migrations
        CategorizeMigrations(sourceMigrations, targetMigrations, diff);

        // Detect schema changes in source
        foreach (var migration in sourceMigrations)
        {
            var changes = _schemaChangeDetectorService.DetectChanges(migration);
            diff.SourceSchemaChanges.AddRange(changes);
        }

        // Detect schema changes in target
        foreach (var migration in targetMigrations)
        {
            var changes = _schemaChangeDetectorService.DetectChanges(migration);
            diff.TargetSchemaChanges.AddRange(changes);
        }

        // Detect conflicts
        var conflicts = _conflictDetectionService.DetectConflicts(diff.SourceSchemaChanges, diff.TargetSchemaChanges);
        foreach (var conflict in conflicts)
        {
            diff.AddConflict(conflict);
        }

        diff.GenerateSummary();
        return diff;
    }

    /// <summary>
    /// Compares migrations within a single DbContext across branches.
    /// Useful when a project contains multiple DbContext classes and you need
    /// to isolate migration analysis to a specific database context.
    /// </summary>
    /// <param name="sourceBranch">The base branch to compare from.</param>
    /// <param name="targetBranch">The feature or target branch to compare against.</param>
    /// <param name="dbContextName">
    /// The fully qualified or simple name of the DbContext class to filter migrations by.
    /// Only migrations belonging to this context are included in the comparison.
    /// </param>
    /// <returns>
    /// A <see cref="MigrationDiff"/> scoped to migrations from the specified DbContext only.
    /// </returns>
    public MigrationDiff CompareDbContextMigrations(
        BranchInfo sourceBranch,
        BranchInfo targetBranch,
        string dbContextName)
    {
        var diff = new MigrationDiff(sourceBranch.Id, targetBranch.Id);

        var sourceMigrations = GetContextMigrations(sourceBranch, dbContextName);
        var targetMigrations = GetContextMigrations(targetBranch, dbContextName);

        CategorizeMigrations(sourceMigrations, targetMigrations, diff);

        foreach (var migration in sourceMigrations)
        {
            diff.SourceSchemaChanges.AddRange(_schemaChangeDetectorService.DetectChanges(migration));
        }

        foreach (var migration in targetMigrations)
        {
            diff.TargetSchemaChanges.AddRange(_schemaChangeDetectorService.DetectChanges(migration));
        }

        var conflicts = _conflictDetectionService.DetectConflicts(diff.SourceSchemaChanges, diff.TargetSchemaChanges);
        foreach (var conflict in conflicts)
        {
            diff.AddConflict(conflict);
        }

        diff.GenerateSummary();
        return diff;
    }

    /// <summary>
    /// Gets all migrations for a specific branch.
    /// </summary>
    private List<Migration> GetBranchMigrations(BranchInfo branch)
    {
        var migrations = new List<Migration>();

        foreach (var migrationId in branch.MigrationIds)
        {
            var migration = _migrationRepository.GetById(migrationId);
            if (migration is not null)
            {
                migrations.Add(migration);
            }
        }

        return migrations.OrderBy(m => m.Sequence).ToList();
    }

    /// <summary>
    /// Gets migrations for a specific DbContext in a branch.
    /// </summary>
    private List<Migration> GetContextMigrations(BranchInfo branch, string dbContextName)
    {
        var migrations = new List<Migration>();

        foreach (var migrationId in branch.MigrationIds)
        {
            var migration = _migrationRepository.GetById(migrationId);
            if (migration?.DbContextName == dbContextName)
            {
                migrations.Add(migration);
            }
        }

        return migrations.OrderBy(m => m.Sequence).ToList();
    }

    /// <summary>
    /// Categorizes migrations into source-only, target-only, and common.
    /// </summary>
    private void CategorizeMigrations(
        List<Migration> sourceMigrations,
        List<Migration> targetMigrations,
        MigrationDiff diff)
    {
        var targetIds = new HashSet<string>(targetMigrations.Select(m => m.Id));
        var sourceIds = new HashSet<string>(sourceMigrations.Select(m => m.Id));

        foreach (var migration in sourceMigrations)
        {
            if (targetIds.Contains(migration.Id))
            {
                diff.AddCommonMigration(migration);
            }
            else
            {
                diff.AddSourceOnlyMigration(migration);
            }
        }

        foreach (var migration in targetMigrations)
        {
            if (!sourceIds.Contains(migration.Id))
            {
                diff.AddTargetOnlyMigration(migration);
            }
        }
    }

    /// <summary>
    /// Generates a detailed comparison report suitable for console or text display.
    /// Includes source-only and target-only migration lists, common migration counts,
    /// conflict details with blocking status, and schema change statistics.
    /// </summary>
    /// <param name="diff">The migration diff result to generate the report from.</param>
    /// <returns>A formatted multi-line string containing the full comparison report.</returns>
    public string GenerateReport(MigrationDiff diff)
    {
        var report = new System.Text.StringBuilder();
        report.AppendLine("=== Migration Diff Report ===");
        report.AppendLine($"Result: {diff.GetResultDescription()}");
        report.AppendLine();

        report.AppendLine($"Source Only Migrations: {diff.OnlyInSource.Count}");
        foreach (var migration in diff.OnlyInSource)
        {
            report.AppendLine($"  - {migration.Name}");
        }
        report.AppendLine();

        report.AppendLine($"Target Only Migrations: {diff.OnlyInTarget.Count}");
        foreach (var migration in diff.OnlyInTarget)
        {
            report.AppendLine($"  - {migration.Name}");
        }
        report.AppendLine();

        report.AppendLine($"Common Migrations: {diff.InBoth.Count}");
        report.AppendLine();

        if (diff.HasConflicts())
        {
            report.AppendLine($"Conflicts Detected: {diff.Conflicts.Count}");
            report.AppendLine($"Blocking Conflicts: {diff.GetBlockingConflicts()}");
            foreach (var conflict in diff.Conflicts)
            {
                report.AppendLine($"  - {conflict}");
            }
            report.AppendLine();
        }

        report.AppendLine($"Schema Changes: {diff.GetTotalSchemaChanges()}");
        report.AppendLine($"Destructive Changes: {diff.GetDestructiveChanges().Count}");

        return report.ToString();
    }
}
