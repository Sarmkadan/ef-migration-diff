using System.Globalization;
using EfMigrationDiff.Models;
using EfMigrationDiff.Services;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;

namespace EfMigrationDiff.Tests;

/// <summary>
/// Provides validation helpers for <see cref="VisualDiffOutputTests"/> instances.
/// </summary>
public static class VisualDiffOutputTestsValidation
{
    /// <summary>
    /// Validates that a <see cref="VisualDiffOutputTests"/> instance is in a valid state.
    /// </summary>
    /// <param name="value">The instance to validate.</param>
    /// <returns>A list of human-readable problems, or an empty list if valid.</returns>
    public static IReadOnlyList<string> Validate(this VisualDiffOutputTests value)
    {
        ArgumentNullException.ThrowIfNull(value);

        var problems = new List<string>();

        // Validate CreateEngine method
        try
        {
            var engine = value.CreateEngine();
            if (engine is null)
            {
                problems.Add("CreateEngine() returned null");
            }
        }
        catch (Exception ex)
        {
            problems.Add($"CreateEngine() threw: {ex.Message}");
        }

        // Validate ComputeDiff_WithIdenticalChanges_ReturnsIdenticalResult
        try
        {
            var engine = value.CreateEngine();
            var changes = new List<SchemaChange>
            {
                new("mig1", SqlChangeType.CreateTable, "CREATE TABLE Users (Id INT)")
                {
                    TableName = "Users"
                }
            };

            var result = engine.ComputeDiff(changes, changes);

            if (result is null)
            {
                problems.Add("ComputeDiff_WithIdenticalChanges_ReturnsIdenticalResult: Result is null");
            }
            else if (!result.IsIdentical)
            {
                problems.Add("ComputeDiff_WithIdenticalChanges_ReturnsIdenticalResult: Expected IsIdentical=true");
            }
            else if (result.SourceOnlyChanges.Count != 0)
            {
                problems.Add("ComputeDiff_WithIdenticalChanges_ReturnsIdenticalResult: Expected SourceOnlyChanges to be empty");
            }
            else if (result.TargetOnlyChanges.Count != 0)
            {
                problems.Add("ComputeDiff_WithIdenticalChanges_ReturnsIdenticalResult: Expected TargetOnlyChanges to be empty");
            }
        }
        catch (Exception ex)
        {
            problems.Add($"ComputeDiff_WithIdenticalChanges_ReturnsIdenticalResult threw: {ex.Message}");
        }

        // Validate ComputeDiff_WithSourceOnlyChange_PopulatesSourceOnlyList
        try
        {
            var engine = value.CreateEngine();
            var sourceChanges = new List<SchemaChange>
            {
                new("mig_src", SqlChangeType.CreateTable, "CREATE TABLE Orders (Id INT)")
                {
                    TableName = "Orders"
                },
                new("mig_src", SqlChangeType.CreateTable, "CREATE TABLE Users (Id INT)")
                {
                    TableName = "Users"
                }
            };
            var targetChanges = new List<SchemaChange>
            {
                new("mig_tgt", SqlChangeType.CreateTable, "CREATE TABLE Users (Id INT)")
                {
                    TableName = "Users"
                }
            };

            var result = engine.ComputeDiff(sourceChanges, targetChanges);

            if (result is null)
            {
                problems.Add("ComputeDiff_WithSourceOnlyChange_PopulatesSourceOnlyList: Result is null");
            }
            else if (result.IsIdentical)
            {
                problems.Add("ComputeDiff_WithSourceOnlyChange_PopulatesSourceOnlyList: Expected IsIdentical=false");
            }
            else if (result.SourceOnlyChanges.Count != 1)
            {
                problems.Add("ComputeDiff_WithSourceOnlyChange_PopulatesSourceOnlyList: Expected SourceOnlyChanges.Count=1");
            }
            else if (result.SourceOnlyChanges[0].TableName != "Orders")
            {
                problems.Add("ComputeDiff_WithSourceOnlyChange_PopulatesSourceOnlyList: Expected SourceOnlyChanges[0].TableName='Orders'");
            }
        }
        catch (Exception ex)
        {
            problems.Add($"ComputeDiff_WithSourceOnlyChange_PopulatesSourceOnlyList threw: {ex.Message}");
        }

        // Validate ComputeDiff_WithTargetOnlyChange_PopulatesTargetOnlyList
        try
        {
            var engine = value.CreateEngine();
            var sourceChanges = new List<SchemaChange>
            {
                new("mig_src", SqlChangeType.CreateTable, "CREATE TABLE Users (Id INT)")
                {
                    TableName = "Users"
                }
            };
            var targetChanges = new List<SchemaChange>
            {
                new("mig_tgt", SqlChangeType.CreateTable, "CREATE TABLE Users (Id INT)")
                {
                    TableName = "Users"
                },
                new("mig_tgt", SqlChangeType.AddColumn, "ALTER TABLE Users ADD Email NVARCHAR(255)")
                {
                    TableName = "Users",
                    ColumnName = "Email"
                }
            };

            var result = engine.ComputeDiff(sourceChanges, targetChanges);

            if (result is null)
            {
                problems.Add("ComputeDiff_WithTargetOnlyChange_PopulatesTargetOnlyList: Result is null");
            }
            else if (result.TargetOnlyChanges.Count != 1)
            {
                problems.Add("ComputeDiff_WithTargetOnlyChange_PopulatesTargetOnlyList: Expected TargetOnlyChanges.Count=1");
            }
            else if (result.TargetOnlyChanges[0].ColumnName != "Email")
            {
                problems.Add("ComputeDiff_WithTargetOnlyChange_PopulatesTargetOnlyList: Expected TargetOnlyChanges[0].ColumnName='Email'");
            }
        }
        catch (Exception ex)
        {
            problems.Add($"ComputeDiff_WithTargetOnlyChange_PopulatesTargetOnlyList threw: {ex.Message}");
        }

        // Validate ComputeDiff_WithDestructiveChange_ReportsDestructive
        try
        {
            var engine = value.CreateEngine();
            var sourceChanges = new List<SchemaChange>
            {
                new("mig_src", SqlChangeType.DropTable, "DROP TABLE LegacyData")
                {
                    TableName = "LegacyData"
                }
            };

            var result = engine.ComputeDiff(sourceChanges, new List<SchemaChange>());

            if (result is null)
            {
                problems.Add("ComputeDiff_WithDestructiveChange_ReportsDestructive: Result is null");
            }
            else if (!result.HasDestructiveChanges)
            {
                problems.Add("ComputeDiff_WithDestructiveChange_ReportsDestructive: Expected HasDestructiveChanges=true");
            }
        }
        catch (Exception ex)
        {
            problems.Add($"ComputeDiff_WithDestructiveChange_ReportsDestructive threw: {ex.Message}");
        }

        // Validate ComputeDiff_WithEmptyInputs_ReturnsIdentical
        try
        {
            var engine = value.CreateEngine();
            var result = engine.ComputeDiff(new List<SchemaChange>(), new List<SchemaChange>());

            if (result is null)
            {
                problems.Add("ComputeDiff_WithEmptyInputs_ReturnsIdentical: Result is null");
            }
            else if (!result.IsIdentical)
            {
                problems.Add("ComputeDiff_WithEmptyInputs_ReturnsIdentical: Expected IsIdentical=true");
            }
            else if (result.TotalAdded != 0)
            {
                problems.Add("ComputeDiff_WithEmptyInputs_ReturnsIdentical: Expected TotalAdded=0");
            }
            else if (result.TotalRemoved != 0)
            {
                problems.Add("ComputeDiff_WithEmptyInputs_ReturnsIdentical: Expected TotalRemoved=0");
            }
        }
        catch (Exception ex)
        {
            problems.Add($"ComputeDiff_WithEmptyInputs_ReturnsIdentical threw: {ex.Message}");
        }

        // Validate AcceptSource_BuildsPlanWithAllSourceResolutions
        try
        {
            var engine = value.CreateEngine();
            var conflictId = Guid.NewGuid();
            var threeWayDiff = new ThreeWayDiffResult
            {
                Id = Guid.NewGuid(),
                BaseLabel = "base",
                SourceLabel = "source",
                TargetLabel = "target",
                BaseToSource = MakeEmptyDiff("base", "source"),
                BaseToTarget = MakeEmptyDiff("base", "target"),
                ConflictRegions = new[]
                {
                    new MergeConflictRegion
                    {
                        Id = conflictId,
                        HunkIndex = 0,
                        Description = "test conflict"
                    }
                }
            };

            var plan = engine.AcceptSource(threeWayDiff);

            if (plan is null)
            {
                problems.Add("AcceptSource_BuildsPlanWithAllSourceResolutions: Plan is null");
            }
            else if (!plan.Resolutions.ContainsKey(conflictId))
            {
                problems.Add("AcceptSource_BuildsPlanWithAllSourceResolutions: Expected plan.Resolutions to contain conflictId");
            }
            else if (plan.Resolutions[conflictId] != MergeResolutionStrategy.AcceptSource)
            {
                problems.Add("AcceptSource_BuildsPlanWithAllSourceResolutions: Expected resolution to be AcceptSource");
            }
        }
        catch (Exception ex)
        {
            problems.Add($"AcceptSource_BuildsPlanWithAllSourceResolutions threw: {ex.Message}");
        }

        // Validate AutoMerge_WithTriviallyResolvableConflicts_ResolvesAll
        try
        {
            var engine = value.CreateEngine();
            var conflictId = Guid.NewGuid();
            var sharedLine = new DiffLine(DiffLineKind.Modified, 1, "same content");

            var region = new MergeConflictRegion
            {
                Id = conflictId,
                HunkIndex = 0,
                Description = "trivial",
                SourceLines = new[] { sharedLine },
                TargetLines = new[] { sharedLine }
            };

            var threeWayDiff = new ThreeWayDiffResult
            {
                Id = Guid.NewGuid(),
                BaseLabel = "base",
                SourceLabel = "source",
                TargetLabel = "target",
                BaseToSource = MakeEmptyDiff("base", "source"),
                BaseToTarget = MakeEmptyDiff("base", "target"),
                ConflictRegions = new[] { region }
            };

            var plan = engine.AutoMerge(threeWayDiff);

            if (plan is null)
            {
                problems.Add("AutoMerge_WithTriviallyResolvableConflicts_ResolvesAll: Plan is null");
            }
            else if (!plan.Resolutions.ContainsKey(conflictId))
            {
                problems.Add("AutoMerge_WithTriviallyResolvableConflicts_ResolvesAll: Expected plan.Resolutions to contain conflictId");
            }
            else if (plan.Resolutions[conflictId] == MergeResolutionStrategy.Unresolved)
            {
                problems.Add("AutoMerge_WithTriviallyResolvableConflicts_ResolvesAll: Expected resolution to not be Unresolved");
            }
        }
        catch (Exception ex)
        {
            problems.Add($"AutoMerge_WithTriviallyResolvableConflicts_ResolvesAll threw: {ex.Message}");
        }

        return problems.AsReadOnly();
    }

