#nullable enable
using EfMigrationDiff.Models;
using EfMigrationDiff.Exceptions;
using System.Text.RegularExpressions;

namespace EfMigrationDiff.Services;

/// <summary>
/// Service for parsing EF migration files and extracting metadata.
/// </summary>
public class MigrationParserService
{
    /// <summary>
    /// Parses a migration file and creates a Migration object.
    /// </summary>
    public Migration? ParseMigrationFile(MigrationFile migrationFile)
    {
        ArgumentNullException.ThrowIfNull(migrationFile);

        if (!migrationFile.IsValid())
            return null;

        var id = ExtractMigrationId(migrationFile.FileName);
        if (string.IsNullOrEmpty(id))
            return null;

        var name = ExtractMigrationName(migrationFile.FileName);
        var dbContextName = migrationFile.DbContextName;

        var migration = new Migration(id, name, dbContextName)
        {
            Content = migrationFile.Content
        };

        ExtractMetadata(migrationFile.Content, migration);

        return migration;
    }

    /// <summary>
    /// Extracts the migration ID (timestamp) from the filename.
    /// </summary>
    private string? ExtractMigrationId(string fileName)
    {
        var baseName = Path.GetFileNameWithoutExtension(fileName);
        if (baseName.EndsWith(".Designer"))
        {
            baseName = baseName[..^".Designer".Length];
        }

        var parts = baseName.Split('_');
        if (parts.Length > 0 && parts[0].Length == 14 && parts[0].All(char.IsDigit))
        {
            return parts[0];
        }

        return null;
    }

    /// <summary>
    /// Extracts the migration name from the filename.
    /// </summary>
    private string ExtractMigrationName(string fileName)
    {
        var baseName = Path.GetFileNameWithoutExtension(fileName);
        if (baseName.EndsWith(".Designer"))
        {
            baseName = baseName[..^".Designer".Length];
        }

        var parts = baseName.Split('_');
        if (parts.Length > 1)
        {
            return string.Join("_", parts.Skip(1));
        }

        return baseName;
    }

    /// <summary>
    /// Extracts metadata from the migration content.
    /// </summary>
    private void ExtractMetadata(string content, Migration migration)
    {
        // Extract class name
        var classMatch = Regex.Match(content, @"public\s+partial\s+class\s+(\w+)\s*:", RegexOptions.IgnoreCase);
        if (classMatch.Success)
        {
            migration.MetadataContent += $"ClassName: {classMatch.Groups[1].Value}\n";
        }

        // Extract timestamp
        var timestampMatch = Regex.Match(content, @"name:\s*""(\d{14})""", RegexOptions.IgnoreCase);
        if (timestampMatch.Success)
        {
            migration.Timestamp = timestampMatch.Groups[1].Value;
        }

        // Check if it's a designer file
        if (content.Contains("partial class"))
        {
            migration.MetadataContent += "IsDesigner: true\n";
        }

        // Extract custom SQL operations count
        var sqlOpsCount = Regex.Matches(content, @"migrationBuilder\.Sql\s*\(", RegexOptions.IgnoreCase).Count;
        migration.MetadataContent += $"SqlOperationsCount: {sqlOpsCount}\n";

        // Extract comments
        var commentMatches = Regex.Matches(content, @"//\s*(.+)", RegexOptions.IgnoreCase);
        if (commentMatches.Count > 0)
        {
            migration.MetadataContent += $"Comments: {commentMatches.Count}\n";
        }
    }

    /// <summary>
    /// Parses multiple migration files and returns Migration objects.
    /// </summary>
    public List<Migration> ParseMigrationFiles(List<MigrationFile> migrationFiles)
    {
        var migrations = new List<Migration>();

        foreach (var file in migrationFiles)
        {
            var migration = ParseMigrationFile(file);
            if (migration is not null)
            {
                migrations.Add(migration);
            }
        }

        return migrations;
    }

