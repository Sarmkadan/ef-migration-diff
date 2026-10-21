#nullable enable
namespace EfMigrationDiff.Exceptions;

/// <summary>
/// Base exception for all application-specific errors.
/// </summary>
public class EfMigrationDiffException : Exception
{
    public EfMigrationDiffException(string message) : base(message)
    {
        ArgumentNullException.ThrowIfNull(message);
    }
    public EfMigrationDiffException(string message, Exception innerException) : base(message, innerException)
    {
        ArgumentNullException.ThrowIfNull(message);
        ArgumentNullException.ThrowIfNull(innerException);
    }
}

/// <summary>
/// Thrown when a repository operation fails.
/// </summary>
public class RepositoryException : EfMigrationDiffException
{
    public RepositoryException(string message) : base(message)
    {
        ArgumentNullException.ThrowIfNull(message);
    }
    public RepositoryException(string message, Exception innerException) : base(message, innerException)
    {
        ArgumentNullException.ThrowIfNull(message);
        ArgumentNullException.ThrowIfNull(innerException);
    }
}

/// <summary>
/// Thrown when a migration file cannot be parsed.
/// </summary>
public class MigrationParsingException : EfMigrationDiffException
{
    public string? FilePath { get; set; }
    public int? LineNumber { get; set; }

    public MigrationParsingException(string message) : base(message)
    {
        ArgumentNullException.ThrowIfNull(message);
    }
    public MigrationParsingException(string message, string filePath) : base(message)
    {
        ArgumentNullException.ThrowIfNull(message);
        ArgumentNullException.ThrowIfNull(filePath);
        FilePath = filePath;
    }
    public MigrationParsingException(string message, string filePath, int lineNumber) : base(message)
    {
        ArgumentNullException.ThrowIfNull(message);
        ArgumentNullException.ThrowIfNull(filePath);
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

    public GitRepositoryException(string message) : base(message)
    {
        ArgumentNullException.ThrowIfNull(message);
    }
    public GitRepositoryException(string message, string repositoryPath) : base(message)
    {
        ArgumentNullException.ThrowIfNull(message);
        ArgumentNullException.ThrowIfNull(repositoryPath);
        RepositoryPath = repositoryPath;
    }
    public GitRepositoryException(string message, Exception innerException) : base(message, innerException)
    {
        ArgumentNullException.ThrowIfNull(message);
        ArgumentNullException.ThrowIfNull(innerException);
    }
}

/// <summary>
/// Thrown when migration conflicts are detected.
/// </summary>
public class MigrationConflictException : EfMigrationDiffException
{
    public List<string> ConflictingMigrations { get; set; } = [];

    public MigrationConflictException(string message) : base(message)
    {
        ArgumentNullException.ThrowIfNull(message);
    }
    public MigrationConflictException(string message, List<string> conflicts) : base(message)
    {
        ArgumentNullException.ThrowIfNull(message);
        ArgumentNullException.ThrowIfNull(conflicts);
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
        ArgumentNullException.ThrowIfNull(branchName);
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

    public InvalidMigrationException(string message) : base(message)
    {
        ArgumentNullException.ThrowIfNull(message);
    }
    public InvalidMigrationException(string migrationId, List<string> errors) : base($"Migration {migrationId} is invalid")
    {
        ArgumentNullException.ThrowIfNull(migrationId);
        ArgumentNullException.ThrowIfNull(errors);
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
        ArgumentNullException.ThrowIfNull(contextName);
        ContextName = contextName;
    }
}

/// <summary>
/// Thrown when application configuration is invalid.
/// </summary>
public class ConfigurationException : EfMigrationDiffException
{
    public List<string> ValidationErrors { get; set; } = [];

    public ConfigurationException(string message) : base(message)
    {
        ArgumentNullException.ThrowIfNull(message);
    }
    public ConfigurationException(List<string> errors) : base("Configuration validation failed")
    {
        ArgumentNullException.ThrowIfNull(errors);
        ValidationErrors = errors;
    }
}

/// <summary>
/// Thrown when one or more configuration values violate their rules.
/// All violations are collected and reported together in a single message.
/// </summary>
public class ConfigurationValidationException : EfMigrationDiffException
{
    public IReadOnlyList<string> Violations { get; }

    public ConfigurationValidationException(IReadOnlyList<string> violations) : base(FormatMessage(violations))
    {
        Violations = violations;
    }

    private static string FormatMessage(IReadOnlyList<string> violations)
    {
        ArgumentNullException.ThrowIfNull(violations);
        var lines = violations.Select(v => $"  - {v}");
        return $"Invalid configuration ({violations.Count} error(s)):{Environment.NewLine}{string.Join(Environment.NewLine, lines)}";
    }
}

/// <summary>
/// Thrown when validation fails.
/// </summary>
public class ValidationException : EfMigrationDiffException
{
    public List<string> ValidationErrors { get; set; } = [];

    public ValidationException(string message) : base(message)
    {
        ArgumentNullException.ThrowIfNull(message);
    }
    public ValidationException(string message, List<string> errors) : base(message)
    {
        ArgumentNullException.ThrowIfNull(message);
        ArgumentNullException.ThrowIfNull(errors);
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

    public FileOperationException(string message) : base(message)
    {
        ArgumentNullException.ThrowIfNull(message);
    }
    public FileOperationException(string filePath, string operation) : base($"Failed to {operation} file: {filePath}")
    {
        ArgumentNullException.ThrowIfNull(filePath);
        ArgumentNullException.ThrowIfNull(operation);
        FilePath = filePath;
        Operation = operation;
    }
    public FileOperationException(string message, Exception innerException) : base(message, innerException)
    {
        ArgumentNullException.ThrowIfNull(message);
        ArgumentNullException.ThrowIfNull(innerException);
    }
    public FileOperationException(string filePath, string operation, Exception innerException)
        : base($"Failed to {operation} file: {filePath}", innerException)
    {
        ArgumentNullException.ThrowIfNull(filePath);
        ArgumentNullException.ThrowIfNull(operation);
        ArgumentNullException.ThrowIfNull(innerException);
        FilePath = filePath;
        Operation = operation;
    }
}

/// <summary>
/// Thrown when configuration file operations fail.
/// </summary>
public class ConfigFileException : EfMigrationDiffException
{
    public string? FilePath { get; set; }

    public ConfigFileException(string message) : base(message)
    {
        ArgumentNullException.ThrowIfNull(message);
    }
    public ConfigFileException(string message, Exception innerException) : base(message, innerException)
    {
        ArgumentNullException.ThrowIfNull(message);
        ArgumentNullException.ThrowIfNull(innerException);
    }
    public ConfigFileException(string filePath, string message) : base($"{message} (file: {filePath})")
    {
        ArgumentNullException.ThrowIfNull(filePath);
        ArgumentNullException.ThrowIfNull(message);
        FilePath = filePath;
    }
}
