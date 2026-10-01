#nullable enable

using EfMigrationDiff.Models;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using EfMigrationDiff.Configuration;

namespace EfMigrationDiff.Services;

/// <summary>
/// Generates SQL migration scripts from schema change operations.
/// </summary>
public class SqlGenerator : IDisposable
{
    private readonly ILogger<SqlGenerator> _logger;
    private readonly EfMigrationDiffOptions _options;
    private readonly StringWriter _buffer;
    private bool _disposed;

    public SqlGenerator(
        ILogger<SqlGenerator> logger,
        IOptions<EfMigrationDiffOptions> options)
    {
        ArgumentNullException.ThrowIfNull(logger);
        ArgumentNullException.ThrowIfNull(options);
        _logger = logger;
        _options = options.Value;
        _buffer = new StringWriter();
    }

    /// <summary>
    /// Generates a complete SQL script from a list of schema changes.
    /// </summary>
    /// <param name="changes">The schema changes to convert into SQL.</param>
    /// <param name="includeTransaction">Whether to wrap the output in a transaction block.</param>
    /// <returns>A SQL script string representing all changes.</returns>
    public string GenerateScript(IReadOnlyList<SchemaChange> changes, bool includeTransaction = true)
    {
        ArgumentNullException.ThrowIfNull(changes);
        _logger.LogInformation("Generating SQL script for {Count} schema changes", changes.Count);

        _buffer.GetStringBuilder().Clear();

        if (includeTransaction)
            _buffer.WriteLine("BEGIN TRANSACTION;");

        foreach (var change in changes)
        {
            var sql = GenerateStatement(change);
            if (!string.IsNullOrWhiteSpace(sql))
            {
                _buffer.WriteLine();
                _buffer.WriteLine($"-- {change.ChangeType}: {change.TableName}");
                _buffer.WriteLine(sql);
            }
        }

        if (includeTransaction)
        {
            _buffer.WriteLine();
            _buffer.WriteLine("COMMIT;");
        }

        return _buffer.ToString();
    }

    /// <summary>
    /// Generates a single SQL statement for one schema change operation.
    /// </summary>
    /// <param name="change">The schema change to convert.</param>
    /// <returns>The SQL statement, or an empty string if the change type is unsupported.</returns>
    public string GenerateStatement(SchemaChange change)
    {
        ArgumentNullException.ThrowIfNull(change);

        return change.ChangeType switch
        {
            SqlChangeType.CreateTable => $"CREATE TABLE [{change.TableName}] (\n    {change.Sql}\n);",
            SqlChangeType.DropTable => $"DROP TABLE [{change.TableName}];",
            SqlChangeType.AddColumn => $"ALTER TABLE [{change.TableName}] ADD [{change.ColumnName}] {change.NewValue ?? "NVARCHAR(MAX)"}{(change.DefaultValue is not null ? $" DEFAULT {change.DefaultValue}" : "")};",
            SqlChangeType.DropColumn => $"ALTER TABLE [{change.TableName}] DROP COLUMN [{change.ColumnName}];",
            SqlChangeType.ModifyColumn => $"ALTER TABLE [{change.TableName}] ALTER COLUMN [{change.ColumnName}] {change.NewValue};",
            SqlChangeType.CreateIndex => $"CREATE INDEX {change.Sql};",
            SqlChangeType.DropIndex => $"DROP INDEX {change.Sql};",
            SqlChangeType.Rename => $"EXEC sp_rename '{change.OldValue}', '{change.NewValue}';",
            _ => !string.IsNullOrEmpty(change.Sql) ? $"{change.Sql};" : string.Empty
        };
    }

    /// <summary>
    /// Generates a rollback script that reverses the given schema changes.
    /// </summary>
    /// <param name="changes">The changes to reverse.</param>
    /// <returns>A SQL script that undoes the provided changes in reverse order.</returns>
    public string GenerateRollback(IReadOnlyList<SchemaChange> changes)
    {
        ArgumentNullException.ThrowIfNull(changes);
        _logger.LogInformation("Generating rollback script for {Count} changes", changes.Count);

        var reversed = changes.Reverse().Select(InvertChange).ToList();
        return GenerateScript(reversed, includeTransaction: true);
    }

    /// <summary>
    /// Validates that all schema changes can be converted to SQL without errors.
    /// </summary>
    /// <param name="changes">The changes to validate.</param>
    /// <returns>A list of validation error messages; empty if all changes are valid.</returns>
    public IReadOnlyList<string> ValidateChanges(IReadOnlyList<SchemaChange> changes)
    {
        ArgumentNullException.ThrowIfNull(changes);
        var errors = new List<string>();

        for (var i = 0; i < changes.Count; i++)
        {
            var change = changes[i];
            if (string.IsNullOrWhiteSpace(change.TableName) && change.ChangeType != SqlChangeType.Unknown)
                errors.Add($"Change [{i}]: missing TableName for {change.ChangeType}");

            if (change.ChangeType is SqlChangeType.AddColumn or SqlChangeType.DropColumn or SqlChangeType.ModifyColumn
                && string.IsNullOrWhiteSpace(change.ColumnName))
                errors.Add($"Change [{i}]: missing ColumnName for {change.ChangeType} on {change.TableName}");
        }

        return errors;
    }

    private static SchemaChange InvertChange(SchemaChange original)
    {
        var inverted = new SchemaChange
        {
            Id = Guid.NewGuid().ToString(),
            MigrationId = original.MigrationId,
            TableName = original.TableName,
            ColumnName = original.ColumnName,
            Sql = original.Sql,
            OldValue = original.NewValue,
            NewValue = original.OldValue,
            Metadata = original.Metadata
        };

        inverted.ChangeType = original.ChangeType switch
        {
            SqlChangeType.CreateTable => SqlChangeType.DropTable,
            SqlChangeType.DropTable => SqlChangeType.CreateTable,
            SqlChangeType.AddColumn => SqlChangeType.DropColumn,
            SqlChangeType.DropColumn => SqlChangeType.AddColumn,
            SqlChangeType.CreateIndex => SqlChangeType.DropIndex,
            SqlChangeType.DropIndex => SqlChangeType.CreateIndex,
            _ => original.ChangeType
        };

        return inverted;
    }

    public void Dispose()
    {
        if (!_disposed)
        {
            _buffer.Dispose();
            _disposed = true;
        }
        GC.SuppressFinalize(this);
    }
}
