// =============================================================================
// Author: Vladyslav Zaiets | https://sarmkadan.com
// CTO & Software Architect
// =============================================================================

namespace EfMigrationDiff.Integration;

/// <summary>
/// Integration with GitHub API for repository operations.
/// Provides methods for fetching repository data, pull requests, and branch information.
/// </summary>
public class GitHubIntegration
{
    private readonly HttpClientWrapper _httpClient;
    private readonly string _owner;
    private readonly string _repo;

    private const string GitHubApiBase = "https://api.github.com";

    public GitHubIntegration(string owner, string repo, string? token = null)
    {
        _owner = owner ?? throw new ArgumentNullException(nameof(owner));
        _repo = repo ?? throw new ArgumentNullException(nameof(repo));

        _httpClient = new HttpClientWrapper(GitHubApiBase);

        if (!string.IsNullOrEmpty(token))
        {
            _httpClient.SetBearerToken(token);
        }

        _httpClient.AddDefaultHeader("Accept", "application/vnd.github.v3+json");
    }

    /// <summary>
    /// Gets repository information.
    /// </summary>
    public async Task<GitHubRepository?> GetRepositoryAsync()
    {
        try
        {
            return await _httpClient.GetAsync<GitHubRepository>($"/repos/{_owner}/{_repo}");
        }
        catch (Exception ex)
        {
            throw new GitHubIntegrationException($"Failed to get repository: {ex.Message}", ex);
        }
    }

    /// <summary>
    /// Gets a list of branches in the repository.
    /// </summary>
    public async Task<List<GitHubBranch>?> GetBranchesAsync()
    {
        try
        {
            return await _httpClient.GetAsync<List<GitHubBranch>>($"/repos/{_owner}/{_repo}/branches");
        }
        catch (Exception ex)
        {
            throw new GitHubIntegrationException($"Failed to get branches: {ex.Message}", ex);
        }
    }

    /// <summary>
    /// Gets a specific branch.
    /// </summary>
    public async Task<GitHubBranch?> GetBranchAsync(string branchName)
    {
        try
        {
            return await _httpClient.GetAsync<GitHubBranch>(
                $"/repos/{_owner}/{_repo}/branches/{Uri.EscapeDataString(branchName)}");
        }
        catch (Exception ex)
        {
            throw new GitHubIntegrationException($"Failed to get branch '{branchName}': {ex.Message}", ex);
        }
    }

    /// <summary>
    /// Gets pull requests with optional filters.
    /// </summary>
    public async Task<List<GitHubPullRequest>?> GetPullRequestsAsync(string state = "open")
    {
        try
        {
            return await _httpClient.GetAsync<List<GitHubPullRequest>>(
                $"/repos/{_owner}/{_repo}/pulls?state={state}");
        }
        catch (Exception ex)
        {
            throw new GitHubIntegrationException($"Failed to get pull requests: {ex.Message}", ex);
        }
    }

    /// <summary>
    /// Gets a specific pull request.
    /// </summary>
    public async Task<GitHubPullRequest?> GetPullRequestAsync(int prNumber)
    {
        try
        {
            return await _httpClient.GetAsync<GitHubPullRequest>(
                $"/repos/{_owner}/{_repo}/pulls/{prNumber}");
        }
        catch (Exception ex)
        {
            throw new GitHubIntegrationException($"Failed to get PR #{prNumber}: {ex.Message}", ex);
        }
    }

    /// <summary>
    /// Creates a pull request comment.
    /// </summary>
    public async Task<GitHubComment?> CreateCommentAsync(int prNumber, string comment)
    {
        try
        {
            var data = new { body = comment };
            return await _httpClient.PostAsync<GitHubComment>(
                $"/repos/{_owner}/{_repo}/issues/{prNumber}/comments",
                data);
        }
        catch (Exception ex)
        {
            throw new GitHubIntegrationException($"Failed to create comment: {ex.Message}", ex);
        }
    }

    /// <summary>
    /// Gets commits in a specific branch.
    /// </summary>
    public async Task<List<GitHubCommit>?> GetCommitsAsync(string branchName, int perPage = 30)
    {
        try
        {
            return await _httpClient.GetAsync<List<GitHubCommit>>(
                $"/repos/{_owner}/{_repo}/commits?sha={Uri.EscapeDataString(branchName)}&per_page={perPage}");
        }
        catch (Exception ex)
        {
            throw new GitHubIntegrationException($"Failed to get commits: {ex.Message}", ex);
        }
    }

    public void Dispose()
    {
        _httpClient?.Dispose();
    }
}

/// <summary>
/// GitHub repository information.
/// </summary>
public class GitHubRepository
{
    public long Id { get; set; }
    public string? Name { get; set; }
    public string? FullName { get; set; }
    public string? Description { get; set; }
    public string? Url { get; set; }
    public string? DefaultBranch { get; set; }
    public bool IsPrivate { get; set; }
}

/// <summary>
/// GitHub branch information.
/// </summary>
public class GitHubBranch
{
    public string? Name { get; set; }
    public GitHubCommitRef? Commit { get; set; }
    public bool Protected { get; set; }
}

/// <summary>
/// GitHub commit reference.
/// </summary>
public class GitHubCommitRef
{
    public string? Sha { get; set; }
    public string? Url { get; set; }
}

/// <summary>
/// GitHub pull request information.
/// </summary>
public class GitHubPullRequest
{
    public int Number { get; set; }
    public string? Title { get; set; }
    public string? Body { get; set; }
    public string? State { get; set; }
    public GitHubUser? User { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
}

/// <summary>
/// GitHub user information.
/// </summary>
public class GitHubUser
{
    public long Id { get; set; }
    public string? Login { get; set; }
    public string? AvatarUrl { get; set; }
}

/// <summary>
/// GitHub comment on a PR or issue.
/// </summary>
public class GitHubComment
{
    public long Id { get; set; }
    public string? Body { get; set; }
    public GitHubUser? User { get; set; }
    public DateTime CreatedAt { get; set; }
}

/// <summary>
/// GitHub commit information.
/// </summary>
public class GitHubCommit
{
    public string? Sha { get; set; }
    public GitHubCommitDetails? Commit { get; set; }
    public GitHubUser? Author { get; set; }
}

/// <summary>
/// Details of a GitHub commit.
/// </summary>
public class GitHubCommitDetails
{
    public string? Message { get; set; }
    public DateTime Date { get; set; }
}

/// <summary>
/// Exception for GitHub integration errors.
/// </summary>
public class GitHubIntegrationException : Exception
{
    public GitHubIntegrationException(string message) : base(message) { }
    public GitHubIntegrationException(string message, Exception innerException) : base(message, innerException) { }
}