    /// <summary>
    /// Loads migrations from a directory.
    /// </summary>
    public async Task<List<Migration>> LoadMigrationsFromDirectoryAsync(string directoryPath, string dbContextName)
    {
        ArgumentException.ThrowIfNullOrEmpty(directoryPath);
        ArgumentException.ThrowIfNullOrEmpty(dbContextName);

        var migrations = new List<Migration>();

        if (!Directory.Exists(directoryPath))
            throw new FileOperationException(directoryPath, "read directory");

        var migrationFiles = Directory.GetFiles(directoryPath, "*.cs")
                                      .Where(f => !f.EndsWith(".Designer.cs"))
                                      .ToList();

        foreach (var filePath in migrationFiles)
        {
            var migrationFile = new MigrationFile(filePath, dbContextName);
            await migrationFile.LoadContentAsync();

            var migration = ParseMigrationFile(migrationFile);
            if (migration is not null)
            {
                migrations.Add(migration);
            }
        }

        return migrations.OrderBy(m => m.Timestamp).ToList();
    }

    /// <summary>
    /// Validates migration structure.
    /// </summary>
    public List<string> ValidateMigrationFile(MigrationFile migrationFile)
    {
        var errors = new List<string>();

        if (string.IsNullOrEmpty(migrationFile.Content))
        {
            errors.Add("Migration content is empty");
            return errors;
        }

        if (!migrationFile.Content.Contains("public partial class"))
        {
            errors.Add("Missing 'public partial class' declaration");
        }

        if (!migrationFile.Content.Contains("Down("))
        {
            errors.Add("Missing Down method");
        }

        if (!migrationFile.Content.Contains("Up("))
        {
            errors.Add("Missing Up method");
        }

        var classMatches = Regex.Matches(migrationFile.Content, @"public\s+partial\s+class\s+\w+\s*:", RegexOptions.IgnoreCase);
        if (classMatches.Count != 1)
        {
            errors.Add("Should contain exactly one public partial class");
        }

        if (!Regex.IsMatch(migrationFile.FileName, @"\d{14}_\w+\.cs"))
        {
            errors.Add("File name format should be: YYYYMMDDHHmmss_MigrationName.cs");
        }

        return errors;
    }

    /// <summary>
    /// Gets migration dependencies from the content.
    /// </summary>
    public List<string> GetMigrationDependencies(Migration migration)
    {
        var dependencies = new List<string>();

        // Look for dependency declaration in the migration
        var depMatch = Regex.Match(migration.Content, @"\.Annotation\s*\(\s*""([^""]+)""\s*,\s*""([^""]+)""", RegexOptions.IgnoreCase);

        if (depMatch.Success)
        {
            dependencies.Add(depMatch.Groups[2].Value);
        }

        return dependencies;
    }

    /// <summary>
    /// Compares two migration files for differences.
    /// </summary>
    public Dictionary<string, object> CompareMigrations(Migration migration1, Migration migration2)
    {
        var comparison = new Dictionary<string, object>
        {
            { "AreIdentical", migration1.Content == migration2.Content },
            { "SizeRatio", migration1.GetContentSize() / (double)(migration2.GetContentSize() + 1) },
            { "StatementDifference", Math.Abs(migration1.CountStatements() - migration2.CountStatements()) },
            { "SameName", migration1.Name == migration2.Name },
            { "SameDbContext", migration1.DbContextName == migration2.DbContextName }
        };

        return comparison;
    }

    /// <summary>
    /// Extracts SQL operations from migration Up method.
    /// </summary>
    public List<string> ExtractSqlOperations(Migration migration)
    {
        var operations = new List<string>();

        var upMethodMatch = Regex.Match(migration.Content, @"protected\s+override\s+void\s+Up\s*\(MigrationBuilder\s+migrationBuilder\)\s*\{(.*?)\}", RegexOptions.Singleline | RegexOptions.IgnoreCase);

        if (upMethodMatch.Success)
        {
            var upMethodContent = upMethodMatch.Groups[1].Value;
            var operationMatches = Regex.Matches(upMethodContent, @"migrationBuilder\.\w+\s*\([^)]*\)");

            foreach (Match opMatch in operationMatches)
            {
                operations.Add(opMatch.Value);
            }
        }

        return operations;
    }

    /// <summary>
    /// Gets the sequence number for a migration based on timestamp.
    /// </summary>
    public int GetMigrationSequence(string migrationId)
    {
        if (string.IsNullOrEmpty(migrationId) || migrationId.Length < 14)
            return 0;

        if (long.TryParse(migrationId[..14], System.Globalization.NumberStyles.None, System.Globalization.CultureInfo.InvariantCulture, out var timestamp))
        {
            return (int)(timestamp % int.MaxValue);
        }

        return 0;
    }
}
