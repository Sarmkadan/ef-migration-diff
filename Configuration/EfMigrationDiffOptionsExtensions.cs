#nullable enable

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace EfMigrationDiff.Configuration;

/// <summary>
/// Extension methods for <see cref="EfMigrationDiffOptions"/> that provide additional utility functionality.
/// </summary>
public static class EfMigrationDiffOptionsExtensions
{
    /// <summary>
    /// Ensures that all required paths exist. Creates directories if they don't exist.
    /// </summary>
    /// <param name="options">The options instance.</param>
    /// <returns>The same options instance for method chaining.</returns>
    public static EfMigrationDiffOptions EnsurePathsExist(this EfMigrationDiffOptions options)
    {
        if (options == null)
        {
            throw new ArgumentNullException(nameof(options));
        }

        // Ensure repository path exists
        if (!string.IsNullOrWhiteSpace(options.RepositoryPath) && !Directory.Exists(options.RepositoryPath))
        {
            Directory.CreateDirectory(options.RepositoryPath);
        }

        // Ensure migrations path exists
        if (!string.IsNullOrWhiteSpace(options.MigrationsPath))
        {
            var fullMigrationsPath = Path.IsPathRooted(options.MigrationsPath)
                ? options.MigrationsPath
                : Path.Combine(options.RepositoryPath, options.MigrationsPath);

            if (!Directory.Exists(fullMigrationsPath))
            {
                Directory.CreateDirectory(fullMigrationsPath);
            }
        }

        // Ensure output path exists
        if (!string.IsNullOrWhiteSpace(options.OutputPath))
        {
            var fullOutputPath = Path.IsPathRooted(options.OutputPath)
                ? options.OutputPath
                : Path.Combine(options.RepositoryPath, options.OutputPath);

            if (!Directory.Exists(fullOutputPath))
            {
                Directory.CreateDirectory(fullOutputPath);
            }
        }

        return options;
    }

    /// <summary>
    /// Configures the options to generate reports in the specified format.
    /// </summary>
    /// <param name="options">The options instance.</param>
    /// <param name="format">The report format (text, json, or html).</param>
    /// <returns>The same options instance for method chaining.</returns>
    public static EfMigrationDiffOptions WithReportFormat(this EfMigrationDiffOptions options, string format)
    {
        if (options == null)
        {
            throw new ArgumentNullException(nameof(options));
        }

        if (string.IsNullOrWhiteSpace(format))
        {
            throw new ArgumentException("Format cannot be null or empty.", nameof(format));
        }

        options.ReportFormat = format.ToLowerInvariant();
        return options;
    }

    /// <summary>
    /// Configures the options to analyze specific DbContexts.
    /// </summary>
    /// <param name="options">The options instance.</param>
    /// <param name="contextNames">The DbContext names to analyze.</param>
    /// <returns>The same options instance for method chaining.</returns>
    public static EfMigrationDiffOptions WithDbContexts(this EfMigrationDiffOptions options, params string[] contextNames)
    {
        if (options == null)
        {
            throw new ArgumentNullException(nameof(options));
        }

        options.DbContextNames = contextNames ?? Array.Empty<string>();
        return options;
    }

    /// <summary>
    /// Configures the options with source and target branch names.
    /// </summary>
    /// <param name="options">The options instance.</param>
    /// <param name="sourceBranch">The source branch name.</param>
    /// <param name="targetBranch">The target branch name.</param>
    /// <returns>The same options instance for method chaining.</returns>
    public static EfMigrationDiffOptions WithBranches(this EfMigrationDiffOptions options, string sourceBranch, string targetBranch)
    {
        if (options == null)
        {
            throw new ArgumentNullException(nameof(options));
        }

        if (string.IsNullOrWhiteSpace(sourceBranch))
        {
            throw new ArgumentException("Source branch cannot be null or empty.", nameof(sourceBranch));
        }

        if (string.IsNullOrWhiteSpace(targetBranch))
        {
            throw new ArgumentException("Target branch cannot be null or empty.", nameof(targetBranch));
        }

        options.SourceBranch = sourceBranch;
        options.TargetBranch = targetBranch;
        return options;
    }
}