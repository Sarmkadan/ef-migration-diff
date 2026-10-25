# Program.cs

`Program.cs` is the entry point of the `ef-migration-diff` console tool. It contains the top-level statements that start the process and the `MigrationDiffApplication` class, which owns the service provider, the command table and the process exit code.

## Invocation

The assembly name is `ef-migration-diff` (`ef-migration-diff.csproj`), so the built binary is `ef-migration-diff` and the DLL is `ef-migration-diff.dll`.

| How it is run | Example |
|---|---|
| Published binary | `ef-migration-diff compare develop main` |
| `dotnet run` from the repo root | `dotnet run -- compare develop main` |
| Docker image | The `Dockerfile` sets `ENTRYPOINT ["dotnet", "ef-migration-diff.dll"]`, so arguments go straight to the tool, e.g. `docker run ... compare develop main` |
| Makefile | `make run` runs `dotnet run -- --help` after building |

The tool reads the repository and configuration from the **current working directory** (`Environment.CurrentDirectory`). Run it from the root of the EF Core git repository you want to inspect.

## Startup flow

Top-level code in `Program.cs` runs in this order:

1. **Construct the application.** `new MigrationDiffApplication()` is called inside a `try`:
   - `ConfigFileLoader.LoadWithPrecedence` merges `efmigrationdiff.json` from the working directory with the defaults in `EfMigrationDiffOptions`.
   - `SqlGenerator.ValidateAndThrow()` runs fail-fast validation of the options.
   - `DependencyInjection.CreateServiceProviderWithConfig` builds the DI container. `AppSettings` is resolved from it.
   - The command table (`_commandHandlers`) is populated.
2. **Handle configuration errors.** If construction throws `ConfigurationValidationException`, the message is written to stderr as `Configuration error: ...` and the process returns `Constants.ExitCodes.UsageError` (64). Argument parsing has not happened yet at this point.
3. **Run.** `await using (application)` calls `application.RunAsync(args)` and returns its result as the process exit code. The `await using` disposes the service provider when the run finishes.

`RunAsync` then:

1. Prints the banner `EF Migration Diff v1.0.0` followed by a separator line.
2. With no arguments, prints usage and returns `0` (`NoDiff`).
3. Dispatches on `args[0]`, case-insensitively:
   - `compare` / `diff` go to `RunCompareAsync`, which goes through `CommandExecutor` so the result maps to the full exit-code contract (see below).
   - Any other registered command runs `BuildDiffOptions(...)?.ValidateAndThrow()` first. Validation failures surface as `ConfigurationValidationException` and exit with 64. Then the handler runs and the method returns `0`.
   - An unknown command prints `Unknown command: <name>`, lists the valid commands and the usage text, and returns `0`.

## Error handling

`RunAsync` wraps everything in a `try`/`catch` with three branches. Each one prints a red message, calls `Environment.Exit`, and returns the code:

| Exception | Output | Exit code |
|---|---|---|
| `ConfigurationValidationException` | `Configuration error: ...` | 64 (`UsageError`) |
| `EfMigrationDiffException` | `Error: ...` | 1 |
| any other `Exception` | `Unexpected error: ...` (stack trace too if `EnableDetailedLogging` is on) | 1 |

Only the compare path returns a meaningful code through the normal return value. Other commands exit with `0` on success, and some of them call `Environment.Exit(1)` directly (see the table below).

## Command registration

Two mechanisms register commands:

**1. `CommandExecutor` for `compare`.** `RunCompareAsync` creates a `CommandExecutor`, registers `CompareCommand` under the name `compare`, and calls `ExecuteAsync`. Its `ExitCode` is returned to the process. This is the only command whose exit code follows the compare contract.

**2. The `_commandHandlers` dictionary** (`Dictionary<string, Func<string[], Task>>`, case-insensitive). It is built in the constructor and maps each name to a handler:

| Name(s) | Handler | Notes |
|---|---|---|
| `validate`, `check` | `ValidateCommand` | Parses `*.cs` migration files (excluding `.Designer.cs`) in the migrations directory. Calls `Environment.Exit(1)` if any file is invalid. |
| `report` | `ReportCommand` | Takes a format argument (default `text`). Prints the output path. |
| `visual-diff`, `visual` | `VisualDiffCommand` | Args: `<source> <target> [format]`. Writes `visual-diff-<UTC stamp>.html`. Calls `Environment.Exit(1)` on destructive changes. |
| `graph`, `dependency-graph` | `DependencyGraphCommand` | Prints migrations in topological order. Calls `Environment.Exit(1)` on cycles. |
| `auto-merge`, `suggest` | `AutoMergeCommand` | Args: `<source> <target>`. Resolves branch conflicts and prints the attempts. |
| `help`, `--help`, `-h` | `ShowHelp` | Prints the full help text. |
| `--version`, `-v` | `ShowVersion` | Prints the name, version and author. |

`compare` and `diff` are not in the dictionary. They are handled before the lookup, so they never reach the `BuildDiffOptions` validation step.

Commands that take branch names are listed in `BranchCommands`: `compare`, `diff`, `visual-diff`, `visual`, `auto-merge`, `suggest`. Commands that also read the migrations directory are listed in `MigrationFileCommands`: `validate`, `check`. For branch commands, `BuildDiffOptions` uses the positional arguments when they are given and falls back to `efmigrationdiff.json` otherwise.

## Exit codes

The exit codes are defined in `Utilities/Constants.cs` (`Constants.ExitCodes`).

| Code | Constant | Meaning |
|---|---|---|
| 0 | `NoDiff` | No changes or conflicts. Also used for help, version and successful non-compare commands. |
| 1 | `DiffFound` | Non-breaking changes (compare). Also used for generic errors and for the `Environment.Exit(1)` paths. |
| 2 | `BreakingChangesFound` | At least one breaking change (compare). |
| 3 | `ConflictsFound` | At least one blocking conflict (compare). |
| 64 | `UsageError` | Bad arguments, options, branch names or configuration. |

## Related files

- `CLI/CommandExecutor.cs`, `CLI/Commands/CompareCommand.cs`: the `compare` pipeline.
- `Configuration/ConfigFileLoader.cs`, `Configuration/DependencyInjection.cs`: configuration precedence and the composition root.
- `Utilities/Constants.cs`: application name, version and exit codes.
- `docs/ARCHITECTURE.md`: design rationale.
