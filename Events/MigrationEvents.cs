#nullable enable
// =============================================================================
// Author: Vladyslav Zaiets | https://sarmkadan.com
// CTO & Software Architect
// =============================================================================

using EfMigrationDiff.Models;

namespace EfMigrationDiff.Events;

/// <summary>
/// Event published when migration comparison starts.
/// </summary>
public class MigrationComparisonStartedEvent : EventBase
{
    public string SourceBranch { get; set; } = string.Empty;
    public string TargetBranch { get; set; } = string.Empty;
    public string RepositoryPath { get; set; } = string.Empty;
}

/// <summary>
/// Event published when migration comparison completes.
/// </summary>
public class MigrationComparisonCompletedEvent : EventBase
{
    public string SourceBranch { get; set; } = string.Empty;
    public string TargetBranch { get; set; } = string.Empty;
    public MigrationDiff? DiffResult { get; set; }
    public TimeSpan ExecutionTime { get; set; }
    public bool HasConflicts { get; set; }
}

/// <summary>
/// Event published when migration validation starts.
/// </summary>
public class MigrationValidationStartedEvent : EventBase
{
    public string MigrationsPath { get; set; } = string.Empty;
    public int FileCount { get; set; }
}

/// <summary>
/// Event published when migration validation completes.
/// </summary>
public class MigrationValidationCompletedEvent : EventBase
{
    public int ValidFiles { get; set; }
    public int InvalidFiles { get; set; }
    public List<string> Errors { get; set; } = new();
    public TimeSpan ExecutionTime { get; set; }
}

/// <summary>
/// Event published when a conflict is detected in migrations.
/// </summary>
public class MigrationConflictDetectedEvent : EventBase
{
    public ConflictInfo? Conflict { get; set; }
    public string SourceFile { get; set; } = string.Empty;
    public string TargetFile { get; set; } = string.Empty;
}

/// <summary>
/// Event published when schema changes are detected.
/// </summary>
public class SchemaChangeDetectedEvent : EventBase
{
    public SchemaChange? Change { get; set; }
    public string MigrationName { get; set; } = string.Empty;
    public string ChangeType { get; set; } = string.Empty;
}

/// <summary>
/// Event published when a report is generated.
/// </summary>
public class ReportGeneratedEvent : EventBase
{
    public string ReportPath { get; set; } = string.Empty;
    public string ReportFormat { get; set; } = string.Empty;
    public long FileSizeBytes { get; set; }
    public string Summary { get; set; } = string.Empty;
}

/// <summary>
/// Event published when an error occurs during migration processing.
/// </summary>
public class MigrationProcessingErrorEvent : EventBase
{
    public string ErrorMessage { get; set; } = string.Empty;
    public string? StackTrace { get; set; }
    public string Operation { get; set; } = string.Empty;
}
