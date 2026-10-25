# Constants

`Utilities/Constants.cs` (namespace `EfMigrationDiff.Utilities`) holds the application-wide constants: names and version, file-system paths, migration limits, regex patterns, git branch names, report formats, user-facing messages, SQL keywords, size limits, timeouts, parallelism bounds and CLI exit codes.

All members are `const`, so they are compile-time values. The class is `public static` and has no instances.

Usage notes:

- **Used** means referenced from production code (`Program.cs`, `CLI/`, `Utilities/`) and, where noted, from tests.
- **Unused** means no reference exists anywhere in the repository (outside `Constants.cs` itself). These are kept as reserved values and are not wired to any behavior.
- Some values are duplicated as literals elsewhere. Those duplicates are noted below, but they were not changed.

## Top level

| Constant | Type | Value | Where used |
|---|---|---|---|
| `ApplicationName` | `string` | `"EF Migration Diff"` | `Program.cs:129`, `Program.cs:518` (version banner) |
| `ApplicationVersion` | `string` | `"1.0.0"` | `Program.cs:129`, `Program.cs:518` (version banner) |
| `Author` | `string` | `"Vladyslav Zaiets"` | `Program.cs:519` (`ShowVersion`, `--version` output) |

## `Constants.Paths`

| Constant | Type | Value | Where used |
|---|---|---|---|
| `DefaultMigrationsFolder` | `string` | `"Migrations"` | **Unused.** `Configuration/AppSettings.cs:21` and `Configuration/EfMigrationDiffOptions.cs:24` set `MigrationsPath = "Migrations"` as a literal. |
| `DefaultReportsFolder` | `string` | `"reports"` | **Unused.** Not referenced. |
| `MigrationDesignerSuffix` | `string` | `".Designer"` | `Utilities/FileHelper.cs:76` (excludes `*.Designer.cs` files when listing migrations) |
| `MigrationExtension` | `string` | `".cs"` | `Utilities/FileHelper.cs:75`, `Utilities/FileHelper.cs:76`, `Utilities/FileHelper.cs:96` (`Directory.GetFiles` search pattern) |

## `Constants.Migration`

| Constant | Type | Value | Where used |
|---|---|---|---|
| `TimestampLength` | `int` | `14` | `Utilities/ValidationHelper.cs:19` (migration ID timestamp length check) |
| `TimestampFormat` | `string` | `"yyyyMMddHHmmss"` | **Unused.** `CLI/Commands/VisualDiffCommand.cs:43` defines its own private `TimestampFormat = "yyyyMMdd-HHmmss"`, which is a different value. |
| `MaxNameLength` | `int` | `255` | `Utilities/ValidationHelper.cs:92` (migration name length) |
| `MaxTableNameLength` | `int` | `128` | `Utilities/ValidationHelper.cs:46`. `Services/ConflictDetectionService.cs:13` has a private duplicate with the same value. |
| `MaxColumnNameLength` | `int` | `128` | `Utilities/ValidationHelper.cs:62`. `Services/ConflictDetectionService.cs:14` has a private duplicate with the same value. |
| `MaxIndexNameLength` | `int` | `128` | `Utilities/ValidationHelper.cs:77` (index name length) |

## `Constants.Patterns`

| Constant | Type | Value | Where used |
|---|---|---|---|
| `MigrationFilePattern` | `string` | `"*.cs"` | **Unused.** `CLI/Commands/ValidateCommand.cs:17` defines its own private `MigrationFilePattern = "*.cs"`. |
| `MigrationIdPattern` | `string` | `@"^\d{14}_"` | `Utilities/ValidationHelper.cs:34`, `Utilities/LookupExtensions.cs:22` (`Regex.IsMatch` for migration IDs) |
| `UpMethodPattern` | `string` | `@"protected\s+override\s+void\s+Up\s*\(MigrationBuilder\s+migrationBuilder\)"` | **Unused.** |
| `DownMethodPattern` | `string` | `@"protected\s+override\s+void\s+Down\s*\(MigrationBuilder\s+migrationBuilder\)"` | **Unused.** |

