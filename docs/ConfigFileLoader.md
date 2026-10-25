# ConfigFileLoader

`ConfigFileLoader` is a static class in `EfMigrationDiff.Configuration` (`Configuration/ConfigFileLoader.cs`). It reads the JSON configuration file (`efmigrationdiff.json`) into an `EfMigrationDiffOptions` instance, locates that file by walking up the directory tree, performs a basic and an unknown-property check, and merges file values with command-line overrides.

It is used by:
* `Program.cs` (`MigrationDiffApplication`), which calls `LoadWithPrecedence` with the current directory and the CLI options.
* `AppSettings.cs`, which exposes `LoadFromNearestConfigFile` (through `LoadFromConfigFile`) and `ValidateNoUnknownProperties` (through `ValidateConfigFile`).

## Purpose

* Find the nearest config file for a starting directory, so the tool works when run from any subdirectory of a project.
* Deserialize the file into `EfMigrationDiffOptions` with case-insensitive property names and string-based enum values.
* Reject files that cannot be used: missing file, invalid JSON, a `null` document, or no `RepositoryPath`.
* Optionally reject files that contain properties the tool does not know.
* Combine file values with CLI values, where the CLI value wins.

Config file names searched, in order within each directory:
1. `efmigrationdiff.json`
2. `ef-migration-diff.json` (legacy name, kept for backward compatibility)

## API

* `public static EfMigrationDiffOptions? LoadFromNearestConfigFile(string startingDirectory)`: Finds the nearest config file with `FindNearestConfigFile` and loads it with `LoadFromFile`. Returns `null` when no file is found. Throws `ArgumentNullException` when `startingDirectory` is null.
* `public static string? FindNearestConfigFile(string startingDirectory)`: Converts the path to a full path with `Path.GetFullPath`, then checks each directory from that point up to the root for the two config file names. Returns the full path of the first match, or `null`. Throws `ArgumentNullException` when `startingDirectory` is null.
* `public static EfMigrationDiffOptions LoadFromFile(string filePath)`: Reads and deserializes one specific file. Throws `ArgumentNullException` when `filePath` is null, and `ConfigFileException` in the cases listed under *Loading behavior*.
* `public static void ValidateNoUnknownProperties(string filePath)`: Parses the file and throws `ConfigFileException` on the first property name that is not a public property of `EfMigrationDiffOptions`. Does nothing when the file does not exist. Throws `ArgumentNullException` when `filePath` is null.
* `public static EfMigrationDiffOptions MergeWithPrecedence(EfMigrationDiffOptions baseOptions, EfMigrationDiffOptions overrideOptions)`: Builds a new `EfMigrationDiffOptions` from the two inputs (see *Merge rules*). Neither input is modified. Throws `ArgumentNullException` when either argument is null.
* `public static EfMigrationDiffOptions LoadWithPrecedence(EfMigrationDiffOptions cliOptions, string repositoryPath)`: Looks for a config file from `repositoryPath` upward. If one is found, returns `MergeWithPrecedence(fileOptions, cliOptions)`; otherwise returns `cliOptions` unchanged (the same instance). Throws `ArgumentNullException` when either argument is null.

## Loading behavior

### File discovery
* `FindNearestConfigFile` starts at the full path of the given directory and checks `efmigrationdiff.json`, then `ef-migration-diff.json`, in each directory. It stops at the first match, so a file in a closer directory wins over a file in a parent directory, and `efmigrationdiff.json` wins over the legacy name in the same directory.
* The search stops at the file-system root. If nothing is found, `null` is returned; no exception is thrown.
* The starting path is not required to exist. Only the existence of config files is checked.

### Deserialization (`LoadFromFile`)
1. If the file does not exist, throws `ConfigFileException` (`Config file not found: <path>`).
2. Reads the whole file as text and deserializes it into `EfMigrationDiffOptions` with `System.Text.Json`:
   * property names are matched case-insensitively;
   * enums are read from strings (`JsonStringEnumConverter`);
   * unknown properties are ignored by this step (they are only reported by `ValidateNoUnknownProperties`).
3. If the result is `null` (for example, the file contains the JSON literal `null`), throws `ConfigFileException`.
4. If `RepositoryPath` is null or empty, throws `ConfigFileException` (`... must specify RepositoryPath`).
5. Errors from step 2 are wrapped in `ConfigFileException`: a `JsonException` gives `Invalid JSON in config file ...`, and any other exception (for example an `IOException`) gives `Failed to load config file ...`. The original exception is kept as the inner exception.

