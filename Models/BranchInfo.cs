// =============================================================================
// Author: Vladyslav Zaiets | https://sarmkadan.com
// CTO & Software Architect
// =============================================================================

namespace EfMigrationDiff.Models;

/// <summary>
/// Represents information about a git branch and its migrations.
/// </summary>
public class BranchInfo
{
    public string Id { get; set; } = string.Empty;
    public string BranchName { get; set; } = string.Empty;
    public string CommitHash { get; set; } = string.Empty;
    public string CommitMessage { get; set; } = string.Empty;
    public DateTime CommitDate { get; set; }
    public string Author { get; set; } = string.Empty;
    public List<string> MigrationIds { get; set; } = [];
    public List<string> DbContexts { get; set; } = [];
    public string MigrationsPath { get; set; } = string.Empty;
    public bool IsRemote { get; set; }

    public BranchInfo()
    {
    }

    public BranchInfo(string branchName, string commitHash)
    {
        Id = Guid.NewGuid().ToString();
        BranchName = branchName;
        CommitHash = commitHash;
        CommitDate = DateTime.UtcNow;
    }

    /// <summary>
    /// Validates the branch info has required properties.
    /// </summary>
    public bool IsValid()
    {
        return !string.IsNullOrWhiteSpace(BranchName) &&
               !string.IsNullOrWhiteSpace(CommitHash);
    }

    /// <summary>
    /// Adds a migration ID to this branch's migrations.
    /// </summary>
    public void AddMigration(string migrationId)
    {
        if (!string.IsNullOrWhiteSpace(migrationId) && !MigrationIds.Contains(migrationId))
        {
            MigrationIds.Add(migrationId);
        }
    }

    /// <summary>
    /// Adds a DbContext to this branch's contexts.
    /// </summary>
    public void AddDbContext(string contextName)
    {
        if (!string.IsNullOrWhiteSpace(contextName) && !DbContexts.Contains(contextName))
        {
            DbContexts.Add(contextName);
        }
    }

    /// <summary>
    /// Gets the number of migrations in this branch.
    /// </summary>
    public int GetMigrationCount()
    {
        return MigrationIds.Count;
    }

    /// <summary>
    /// Gets the number of DbContexts in this branch.
    /// </summary>
    public int GetDbContextCount()
    {
        return DbContexts.Count;
    }

    /// <summary>
    /// Checks if this branch has a specific migration.
    /// </summary>
    public bool HasMigration(string migrationId)
    {
        return MigrationIds.Contains(migrationId);
    }

    /// <summary>
    /// Checks if this branch has a specific DbContext.
    /// </summary>
    public bool HasDbContext(string contextName)
    {
        return DbContexts.Contains(contextName);
    }

    /// <summary>
    /// Gets the short commit hash (first 7 characters).
    /// </summary>
    public string GetShortCommitHash()
    {
        return CommitHash.Length > 7 ? CommitHash[..7] : CommitHash;
    }

    /// <summary>
    /// Checks if this is the main/master branch.
    /// </summary>
    public bool IsMainBranch()
    {
        var lowerName = BranchName.ToLowerInvariant();
        return lowerName is "main" or "master" or "develop" or "development";
    }

    /// <summary>
    /// Gets a display-friendly branch name.
    /// </summary>
    public string GetDisplayName()
    {
        if (IsRemote && BranchName.StartsWith("origin/"))
            return BranchName["origin/".Length..];

        return BranchName;
    }

    /// <summary>
    /// Clears all migration data, keeping branch metadata.
    /// </summary>
    public void ClearMigrations()
    {
        MigrationIds.Clear();
    }

    /// <summary>
    /// Gets a summary of the branch.
    /// </summary>
    public string GetSummary()
    {
        return $"{GetDisplayName()} ({GetShortCommitHash()}) - {GetMigrationCount()} migrations, {GetDbContextCount()} contexts";
    }

    public override string ToString()
    {
        return GetSummary();
    }
}