## `Constants.Git`

| Constant | Type | Value | Where used |
|---|---|---|---|
| `GitDirectoryName` | `string` | `".git"` | **Unused.** |
| `MainBranch` | `string` | `"main"` | **Unused** in production code. The tests define their own `MainBranch` in `tests/ef-migration-diff.Tests/CompareExitCodeTests.cs:26`. `Models/BranchInfo.cs:113` uses the literal `"main"`. |
| `MasterBranch` | `string` | `"master"` | **Unused.** `Models/BranchInfo.cs:113` uses the literal `"master"`. |
| `DevelopBranch` | `string` | `"develop"` | **Unused.** `Configuration/AppSettings.cs:65` and `Configuration/EfMigrationDiffOptions.cs:68` use the literal `"develop"` as the default source branch. |

## `Constants.ReportFormats`

Values are the accepted `ReportFormat` setting values; `text` is the default.

| Constant | Type | Value | Where used |
|---|---|---|---|
| `Text` | `string` | `"text"` | **Unused.** `Configuration/AppSettings.cs:33` and `Configuration/EfMigrationDiffOptions.cs:36` use the literal `"text"` as the default. |
| `Json` | `string` | `"json"` | `CLI/Commands/CompareCommand.cs:435`, `CLI/Commands/CompareCommand.cs:445` |
| `Html` | `string` | `"html"` | `CLI/Commands/CompareCommand.cs:436` |
| `Xml` | `string` | `"xml"` | **Unused.** |

## `Constants.OutputMessages`

| Constant | Type | Value | Where used |
|---|---|---|---|
| `AnalysisStarted` | `string` | `"Starting migration analysis..."` | **Unused.** |
| `AnalysisCompleted` | `string` | `"Analysis completed successfully."` | **Unused.** |
| `ConflictsDetected` | `string` | `"Conflicts detected during analysis."` | **Unused.** `CLI/Commands/CompareCommand.cs:70` has a separate `Messages.ConflictsDetected` with the value `" Conflicts detected: {0}"`. |
| `NoConflicts` | `string` | `"No conflicts detected."` | **Unused.** |
| `NoMigrations` | `string` | `"No migrations found."` | **Unused.** |

## `Constants.ErrorMessages`

| Constant | Type | Value | Where used |
|---|---|---|---|
| `InvalidRepository` | `string` | `"Invalid or not a git repository."` | **Unused.** |
| `MigrationsNotFound` | `string` | `"Migrations directory not found."` | **Unused.** |
| `InvalidMigrationFile` | `string` | `"Invalid migration file format."` | **Unused.** |
| `BranchNotFound` | `string` | `"The specified branch was not found."` | **Unused.** |
| `NoMigrationsInBranch` | `string` | `"No migrations found in the specified branch."` | **Unused.** |

## `Constants.SqlStatements`

Keyword names of EF Core `MigrationBuilder` operations as they appear in migration source text.

| Constant | Type | Value | Where used |
|---|---|---|---|
| `CreateTableKeyword` | `string` | `"CreateTable"` | **Unused.** `Analysis/MigrationImpactAnalyzer.cs:15` has a separate `DropTableMarker = "DropTable"`. |
| `DropTableKeyword` | `string` | `"DropTable"` | **Unused.** Same duplicate as above. |
| `AddColumnKeyword` | `string` | `"AddColumn"` | **Unused.** |
| `DropColumnKeyword` | `string` | `"DropColumn"` | **Unused.** |
| `AlterColumnKeyword` | `string` | `"AlterColumn"` | **Unused.** |
| `CreateIndexKeyword` | `string` | `"CreateIndex"` | **Unused.** `Analysis/MigrationImpactAnalyzer.cs:16` has a separate `CreateIndexMarker = "CreateIndex"`. |
| `DropIndexKeyword` | `string` | `"DropIndex"` | **Unused.** |

