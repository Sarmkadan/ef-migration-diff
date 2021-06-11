#nullable enable

using System;
using System.Collections.Generic;
using System.Globalization;

namespace EfMigrationDiff.Tests;

/// <summary>
/// Provides validation helpers for <see cref="ReportGenerationServiceTests"/> instances.
/// </summary>
public static class ReportGenerationServiceTestsValidation
{
    /// <summary>
    /// Validates the specified <see cref="ReportGenerationServiceTests"/> instance.
    /// </summary>
    /// <param name="value">The instance to validate.</param>
    /// <returns>A list of validation problems; empty if the instance is valid.</returns>
    /// <exception cref="ArgumentNullException">Thrown if <paramref name="value"/> is null.</exception>
    public static IReadOnlyList<string> Validate(this ReportGenerationServiceTests? value)
    {
        ArgumentNullException.ThrowIfNull(value);

        var problems = new List<string>();

        // Validate all public methods are present and not null
        if (value.GenerateTextReport_WithDiffContainingConflicts_IncludesConflictSummary == null)
        {
            problems.Add("Method GenerateTextReport_WithDiffContainingConflicts_IncludesConflictSummary is null");
        }

        if (value.GenerateTextReport_WithMultipleMigrations_IncludesMigrationSummary == null)
        {
            problems.Add("Method GenerateTextReport_WithMultipleMigrations_IncludesMigrationSummary is null");
        }

        if (value.GenerateTextReport_WithSchemaChanges_IncludesSchemaChangeSummary == null)
        {
            problems.Add("Method GenerateTextReport_WithSchemaChanges_IncludesSchemaChangeSummary is null");
        }

        if (value.GenerateTextReport_WithNoIssues_ReportsCleanComparison == null)
        {
            problems.Add("Method GenerateTextReport_WithNoIssues_ReportsCleanComparison is null");
        }

        if (value.GenerateJsonReport_ProducesValidJson == null)
        {
            problems.Add("Method GenerateJsonReport_ProducesValidJson is null");
        }

        if (value.GenerateJsonReport_IncludesAllMigrationCategories == null)
        {
            problems.Add("Method GenerateJsonReport_IncludesAllMigrationCategories is null");
        }

        if (value.GenerateJsonReport_IncludesConflicts == null)
        {
            problems.Add("Method GenerateJsonReport_IncludesConflicts is null");
        }

        if (value.GenerateJsonReport_IncludesSchemaChanges == null)
        {
            problems.Add("Method GenerateJsonReport_IncludesSchemaChanges is null");
        }

        if (value.GenerateHtmlReport_ProducesValidHtml == null)
        {
            problems.Add("Method GenerateHtmlReport_ProducesValidHtml is null");
        }

        if (value.GenerateConflictSummary_WithConflicts_IncludesAllConflictDetails == null)
        {
            problems.Add("Method GenerateConflictSummary_WithConflicts_IncludesAllConflictDetails is null");
        }

        if (value.GenerateConflictSummary_WithNoConflicts_ReturnsNoConflictsMessage == null)
        {
            problems.Add("Method GenerateConflictSummary_WithNoConflicts_ReturnsNoConflictsMessage is null");
        }

        if (value.GenerateReport_WithDifferentFormats_AllProduceSomeOutput == null)
        {
            problems.Add("Method GenerateReport_WithDifferentFormats_AllProduceSomeOutput is null");
        }

        if (value.GenerateJsonReport_WithDestructiveChanges_IncludesDestructiveChanges == null)
        {
            problems.Add("Method GenerateJsonReport_WithDestructiveChanges_IncludesDestructiveChanges is null");
        }

        if (value.GenerateTextReport_IncludesTimestamp == null)
        {
            problems.Add("Method GenerateTextReport_IncludesTimestamp is null");
        }

        if (value.GenerateHtmlReport_WithMultipleConflicts_CreatesProperTable == null)
        {
            problems.Add("Method GenerateHtmlReport_WithMultipleConflicts_CreatesProperTable is null");
        }

        return problems.AsReadOnly();
    }

    /// <summary>
    /// Determines whether the specified <see cref="ReportGenerationServiceTests"/> instance is valid.
    /// </summary>
    /// <param name="value">The instance to check.</param>
    /// <returns><see langword="true"/> if the instance is valid; otherwise, <see langword="false"/>.</returns>
    public static bool IsValid(this ReportGenerationServiceTests? value)
    {
        return value.Validate().Count == 0;
    }

    /// <summary>
    /// Ensures that the specified <see cref="ReportGenerationServiceTests"/> instance is valid.
    /// </summary>
    /// <param name="value">The instance to validate.</param>
    /// <exception cref="ArgumentNullException">Thrown if <paramref name="value"/> is null.</exception>
    /// <exception cref="ArgumentException">Thrown if the instance is not valid, containing a list of validation problems.</exception>
    public static void EnsureValid(this ReportGenerationServiceTests? value)
    {
        ArgumentNullException.ThrowIfNull(value);

        var problems = value.Validate();
        if (problems.Count > 0)
        {
            throw new ArgumentException(
                $"ReportGenerationServiceTests instance is not valid. Problems: {string.Join(", ", problems)}",
                nameof(value));
        }
    }
}
