#nullable enable
namespace EfMigrationDiff.Exceptions;

/// <summary>
/// Base exception for all application-specific errors.
/// </summary>
public class EfMigrationDiffException : Exception
{
    public EfMigrationDiffException(string message) : base(message) { }
    public EfMigrationDiffException(string message, Exception innerException) : base(message, innerException) { }
}

/// <summary>
/// Thrown when a repository operation fails.
/// </summary>
public class RepositoryException : EfMigrationDiffException
{
    public RepositoryException(string message) : base(message) { }
    public RepositoryException(string message, Exception innerException) : base(message, innerException) { }
}

/// <summary>
/// Thrown when a migration file cannot be parsed.
/// </summary>
public class MigrationParsingException : EfMigrationDiffException
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
public class GitRepositoryException : EfMigrationDiffException
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
public class MigrationConflictException : EfMigrationDiffException
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
public class BranchNotFoundException : EfMigrationDiffException
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
public class InvalidMigrationException : EfMigrationDiffException
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
public class DbContextNotFoundException : EfMigrationDiffException
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
public class ConfigurationException : EfMigrationDiffException
{
    public List<string> ValidationErrors { get; set; } = [];

    public ConfigurationException(string message) : base(message) { }
    public ConfigurationException(List<string> errors) : base("Configuration validation failed")
    {
        ValidationErrors = errors;
    }
}

/// <summary>
/// Thrown when validation fails.
/// </summary>
public class ValidationException : EfMigrationDiffException
{
    public List<string> ValidationErrors { get; set; } = [];

    public ValidationException(string message) : base(message) { }
    public ValidationException(string message, List<string> errors) : base(message)
    {
        ValidationErrors = errors;
    }
}

/// <summary>
/// Thrown when file operations fail.
/// </summary>
public class FileOperationException : EfMigrationDiffException
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