## `Constants.FileSize`

| Constant | Type | Value | Where used |
|---|---|---|---|
| `MaxMigrationFileSize` | `long` | `10 * 1024 * 1024` (10 MB) | `Utilities/FileHelper.cs:28` (rejects oversized migration files) |
| `MaxReportFileSize` | `long` | `50 * 1024 * 1024` (50 MB) | **Unused.** |

## `Constants.Timeouts`

| Constant | Type | Value | Where used |
|---|---|---|---|
| `GitOperationTimeoutMs` | `int` | `30000` (30 s) | **Unused.** |
| `FileOperationTimeoutMs` | `int` | `10000` (10 s) | **Unused.** |
| `AnalysisTimeoutMs` | `int` | `300000` (5 min) | **Unused.** |

## `Constants.Concurrency`

| Constant | Type | Value | Where used |
|---|---|---|---|
| `DefaultMaxDegreeOfParallelism` | `int` | `4` | **Unused.** |
| `MinDegreeOfParallelism` | `int` | `1` | **Unused.** |
| `MaxDegreeOfParallelism` | `int` | `16` | **Unused.** |

## `Constants.ExitCodes`

Process exit codes returned by CLI commands. The `compare` command uses the full contract so CI pipelines can gate on the result. Codes 64 and above mean usage, parse or internal errors.

| Constant | Type | Value | Meaning | Where used |
|---|---|---|---|---|
| `NoDiff` | `int` | `0` | No schema changes and no conflicts. | `Program.cs:135`, `Program.cs:150`, `Program.cs:157`, `CLI/CommandExecutor.cs:291`, `CLI/Commands/CompareCommand.cs:198`, `CLI/Commands/CompareCommand.cs:282`, `tests/ef-migration-diff.Tests/CompareExitCodeTests.cs:202` |
| `DiffFound` | `int` | `1` | Changes found, none breaking. | `CLI/Commands/CompareCommand.cs:198`, `CLI/Commands/CompareCommand.cs:283`, `tests/ef-migration-diff.Tests/CompareExitCodeTests.cs:213` |
| `BreakingChangesFound` | `int` | `2` | At least one breaking schema change. | `CLI/Commands/CompareCommand.cs:195`, `CLI/Commands/CompareCommand.cs:289`, `CLI/Commands/CompareCommand.cs:464`, `tests/ef-migration-diff.Tests/CompareExitCodeTests.cs:224`, `tests/ef-migration-diff.Tests/CompareExitCodeTests.cs:236` |
| `ConflictsFound` | `int` | `3` | At least one blocking conflict between branches. | `CLI/Commands/CompareCommand.cs:188`, `CLI/Commands/CompareCommand.cs:461`, `tests/ef-migration-diff.Tests/CompareExitCodeTests.cs:246` |
| `UsageError` | `int` | `64` | Invalid arguments, options, branch names or configuration. | `Program.cs:20`, `Program.cs:164`, `Program.cs:165`, `CLI/CommandExecutor.cs:74`, `CLI/CommandExecutor.cs:98`, `CLI/CommandExecutor.cs:116`, `CLI/CommandExecutor.cs:131`, `CLI/Commands/CompareCommand.cs:337`, `CLI/Commands/CompareCommand.cs:342`, `tests/ef-migration-diff.Tests/CompareExitCodeTests.cs:255`, `tests/ef-migration-diff.Tests/CompareExitCodeTests.cs:267` |
| `Error` | `int` | `70` | Unexpected internal failure, including analysis that throws. Also the default exit code of `CommandResult.Error`. | `CLI/CommandExecutor.cs:87`, `CLI/CommandExecutor.cs:146`, `:161`, `:176`, `:191`, `:206`, `:221` (handler failures), `CLI/CommandExecutor.cs:298` (default parameter), `CLI/Commands/CompareCommand.cs:371` |
