#nullable enable
using System.Text;
using System.Text.RegularExpressions;
using EfMigrationDiff.Exceptions;

namespace EfMigrationDiff.Configuration;

/// <summary>
/// The effective inputs of a single diff command run, resolved from configuration and command-line arguments.
/// <see cref="Validate"/> reports every violated rule so the CLI can print them all before doing any work.
/// </summary>
public sealed record DiffOptions
{
    /// <summary>
    /// Gets the repository directory the diff runs against. Must exist and be readable.
    /// </summary>
    public string RepositoryPath { get; init; } = string.Empty;

    /// <summary>
    /// Gets the migrations directory, relative to <see cref="RepositoryPath"/> or absolute.
    /// Only checked when <see cref="RequiresMigrationsDirectory"/> is <c>true</c>.
    /// </summary>
    public string MigrationsPath { get; init; } = string.Empty;

    /// <summary>
    /// Gets a value indicating whether the command reads migration files from disk,
    /// in which case <see cref="MigrationsPath"/> must exist and be readable.
    /// </summary>
    public bool RequiresMigrationsDirectory { get; init; }

    /// <summary>
    /// Gets the source branch, or <c>null</c> when the command does not compare branches.
    /// </summary>
    public string? SourceBranch { get; init; }

    /// <summary>
    /// Gets the target branch, or <c>null</c> when the command does not compare branches.
    /// </summary>
    public string? TargetBranch { get; init; }

    /// <summary>
    /// Gets the migration name globs to ignore. Each pattern supports <c>*</c>, <c>?</c> and <c>[...]</c>.
    /// </summary>
    public IReadOnlyList<string> IgnoredMigrations { get; init; } = Array.Empty<string>();

    /// <summary>
    /// Validates the diff inputs and returns every violation found.
    /// </summary>
    /// <returns>A list of human-readable errors, or an empty list when the options are valid.</returns>
    public List<string> Validate()
    {
        var errors = new List<string>();

        ValidateRepositoryPath(errors);

        if (RequiresMigrationsDirectory)
        {
            ValidateMigrationsDirectory(errors);
        }

        ValidateBranches(errors);
        ValidateIgnoredMigrations(errors);

        return errors;
    }

    /// <summary>
    /// Validates the diff inputs and throws if any rule is violated.
    /// </summary>
    /// <exception cref="ConfigurationValidationException">Thrown with all violations when options are invalid.</exception>
    public void ValidateAndThrow()
    {
        var violations = Validate();
        if (violations.Count > 0)
        {
            throw new ConfigurationValidationException(violations);
        }
    }

    private void ValidateRepositoryPath(List<string> errors)
    {
        if (string.IsNullOrWhiteSpace(RepositoryPath))
        {
            errors.Add("RepositoryPath is required.");
            return;
        }

        if (!Directory.Exists(RepositoryPath))
        {
            errors.Add($"Repository directory does not exist: {RepositoryPath}");
        }
        else if (!IsReadableDirectory(RepositoryPath))
        {
            errors.Add($"Repository directory is not readable: {RepositoryPath}");
        }
    }

    private void ValidateMigrationsDirectory(List<string> errors)
    {
        if (string.IsNullOrWhiteSpace(MigrationsPath))
        {
            errors.Add("MigrationsPath is required.");
            return;
        }

        var fullPath = Path.Combine(RepositoryPath, MigrationsPath);

        if (!Directory.Exists(fullPath))
        {
            errors.Add($"Migrations directory does not exist: {fullPath}");
        }
        else if (!IsReadableDirectory(fullPath))
        {
            errors.Add($"Migrations directory is not readable: {fullPath}");
        }
    }

    private void ValidateBranches(List<string> errors)
    {
        if (SourceBranch is not null && string.IsNullOrWhiteSpace(SourceBranch))
        {
            errors.Add("Source branch must not be empty.");
        }

        if (TargetBranch is not null && string.IsNullOrWhiteSpace(TargetBranch))
        {
            errors.Add("Target branch must not be empty.");
        }

        // Branch names are case-sensitive in git, so the comparison is ordinal.
        if (!string.IsNullOrWhiteSpace(SourceBranch)
            && !string.IsNullOrWhiteSpace(TargetBranch)
            && string.Equals(SourceBranch, TargetBranch, StringComparison.Ordinal))
        {
            errors.Add($"Source and target branch must differ, but both are '{SourceBranch}'.");
        }
    }

    private void ValidateIgnoredMigrations(List<string> errors)
    {
        foreach (var pattern in IgnoredMigrations)
        {
            if (string.IsNullOrWhiteSpace(pattern))
            {
                errors.Add("IgnoredMigrations contains an empty pattern.");
                continue;
            }

            try
            {
                // Compiled eagerly so that a broken pattern fails at startup, not in the middle of a diff.
                _ = new Regex(GlobToRegex(pattern), RegexOptions.CultureInvariant);
            }
            catch (ArgumentException ex)
            {
                errors.Add($"IgnoredMigrations pattern '{pattern}' is invalid: {ex.Message}");
            }
        }
    }

    /// <summary>
    /// Converts a migration name glob into an anchored regular expression.
    /// Throws <see cref="ArgumentException"/> when the glob itself is malformed (for example an unclosed <c>[</c>).
    /// </summary>
    private static string GlobToRegex(string glob)
    {
        var pattern = new StringBuilder("^");

        for (var i = 0; i < glob.Length; i++)
        {
            switch (glob[i])
            {
                case '*':
                    pattern.Append(".*");
                    break;

                case '?':
                    pattern.Append('.');
                    break;

                case '[':
                    var close = glob.IndexOf(']', i + 1);
                    if (close < 0)
                    {
                        throw new ArgumentException("unclosed '[' character class");
                    }

                    var body = glob.Substring(i + 1, close - i - 1);
                    if (body.Length == 0)
                    {
                        throw new ArgumentException("empty '[]' character class");
                    }

                    // A leading '!' negates the class, as in shell globs.
                    pattern.Append('[');
                    pattern.Append(body[0] == '!' ? "^" + body[1..] : body);
                    pattern.Append(']');
                    i = close;
                    break;

                default:
                    pattern.Append(Regex.Escape(glob[i].ToString()));
                    break;
            }
        }

        return pattern.Append('$').ToString();
    }

    private static bool IsReadableDirectory(string path)
    {
        try
        {
            using var entries = Directory.EnumerateFileSystemEntries(path).GetEnumerator();
            _ = entries.MoveNext();
            return true;
        }
        catch (Exception ex) when (ex is UnauthorizedAccessException or IOException)
        {
            return false;
        }
    }
}
