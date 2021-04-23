// =============================================================================
// Author: Vladyslav Zaiets | https://sarmkadan.com
// CTO & Software Architect
// =============================================================================

namespace EfMigrationDiff.Models;

/// <summary>
/// Represents a detected conflict between two migrations.
/// </summary>
public class ConflictInfo
{
    public string Id { get; set; } = string.Empty;
    public string FirstMigrationId { get; set; } = string.Empty;
    public string SecondMigrationId { get; set; } = string.Empty;
    public ConflictType ConflictType { get; set; }
    public ConflictSeverity Severity { get; set; }
    public string Description { get; set; } = string.Empty;
    public List<string> AffectedElements { get; set; } = [];
    public Dictionary<string, string> Details { get; set; } = [];
    public DateTime DetectedAt { get; set; }
    public bool IsResolved { get; set; }
    public string? ResolutionStrategy { get; set; }

    public ConflictInfo()
    {
    }

    public ConflictInfo(string firstMigrationId, string secondMigrationId, ConflictType conflictType)
    {
        Id = Guid.NewGuid().ToString();
        FirstMigrationId = firstMigrationId;
        SecondMigrationId = secondMigrationId;
        ConflictType = conflictType;
        DetectedAt = DateTime.UtcNow;
        Severity = ConflictSeverity.Error;
    }

    /// <summary>
    /// Validates the conflict has required properties.
    /// </summary>
    public bool IsValid()
    {
        return !string.IsNullOrWhiteSpace(FirstMigrationId) &&
               !string.IsNullOrWhiteSpace(SecondMigrationId) &&
               ConflictType != ConflictType.None &&
               !string.IsNullOrWhiteSpace(Description);
    }

    /// <summary>
    /// Gets a human-readable title for the conflict.
    /// </summary>
    public string GetTitle()
    {
        return ConflictType switch
        {
            ConflictType.TableConflict => "Table Schema Conflict",
            ConflictType.ColumnConflict => "Column Definition Conflict",
            ConflictType.IndexConflict => "Index Conflict",
            ConflictType.ConstraintConflict => "Constraint Conflict",
            ConflictType.OperationConflict => "Operation Order Conflict",
            ConflictType.DependencyConflict => "Dependency Conflict",
            ConflictType.NameConflict => "Naming Conflict",
            _ => "Unknown Conflict"
        };
    }

    /// <summary>
    /// Adds an affected element to track what this conflict impacts.
    /// </summary>
    public void AddAffectedElement(string elementName)
    {
        if (!string.IsNullOrWhiteSpace(elementName) && !AffectedElements.Contains(elementName))
        {
            AffectedElements.Add(elementName);
        }
    }

    /// <summary>
    /// Adds a detail key-value pair for context about the conflict.
    /// </summary>
    public void AddDetail(string key, string value)
    {
        Details[key] = value;
    }

    /// <summary>
    /// Gets detail value by key, returns empty string if not found.
    /// </summary>
    public string GetDetail(string key)
    {
        return Details.TryGetValue(key, out var value) ? value : string.Empty;
    }

    /// <summary>
    /// Marks this conflict as resolved with a specific strategy.
    /// </summary>
    public void MarkResolved(string strategy)
    {
        IsResolved = true;
        ResolutionStrategy = strategy;
    }

    /// <summary>
    /// Determines if this conflict is critical and blocks deployment.
    /// </summary>
    public bool IsBlocking()
    {
        return !IsResolved && Severity is ConflictSeverity.Critical or ConflictSeverity.Error;
    }

    /// <summary>
    /// Checks if this conflict involves a specific migration.
    /// </summary>
    public bool InvolvesMigration(string migrationId)
    {
        return FirstMigrationId == migrationId || SecondMigrationId == migrationId;
    }

    /// <summary>
    /// Gets the other migration involved in this conflict.
    /// </summary>
    public string GetOtherMigration(string migrationId)
    {
        if (FirstMigrationId == migrationId)
            return SecondMigrationId;

        if (SecondMigrationId == migrationId)
            return FirstMigrationId;

        return string.Empty;
    }

    /// <summary>
    /// Compares severity with another conflict.
    /// </summary>
    public int CompareSeverityWith(ConflictInfo other)
    {
        return Severity.CompareTo(other.Severity);
    }

    public override string ToString()
    {
        return $"[{Severity}] {GetTitle()} between {FirstMigrationId} and {SecondMigrationId}";
    }
}
