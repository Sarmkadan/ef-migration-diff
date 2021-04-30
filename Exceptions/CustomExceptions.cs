#nullable enable
// =============================================================================
// Author: Vladyslav Zaiets | https://sarmkadan.com
// CTO & Software Architect
// =============================================================================

namespace EfMigrationDiff.Exceptions;

/// <summary>
/// Base exception for all application-specific errors.
/// </summary>
public class MigrationDiffException : Exception
{
    public MigrationDiffException(string message) : base(message) { }
    public MigrationDiffException(string message, Exception innerException) : base(message, innerException) { }
}

/// <summary>
/// Thrown when a repository operation fails.
/// </summary>
public class RepositoryException : MigrationDiffException
{
    public RepositoryException(string message) : base(message) { }
    public RepositoryException(string message, Exception innerException) : base(message, innerException) { }
}

/// <summary>
/// Thrown when a migration file cannot be parsed.
/// </summary>
public class MigrationParsingException : MigrationDiffException
{
    public string? FilePath { get; set; }
    public int? LineNumber { get; set; }

    public MigrationParsingException(string message) : base(message) { }
    public MigrationParsingException(string message, string filePath) : base(message)
    {
        FilePath = filePath;
    }
    public MigrationParsingException(string message, string filePath, int lineNumber) : base(message)
    {
        FilePath = filePath;
        LineNumber = lineNumber;
    }
}

/// <summary>
/// Thrown when a git repository operation fails.
/// </summary>
public class GitRepositoryException : MigrationDiffException
{
    public string? RepositoryPath { get; set; }

    public GitRepositoryException(string message) : base(message) { }
    public GitRepositoryException(string message, string repositoryPath) : base(message)
    {
        RepositoryPath = repositoryPath;
    }
    public GitRepositoryException(string message, Exception innerException) : base(message, innerException) { }
}

/// <summary>
/// Thrown when migration conflicts are detected.
/// </summary>
public class MigrationConflictException : MigrationDiffException
{
    public List<string> ConflictingMigrations { get; set; } = [];

    public MigrationConflictException(string message) : base(message) { }
    public MigrationConflictException(string message, List<string> conflicts) : base(message)
    {
        ConflictingMigrations = conflicts;
    }
}

/// <summary>
/// Thrown when a branch cannot be found.
/// </summary>
public class BranchNotFoundException : MigrationDiffException
{
    public string? BranchName { get; set; }

    public BranchNotFoundException(string branchName) : base($"Branch '{branchName}' not found")
    {
        BranchName = branchName;
    }
}

/// <summary>
/// Thrown when a migration is invalid or malformed.
/// </summary>
public class InvalidMigrationException : MigrationDiffException
{
    public string? MigrationId { get; set; }
    public List<string> ValidationErrors { get; set; } = [];

    public InvalidMigrationException(string message) : base(message) { }
    public InvalidMigrationException(string migrationId, List<string> errors) : base($"Migration {migrationId} is invalid")
    {
        MigrationId = migrationId;
        ValidationErrors = errors;
    }
}

/// <summary>
/// Thrown when a DbContext is not found.
/// </summary>
public class DbContextNotFoundException : MigrationDiffException
{
    public string? ContextName { get; set; }

    public DbContextNotFoundException(string contextName) : base($"DbContext '{contextName}' not found")
    {
        ContextName = contextName;
    }
}

/// <summary>
/// Thrown when application configuration is invalid.
/// </summary>
public class ConfigurationException : MigrationDiffException
{
    public List<string> ValidationErrors { get; set; } = [];

    public ConfigurationException(string message) : base(message) { }
    public ConfigurationException(List<string> errors) : base("Configuration validation failed")
    {
        ValidationErrors = errors;
    }
}

/// <summary>
/// Thrown when file operations fail.
/// </summary>
public class FileOperationException : MigrationDiffException
{
    public string? FilePath { get; set; }
    public string? Operation { get; set; }

    public FileOperationException(string message) : base(message) { }
    public FileOperationException(string filePath, string operation) : base($"Failed to {operation} file: {filePath}")
    {
        FilePath = filePath;
        Operation = operation;
    }
    public FileOperationException(string message, Exception innerException) : base(message, innerException) { }
    public FileOperationException(string filePath, string operation, Exception innerException)
        : base($"Failed to {operation} file: {filePath}", innerException)
    {
        FilePath = filePath;
        Operation = operation;
    }
}
