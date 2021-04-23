// =============================================================================
// Author: Vladyslav Zaiets | https://sarmkadan.com
// CTO & Software Architect
// =============================================================================

using System.Text.RegularExpressions;

namespace EfMigrationDiff.Utilities;

/// <summary>
/// Helper class for validation operations and input sanitization.
/// </summary>
public static class ValidationHelper
{
    /// <summary>
    /// Validates a migration timestamp format.
    /// </summary>
    public static bool IsValidMigrationTimestamp(string timestamp)
    {
        if (string.IsNullOrWhiteSpace(timestamp))
            return false;

        if (timestamp.Length != Constants.Migration.TimestampLength)
            return false;

        return Regex.IsMatch(timestamp, @"^\d{14}$");
    }

    /// <summary>
    /// Validates a migration ID format.
    /// </summary>
    public static bool IsValidMigrationId(string migrationId)
    {
        if (string.IsNullOrWhiteSpace(migrationId))
            return false;

        return Regex.IsMatch(migrationId, Constants.Patterns.MigrationIdPattern);
    }

    /// <summary>
    /// Validates a table name.
    /// </summary>
    public static bool IsValidTableName(string tableName)
    {
        if (string.IsNullOrWhiteSpace(tableName))
            return false;

        if (tableName.Length > Constants.Migration.MaxTableNameLength)
            return false;

        // Allow alphanumeric, underscore, and brackets for schema
        return Regex.IsMatch(tableName, @"^[\[\]a-zA-Z0-9_\.]+$");
    }

    /// <summary>
    /// Validates a column name.
    /// </summary>
    public static bool IsValidColumnName(string columnName)
    {
        if (string.IsNullOrWhiteSpace(columnName))
            return false;

        if (columnName.Length > Constants.Migration.MaxColumnNameLength)
            return false;

        return Regex.IsMatch(columnName, @"^[a-zA-Z_][a-zA-Z0-9_]*$");
    }

    /// <summary>
    /// Validates an index name.
    /// </summary>
    public static bool IsValidIndexName(string indexName)
    {
        if (string.IsNullOrWhiteSpace(indexName))
            return false;

        if (indexName.Length > Constants.Migration.MaxIndexNameLength)
            return false;

        return Regex.IsMatch(indexName, @"^[a-zA-Z_][a-zA-Z0-9_]*$");
    }

    /// <summary>
    /// Validates a migration name.
    /// </summary>
    public static bool IsValidMigrationName(string migrationName)
    {
        if (string.IsNullOrWhiteSpace(migrationName))
            return false;

        if (migrationName.Length > Constants.Migration.MaxNameLength)
            return false;

        return Regex.IsMatch(migrationName, @"^[a-zA-Z][a-zA-Z0-9]*$");
    }

    /// <summary>
    /// Validates a branch name.
    /// </summary>
    public static bool IsValidBranchName(string branchName)
    {
        if (string.IsNullOrWhiteSpace(branchName))
            return false;

        // Git branch name validation
        return !Regex.IsMatch(branchName, @"[\s~\^:\?*\[\]\\]");
    }

    /// <summary>
    /// Validates a file path.
    /// </summary>
    public static bool IsValidFilePath(string filePath)
    {
        if (string.IsNullOrWhiteSpace(filePath))
            return false;

        try
        {
            var fullPath = Path.GetFullPath(filePath);
            return true;
        }
        catch
        {
            return false;
        }
    }

    /// <summary>
    /// Validates a directory path.
    /// </summary>
    public static bool IsValidDirectoryPath(string directoryPath)
    {
        if (string.IsNullOrWhiteSpace(directoryPath))
            return false;

        try
        {
            var fullPath = Path.GetFullPath(directoryPath);
            return Directory.Exists(fullPath);
        }
        catch
        {
            return false;
        }
    }

    /// <summary>
    /// Validates a git commit SHA.
    /// </summary>
    public static bool IsValidCommitSha(string commitSha)
    {
        if (string.IsNullOrWhiteSpace(commitSha))
            return false;

        // Git SHA can be 40 (SHA-1) or 64 (SHA-256) characters
        return Regex.IsMatch(commitSha, @"^[a-fA-F0-9]{40}$|^[a-fA-F0-9]{64}$");
    }

    /// <summary>
    /// Sanitizes a string to remove potentially harmful characters.
    /// </summary>
    public static string SanitizeInput(string input)
    {
        if (string.IsNullOrEmpty(input))
            return string.Empty;

        // Remove potential SQL injection characters
        return Regex.Replace(input, @"[;'""\\]", "");
    }

    /// <summary>
    /// Validates a date string format.
    /// </summary>
    public static bool IsValidDateFormat(string dateString, string format = "yyyy-MM-dd")
    {
        if (string.IsNullOrWhiteSpace(dateString))
            return false;

        return DateTime.TryParseExact(dateString, format, System.Globalization.CultureInfo.InvariantCulture, System.Globalization.DateTimeStyles.None, out _);
    }

    /// <summary>
    /// Validates a DbContext name.
    /// </summary>
    public static bool IsValidDbContextName(string contextName)
    {
        if (string.IsNullOrWhiteSpace(contextName))
            return false;

        // Must be a valid C# identifier
        return Regex.IsMatch(contextName, @"^[a-zA-Z_][a-zA-Z0-9_]*$") &&
               contextName.Length <= 255;
    }

    /// <summary>
    /// Validates an email address (basic validation).
    /// </summary>
    public static bool IsValidEmail(string email)
    {
        if (string.IsNullOrWhiteSpace(email))
            return false;

        return Regex.IsMatch(email, @"^[^@\s]+@[^@\s]+\.[^@\s]+$");
    }

    /// <summary>
    /// Checks if a string is a reserved SQL keyword.
    /// </summary>
    public static bool IsReservedKeyword(string text)
    {
        var reservedKeywords = new[]
        {
            "SELECT", "INSERT", "UPDATE", "DELETE", "CREATE", "DROP",
            "ALTER", "TABLE", "VIEW", "INDEX", "PROCEDURE", "FUNCTION"
        };

        return reservedKeywords.Contains(text.ToUpperInvariant());
    }

    /// <summary>
    /// Validates the length of a string.
    /// </summary>
    public static bool IsValidLength(string text, int minLength = 1, int maxLength = int.MaxValue)
    {
        if (text == null)
            return minLength == 0;

        return text.Length >= minLength && text.Length <= maxLength;
    }

    /// <summary>
    /// Checks if a string contains only alphanumeric characters.
    /// </summary>
    public static bool IsAlphanumeric(string text)
    {
        if (string.IsNullOrWhiteSpace(text))
            return false;

        return Regex.IsMatch(text, @"^[a-zA-Z0-9]+$");
    }

    /// <summary>
    /// Checks if a string contains only numeric characters.
    /// </summary>
    public static bool IsNumeric(string text)
    {
        if (string.IsNullOrWhiteSpace(text))
            return false;

        return Regex.IsMatch(text, @"^\d+$");
    }
}