    /// <summary>
    /// Determines whether the specified <see cref="VisualDiffOutputTests"/> instance is valid.
    /// </summary>
    /// <param name="value">The instance to check.</param>
    /// <returns><c>true</c> if valid; otherwise, <c>false</c>.</returns>
    public static bool IsValid(this VisualDiffOutputTests value)
    {
        return value.Validate().Count == 0;
    }

    /// <summary>
    /// Validates the specified <see cref="VisualDiffOutputTests"/> instance and throws an <see cref="ArgumentException"/>
    /// if it is not valid.
    /// </summary>
    /// <param name="value">The instance to validate.</param>
    /// <exception cref="ArgumentException">Thrown when the instance is not valid.</exception>
    public static void EnsureValid(this VisualDiffOutputTests value)
    {
        ArgumentNullException.ThrowIfNull(value);

        var problems = value.Validate();
        if (problems.Count > 0)
        {
            throw new ArgumentException(
                $"VisualDiffOutputTests is not valid. Problems: {string.Join("; ", problems)}");
        }
    }

    private static SchemaDiffEngine CreateEngine(this VisualDiffOutputTests _) =>
        new(new ConflictDetectionService(NullLogger<ConflictDetectionService>.Instance),
            NullLogger<SchemaDiffEngine>.Instance);

    private static SchemaDiffResult MakeEmptyDiff(string source, string target) =>
        new()
        {
            Id = Guid.NewGuid(),
            SourceLabel = source,
            TargetLabel = target
        };
}