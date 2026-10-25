# CompareCommand

`CompareCommand` implements the `compare` CLI command (aliased as `diff`). It compares the EF Core migrations present on two git branches, detects schema changes and conflicts, writes a report, and maps the outcome onto the compare exit code contract.

The class lives in `CLI/Commands/CompareCommand.cs` under the namespace `EfMigrationDiff.CLI.Commands` and implements `ICommand` (`EfMigrationDiff.CLI`).

## Usage

```bash
dotnet run -- compare <source> <target> [--format text|json|html] [--dot <path>] [--summary]
dotnet run -- diff main feature/orders --format json
```

Positional arguments are the source and target branch names. When omitted, they fall back to `AppSettings.SourceBranch` and `AppSettings.TargetBranch`.

| Option | Behavior |
|---|---|
| `--format <value>` | Overrides `AppSettings.ReportFormat` for this run. Expected values are `text`, `json` or `html`. Any other value is passed through and produces a text report. |
| `--dot <path>` | Writes a DOT dependency graph of all migrations found on either branch to `<path>`. The path must resolve inside the repository directory (see `ValidateAndResolvePath`). |
| `--summary` | Presence-only flag. Skips report generation and prints a one-line summary instead. |

## API

### `public Task<CommandResult> ExecuteAsync(CommandContext context)`
Runs the comparison. Steps, in order:

1. Resolves `AppSettings`, `MigrationDiffService` and `ReportGenerationService` from `context.ServiceProvider`. A missing service throws `InvalidOperationException`.
2. Determines source and target branches from the positional arguments or settings, and applies the `--format` override.
3. Sets `AppSettings.RepositoryPath` to `Environment.CurrentDirectory`, opens a `GitRepository` there and resolves both branches. If the repository cannot be initialized, returns an error with the default exit code (`70`). If either branch is missing, returns an error with exit code `64` (`UsageError`).
4. Loads migration files from both branch tips under `AppSettings.MigrationsPath`, then calls `MigrationDiffService.CompareBranches`.
5. If `--dot` is given, validates the path, creates its directory if needed and writes the DOT graph built by `MigrationDependencyGraphService`. An invalid path writes the error to `context.ErrorOutput` and returns exit code `70`.
6. Prints a summary (conflicts, schema change count, result, table/column/index statistics). With `--summary`, it also prints the added/removed/conflict counts. Otherwise it writes a report to `AppSettings.GetOutputDirectory()` using `migration-comparison` as the base name.
7. Disposes the `GitRepository`, computes the exit code with `DetermineExitCode` and returns the result built by `CreateExitResult`.

With the JSON format, the report goes to standard output and the "report saved" status line goes to standard error, so stdout stays machine-readable.

### `public string GetDescription()`
Returns `"Compare migrations between two branches and detect conflicts"`. Used in help output.

### `public static string ValidateAndResolvePath(string path, string repositoryPath)`
Resolves `path` against `repositoryPath` and rejects anything that could escape the repository. Used to validate the `--dot` target.

- Throws `ArgumentNullException` when `path` or `repositoryPath` is `null`, and `ArgumentException` when either is empty or whitespace.
- Throws `ArgumentException` when `path` contains invalid path characters, when it cannot be resolved (`ArgumentException`, `NotSupportedException` or `PathTooLongException` from `Path.GetFullPath`), when the resolved path is outside `repositoryPath`, or when the raw input contains `..`, `~`, or starts with `/` or `\`.
- Returns the resolved absolute path on success.

### `public static int DetermineExitCode(MigrationDiff diff)`
Maps a comparison result to a compare exit code. Checks run from most to least severe:

1. Blocking conflicts (`MigrationDiff.HasBlockingConflicts()`) → `ConflictsFound` (3).
2. Any schema change classified as `Breaking` by `BreakingChangeDetector` → `BreakingChangesFound` (2). Only changes from source-only and target-only migrations are considered.
3. Any schema change in that set, any migration needing attention, or any conflict → `DiffFound` (1).
4. Otherwise → `NoDiff` (0).

Throws `ArgumentNullException` when `diff` is `null`.

## Exit codes

| Code | Constant | Result returned by `ExecuteAsync` |
|---|---|---|
| 0 | `Constants.ExitCodes.NoDiff` | `Success = true`, message "No schema changes between the branches" |
| 1 | `Constants.ExitCodes.DiffFound` | `Success = true`, message "Non-breaking schema changes detected" |
| 2 | `Constants.ExitCodes.BreakingChangesFound` | `Success = false`, message "Breaking schema changes detected" |
| 3 | `Constants.ExitCodes.ConflictsFound` | `Success = false`, message "Blocking conflicts detected" |
| 64 | `Constants.ExitCodes.UsageError` | Source or target branch not found |
| 70 | `Constants.ExitCodes.Error` | Repository init failure, invalid `--dot` path |

Warnings for codes 2 and 3 are also written to the console before the result is returned.

## Usage example

Programmatic use, mirroring `Program.RunCompareAsync`:

```csharp
using EfMigrationDiff.CLI;
using EfMigrationDiff.CLI.Commands;

var executor = new CommandExecutor()
    .RegisterCommand("compare", new CompareCommand());

var result = await executor.ExecuteAsync(
    "compare",
    new[] { "main", "feature/orders", "--format", "json" },
    serviceProvider);

Console.WriteLine($"Exit code: {result.ExitCode}");
```

Resolving an export path:

```csharp
// Returns "<repo>/graphs/migrations.dot"
string resolved = CompareCommand.ValidateAndResolvePath("graphs/migrations.dot", repositoryRoot);

// Throws ArgumentException: contains a traversal sequence
CompareCommand.ValidateAndResolvePath("../outside.dot", repositoryRoot);
```

## Notes

- `GitRepository` is disposed only on the success path, after report generation. Early returns (branch not found, invalid `--dot` path) leave it undisposed.
- `ExecuteAsync` mutates the shared `AppSettings` instance (`RepositoryPath`, and `ReportFormat` when `--format` is given).
- Migration files are parsed from source text, and only `.cs` files that are not `*.Designer.cs` are loaded. Migrations already registered under the same id are reused.
- Tests: `tests/ef-migration-diff.Tests/PathValidationTests.cs` covers `ValidateAndResolvePath`; `CompareExitCodeTests.cs` runs the command through `CommandExecutor` and checks the exit codes.
