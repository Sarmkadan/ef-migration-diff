# GitRepository
Wrapper around a local Git repository that provides read‑only access to branch, commit, and file‑system information used by the ef‑migration‑diff tool.

## API
### public GitRepository()
Creates an instance bound to the repository located at the path supplied to the constructor (not shown in the member list). The object is not initialized until `Initialize` is called.

### public bool Initialize()
Attempts to open and prepare the underlying Git repository.  
- **Return value**: `true` if the repository was successfully opened; `false` if the path does not point to a valid Git repository.  
- **Exceptions**:  
  - `IOException` – an I/O error occurred while accessing the filesystem.  
  - `UnauthorizedAccessException` – insufficient permissions to read the repository directory.

### public void Dispose()
Releases any unmanaged resources held by the instance (e.g., native libgit2 handles).  
- **Exceptions**: None; calling `Dispose` multiple times is safe.

### public List<BranchInfo> GetAllBranches()
Enumerates every branch known to the repository.  
- **Return value**: A list containing `BranchInfo` objects for each local and remote‑tracking branch.  
- **Exceptions**:  
  - `InvalidOperationException` – the repository has not been initialized via `Initialize`.

### public BranchInfo? GetBranch()
Retrieves information about the repository’s default branch (the branch pointed to by the remote’s HEAD).  
- **Return value**: A `BranchInfo` instance if the default branch can be determined; otherwise `null`.  
- **Exceptions**:  
  - `InvalidOperationException` – the repository has not been initialized.

### public string? GetCurrentBranch()
Returns the name of the branch that HEAD currently points to.  
- **Return value**: The branch name as a string, or `null` when HEAD is in a detached state.  
- **Exceptions**:  
  - `InvalidOperationException` – the repository has not been initialized.

### public List<string> GetCommitsBetween()
Provides the commit hashes that differ between the current branch and its configured upstream (or between HEAD and the default branch when no upstream is set).  
- **Return value**: A list of commit SHA‑1 strings, ordered from oldest to newest. An empty list indicates no divergent commits.  
- **Exceptions**:  
  - `InvalidOperationException` – the repository has not been initialized.  
  - `InvalidOperationException` – the upstream branch cannot be resolved.

### public List<string> GetChangedFiles()
Lists files that have modifications in the working tree relative to the HEAD commit (includes both staged and unstaged changes).  
- **Return value**: A list of file paths relative to the repository root. An empty list indicates a clean working tree.  
- **Exceptions**:  
  - `InvalidOperationException` – the repository has not been initialized.

### public string? GetFileContent()
Attempts to read the contents of a file located at the repository root (the file to read is implied by the internal state set by prior calls such as `GetChangedFiles`).  
- **Return value**: The file’s text content encoded as UTF‑8, or `null` if the file does not exist or cannot be read.  
- **Exceptions**:  
  - `IOException` – an I/O error occurred while reading the file.  
  - `InvalidOperationException` – the repository has not been initialized.

### public bool IsClean()
Indicates whether the working directory has no pending changes (neither staged nor unstaged).  
- **Return value**: `true` if the repository is clean; `false` otherwise.  
- **Exceptions**:  
  - `InvalidOperationException` – the repository has not been initialized.

### public string GetRepositoryRoot()
Provides the absolute filesystem path to the root of the Git repository.  
- **Return value**: The repository root directory.  
- **Exceptions**:  
  - `InvalidOperationException` – the repository has not been initialized.

### public override string ToString()
Returns a human‑readable representation of the instance, currently the repository root path.  
- **Return value**: A string containing the repository root.

## Usage
```csharp
using var repo = new GitRepository(@"C:\projects\my-app");
if (repo.Initialize())
{
    // List all branches
    foreach (var branch in repo.GetAllBranches())
    {
        Console.WriteLine($"{branch.Name} ({branch.Tip})");
    }

    // Show the current branch
    var current = repo.GetCurrentBranch();
    Console.WriteLine($"Current branch: {current ?? "(detached)"}");

    // Check for uncommitted changes
    if (!repo.IsClean())
    {
        var changed = repo.GetChangedFiles();
        Console.WriteLine("Changed files:");
        foreach (var file in changed)
            Console.WriteLine($"  {file}");

        // Example: read the first changed file
        var first = changed.FirstOrDefault();
        if (first != null)
        {
            var content = repo.GetFileContent();
            if (content != null)
                Console.WriteLine($"Content of {first}:{Environment.NewLine}{content}");
        }
    }
}
```
```csharp
// Example using the default branch information
using var repo = new GitRepository(@"C:\projects\ef-migration-diff");
if (repo.Initialize())
{
    var defaultBranch = repo.GetBranch();
    if (defaultBranch != null)
    {
        Console.WriteLine($"Default branch: {defaultBranch.Name} (commit {defaultBranch.Tip})");
    }

    // Get commits that are on the current branch but not on the default branch
    var diverging = repo.GetCommitsBetween();
    if (diverging.Any())
    {
        Console.WriteLine($"Commits diverging from default ({diverging.Count}):");
        foreach (var sha in diverging)
            Console.WriteLine($"  {sha}");
    }
    else
    {
        Console.WriteLine("No diverging commits.");
    }
}
```

## Notes
- All instance members except the constructor and `Dispose` require a successful call to `Initialize`; invoking them beforehand throws `InvalidOperationException`.  
- The class is **not thread‑safe**. Concurrent calls from multiple threads on the same `GitRepository` instance may result in undefined behavior; external synchronization is required if shared access is needed.  
- `GetBranch` returns `null` when the repository lacks a detectable default branch (e.g., a bare repository with no remote HEAD).  
- `GetCurrentBranch` returns `null` when HEAD is detached; callers should treat this as a non‑branch state.  
- `GetCommitsBetween` yields an empty list when the current branch is identical to its upstream (or to the default branch when no upstream is configured).  
- `GetChangedFiles` includes both staged (index) and unstaged (working tree) modifications; it returns an empty list only when `IsClean` is `true`.  
- `GetFileContent` returns `null` if the target file cannot be found, is binary, or cannot be decoded as UTF‑8; callers should check for `null` before using the result.  
- `Dispose` may be called multiple times without error; after disposal, any further instance member use throws `ObjectDisposedException`.  
- `ToString` is provided for convenience and debugging; its exact format is subject to change but will always contain the repository root path.
