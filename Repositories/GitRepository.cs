#nullable enable
using EfMigrationDiff.Models;
using LibGit2Sharp;
using System.Text;

namespace EfMigrationDiff.Repositories;

/// <summary>
/// Repository for accessing git operations and branch information.
/// </summary>
public class GitRepository
{
    private readonly string _repositoryPath;
    private Repository? _repository;

    public GitRepository(string repositoryPath)
    {
        ArgumentException.ThrowIfNullOrEmpty(repositoryPath);
        _repositoryPath = repositoryPath;
    }

    /// <summary>
    /// Initializes the git repository connection.
    /// </summary>
    public bool Initialize()
    {
        try
        {
            if (!Directory.Exists(_repositoryPath))
                return false;

            _repository = new Repository(_repositoryPath);
            return _repository is not null;
        }
        catch
        {
            return false;
        }
    }

    /// <summary>
    /// Disposes the git repository connection.
    /// </summary>
    public void Dispose()
    {
        _repository?.Dispose();
    }

    /// <summary>
    /// Gets all branches in the repository.
    /// </summary>
    public List<BranchInfo> GetAllBranches()
    {
        if (_repository is null)
            return [];

        var branches = new List<BranchInfo>();

        try
        {
            foreach (var branch in _repository.Branches)
            {
                if (branch.Tip is not null)
                {
                    branches.Add(new BranchInfo(branch.FriendlyName, branch.Tip.Sha)
                    {
                        CommitMessage = branch.Tip.Message,
                        CommitDate = branch.Tip.Author.When.UtcDateTime,
                        Author = branch.Tip.Author.Name,
                        IsRemote = branch.IsRemote
                    });
                }
            }
        }
        catch
        {
            // Handle git errors gracefully
        }

        return branches;
    }

    /// <summary>
    /// Gets information about a specific branch.
    /// </summary>
    public BranchInfo? GetBranch(string branchName)
    {
        ArgumentException.ThrowIfNullOrEmpty(branchName);
        if (_repository is null)
            return null;

        try
        {
            var branch = _repository.Branches[branchName];
            if (branch?.Tip is not null)
            {
                return new BranchInfo(branch.FriendlyName, branch.Tip.Sha)
                {
                    CommitMessage = branch.Tip.Message,
                    CommitDate = branch.Tip.Author.When.UtcDateTime,
                    Author = branch.Tip.Author.Name,
                    IsRemote = branch.IsRemote
                };
            }
        }
        catch
        {
            // Handle git errors gracefully
        }

        return null;
    }

    /// <summary>
    /// Gets the current branch name.
    /// </summary>
    public string? GetCurrentBranch()
    {
        if (_repository is null)
            return null;

        try
        {
            return _repository.Head.FriendlyName;
        }
        catch
        {
            return null;
        }
    }

    /// <summary>
    /// Gets the commit history between two branches.
    /// </summary>
    public List<string> GetCommitsBetween(string sourceBranch, string targetBranch)
    {
        ArgumentException.ThrowIfNullOrEmpty(sourceBranch);
        ArgumentException.ThrowIfNullOrEmpty(targetBranch);
        if (_repository is null)
            return [];

        var commits = new List<string>();

        try
        {
            var sourceCommit = _repository.Branches[sourceBranch]?.Tip;
            var targetCommit = _repository.Branches[targetBranch]?.Tip;

            if (sourceCommit is not null && targetCommit is not null)
            {
                var sourceHistory = _repository.Commits.QueryBy(new CommitFilter
                {
                    IncludeReachableFrom = sourceCommit
                });
                var targetShas = _repository.Commits.QueryBy(new CommitFilter
                {
                    IncludeReachableFrom = targetCommit
                })
                .Select(c => c.Sha)
                .ToHashSet(StringComparer.Ordinal);

                var range = sourceHistory
                    .Where(c => !targetShas.Contains(c.Sha))
                    .Take(100)
                    .ToList();

                commits.AddRange(range.Select(c => c.Sha));
            }
        }
        catch
        {
            // Handle git errors gracefully
        }

        return commits;
    }

    /// <summary>
    /// Gets files changed between two branches.
    /// </summary>
    public List<string> GetChangedFiles(string sourceBranch, string targetBranch, string? pathFilter = null)
    {
        ArgumentException.ThrowIfNullOrEmpty(sourceBranch);
        ArgumentException.ThrowIfNullOrEmpty(targetBranch);
        if (_repository is null)
            return [];

        var changedFiles = new List<string>();

        try
        {
            var sourceCommit = _repository.Branches[sourceBranch]?.Tip;
            var targetCommit = _repository.Branches[targetBranch]?.Tip;

            if (sourceCommit is not null && targetCommit is not null)
            {
                var compareResult = _repository.Diff.Compare<TreeChanges>(sourceCommit.Tree, targetCommit.Tree);

                foreach (var change in compareResult)
                {
                    var path = change.Path;

                    if (pathFilter is not null && !path.Contains(pathFilter, StringComparison.OrdinalIgnoreCase))
                        continue;

                    if (!changedFiles.Contains(path))
                        changedFiles.Add(path);
                }
            }
        }
        catch
        {
            // Handle git errors gracefully
        }

        return changedFiles;
    }

    /// <summary>
    /// Gets the content of a file at a specific commit.
    /// </summary>
    public string? GetFileContent(string commitSha, string filePath)
    {
        ArgumentException.ThrowIfNullOrEmpty(commitSha);
        ArgumentException.ThrowIfNullOrEmpty(filePath);
        if (_repository is null)
            return null;

        try
        {
            var commit = _repository.Lookup<Commit>(commitSha);
            if (commit is null)
                return null;

            var blob = commit.Tree[filePath]?.Target as Blob;
            return blob?.GetContentText(Encoding.UTF8);
        }
        catch
        {
            return null;
        }
    }

    /// <summary>
    /// Checks if the repository is clean (no uncommitted changes).
    /// </summary>
    public bool IsClean()
    {
        if (_repository is null)
            return false;

        try
        {
            return _repository.RetrieveStatus().IsDirty == false;
        }
        catch
        {
            return false;
        }
    }

    /// <summary>
    /// Gets the root path of the git repository.
    /// </summary>
    public string GetRepositoryRoot()
    {
        return _repository?.Info.WorkingDirectory ?? _repositoryPath;
    }

    public override string ToString()
    {
        return $"GitRepository: {_repositoryPath}";
    }
}
