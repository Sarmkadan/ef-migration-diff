# EfMigrationDiffOptions

Configuration options for generating and comparing Entity Framework Core migrations between branches or paths.

## API

### `public string RepositoryPath`

Gets or sets the file system path to the root of the Git repository containing the EF Core project. Used to resolve relative paths for migrations and schema comparisons. Must point to a valid Git repository root.

### `public string MigrationsPath`

Gets or sets the directory path, relative to `RepositoryPath`, where migration files are located. Defaults to `"Migrations"` if not specified. The path must exist and contain valid EF Core migration classes.

### `public string OutputPath`

Gets or sets the directory where generated reports (HTML, JSON, or text) will be written. If not specified, reports are written to a temporary directory. The directory must be writable.

### `public string ReportFormat`

Gets or sets the format of the generated report. Supported values are `"Text"`, `"Html"`, and `"Json"`. Defaults to `"Text"`. Invalid values will cause `ValidateAndThrow` to throw an `ArgumentException`.

### `public bool EnableDetailedLogging`

Gets or sets a value indicating whether detailed diagnostic logging is enabled during migration analysis. When `true`, logs include intermediate schema states and comparison steps. Useful for debugging but may impact performance.

### `public int MaxConcurrentAnalysis`

Gets or sets the maximum number of concurrent schema analysis tasks. Must be a positive integer. Higher values may improve performance on multi-core systems but increase memory usage. Defaults to `Environment.ProcessorCount`.

### `public bool GenerateHtmlReport`

Gets or sets a value indicating whether an HTML-formatted report should be generated. When `true`, an HTML report is written to `OutputPath` if `ReportFormat` includes `"Html"` or is unspecified.

### `public bool GenerateJsonReport`

Gets or sets a value indicating whether a JSON-formatted report should be generated. When `true`, a JSON report is written to `OutputPath` if `ReportFormat` includes `"Json"` or is unspecified.

### `public string[] DbContextNames`

Gets or sets the names of the `DbContext` classes to include in the migration comparison. If empty or `null`, all contexts in the project are analyzed. Names must match the context class names exactly.

### `public string SourceBranch`

Gets or sets the name of the Git branch or commit hash representing the source state for comparison. Must be a valid branch name or commit SHA in the repository. If not specified, defaults to the current HEAD.

### `public string TargetBranch`

Gets or sets the name of the Git branch or commit hash representing the target state for comparison. Must be a valid branch name or commit SHA in the repository. Required; `ValidateAndThrow` throws an `ArgumentException` if `null` or empty.

### `public SchemaDiffOptions SchemaDiff`

Gets or sets advanced schema comparison options. Allows customization of table, column, index, and constraint comparison behavior. Can be `null`, in which case default comparison rules are applied.

### `public List<string> Validate`

Gets the list of validation rules to enforce during migration analysis. Each string represents a rule identifier (e.g., `"NoDataLoss"`, `"ForeignKeyConsistency"`). Rules are validated during `ValidateAndThrow`.

### `public void ValidateAndThrow()`

Validates all configuration options and throws an `ArgumentException` if any required value is missing or invalid. Validations include:
- `TargetBranch` is not `null` or empty
- `RepositoryPath` exists and is a Git repository
- `MigrationsPath` exists and is readable
- `OutputPath` is writable (if specified)
- `ReportFormat` is a supported value
- `MaxConcurrentAnalysis` is positive

## Usage

### Example 1: Compare migrations between two branches and generate an HTML report