Defaults for properties that are not in the file come from the `EfMigrationDiffOptions` property initializers (for example `MigrationsPath` is `"Migrations"`, `OutputPath` is `"./reports"`, `SourceBranch` is `"develop"`, `TargetBranch` is `"main"`, `MaxConcurrentAnalysis` is `4`).

The file is read with `File.ReadAllText`, so encoding detection follows the .NET defaults.

### Unknown-property check (`ValidateNoUnknownProperties`)
* The file is parsed with `JsonDocument.Parse`.
* If the root object has an `EfMigrationDiff` property (exact case), its children are checked. Otherwise the root object's own properties are checked (the legacy flat format).
* Each name is compared case-insensitively with the names of the public properties of `EfMigrationDiffOptions` (found by reflection). The first unknown name causes `ConfigFileException` listing the valid names.
* This check is separate from `LoadFromFile`; `LoadFromFile` does not call it.

### Merge rules (`MergeWithPrecedence`)
The result is a new instance. For each property, the rule is:

| Property | Result |
|---|---|
| `RepositoryPath` | Always from `baseOptions` (the file). The override value is ignored. |
| `MigrationsPath`, `OutputPath`, `ReportFormat`, `SourceBranch`, `TargetBranch` | From `overrideOptions` when it is non-empty; otherwise from `baseOptions`. |
| `MaxConcurrentAnalysis` | From `overrideOptions` when it is non-zero; otherwise from `baseOptions`. |
| `DbContextNames`, `IgnoredMigrations` | From `overrideOptions` when it has at least one item; otherwise from `baseOptions`. |
| `SchemaDiff` | From `overrideOptions` when it is not null; otherwise from `baseOptions`. |
| `SqlGenerator` | From `overrideOptions` when it is not `SqlGeneratorOptions.Default`; otherwise from `baseOptions`. |
| `EnableDetailedLogging`, `GenerateHtmlReport`, `GenerateJsonReport` | Always from `overrideOptions`. |

## Usage

```csharp
// 1. Load the nearest config file, or null when there is none
var fromFile = ConfigFileLoader.LoadFromNearestConfigFile(Environment.CurrentDirectory);

// 2. Load one specific file
var options = ConfigFileLoader.LoadFromFile(@"C:\repo\efmigrationdiff.json");

// 3. Check a file for typos before using it
ConfigFileLoader.ValidateNoUnknownProperties(@"C:\repo\efmigrationdiff.json");

// 4. Combine the file with CLI values (CLI wins for the properties listed above)
var cli = new EfMigrationDiffOptions { SourceBranch = "release/1.0" };
var merged = ConfigFileLoader.LoadWithPrecedence(cli, @"C:\repo");
```

Example file (`efmigrationdiff.json`). Only `RepositoryPath` is required:

```json
{
  "RepositoryPath": "C:/src/my-app",
  "MigrationsPath": "Data/Migrations",
  "SourceBranch": "develop",
  "TargetBranch": "main",
  "DbContextNames": [ "AppDbContext" ]
}
```

## Notes

* `LoadFromFile` does not accept the `EfMigrationDiff` wrapper object that `ValidateNoUnknownProperties` allows. If the file is `{ "EfMigrationDiff": { "RepositoryPath": "..." } }`, the wrapper is ignored during deserialization, `RepositoryPath` is empty, and `LoadFromFile` throws. Use the flat format for files that are loaded.
* `ValidateNoUnknownProperties` compares names with the property list of `EfMigrationDiffOptions` only. Methods are not counted, so a method name in the file is reported as unknown.
* `ValidateNoUnknownProperties` calls `JsonElement.TryGetProperty`, which throws `InvalidOperationException` when the root of the file is a JSON array or other non-object value. That exception is not a `JsonException` and is not wrapped in `ConfigFileException`.
* `ValidateNoUnknownProperties` does not dispose the `JsonDocument` it creates.
* `FindNearestConfigFile` and `LoadWithPrecedence` pass the path to `Path.GetFullPath`. Invalid path strings (for example, an empty string) throw the exception from that call (`ArgumentException`, `NotSupportedException`), which is not wrapped.
* `EnableDetailedLogging`, `GenerateHtmlReport` and `GenerateJsonReport` are `bool` properties and have no "not set" state, so `MergeWithPrecedence` always takes them from the override. A `false` in the CLI options therefore replaces a `true` from the file, even when the CLI did not explicitly set it.
* `ConfigFileLoader` has no shared state other than the read-only `JsonSerializerOptions` instance. Its methods are safe to call from several threads, provided the caller does not change the `EfMigrationDiffOptions` instances it passes in.
