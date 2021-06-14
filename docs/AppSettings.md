# AppSettings
The `AppSettings` type is designed to hold configuration settings for the `ef-migration-diff` project, providing a centralized location for managing various options and paths used during the migration diff process. This includes settings for repository and migrations paths, output directories, logging, and report generation, among others.

## API
* `public string RepositoryPath`: Gets the path to the repository.
* `public string MigrationsPath`: Gets the path to the migrations.
* `public string OutputPath`: Gets the path where output will be generated.
* `public string ReportFormat`: Gets the format of the report.
* `public bool EnableDetailedLogging`: Gets a value indicating whether detailed logging is enabled.
* `public int MaxConcurrentAnalysis`: Gets the maximum number of concurrent analyses allowed.
* `public bool GenerateHtmlReport`: Gets a value indicating whether an HTML report should be generated.
* `public bool GenerateJsonReport`: Gets a value indicating whether a JSON report should be generated.
* `public string[] DbContextNames`: Gets an array of DbContext names.
* `public string SourceBranch`: Gets the source branch.
* `public string TargetBranch`: Gets the target branch.
* `public SchemaDiffOptions SchemaDiff`: Gets the schema diff options.
* `public List<string> Validate`: Gets a list of validation settings.
* `public string GetMigrationsDirectory()`: Returns the migrations directory path.
* `public string GetOutputDirectory()`: Returns the output directory path.
* `public void EnsureOutputDirectory()`: Ensures the output directory exists, creating it if necessary.
* `public string GetReportFilename()`: Returns the report filename based on the current settings.
* `public SchemaDiffOptions GetSchemaDiffOptions()`: Returns the schema diff options.
* `public void ValidateAndThrow()`: Validates the current settings and throws an exception if any validation fails.
* `public EfMigrationDiffOptions ToEfMigrationDiffOptions()`: Converts the current `AppSettings` instance to an `EfMigrationDiffOptions` instance.

## Usage
The following examples demonstrate how to use the `AppSettings` type in a C# application:
```csharp
// Example 1: Basic usage
var appSettings = new AppSettings
{
    RepositoryPath = @"C:\Repository",
    MigrationsPath = @"C:\Migrations",
    OutputPath = @"C:\Output",
    EnableDetailedLogging = true
};

appSettings.EnsureOutputDirectory();
Console.WriteLine(appSettings.GetReportFilename());

// Example 2: Advanced usage with validation
var advancedAppSettings = new AppSettings
{
    RepositoryPath = @"C:\Repository",
    MigrationsPath = @"C:\Migrations",
    OutputPath = @"C:\Output",
    EnableDetailedLogging = true,
    Validate = new List<string> { "DbContext1", "DbContext2" }
};

advancedAppSettings.ValidateAndThrow();
Console.WriteLine(advancedAppSettings.GetSchemaDiffOptions());
```

## Notes
When using the `AppSettings` type, consider the following edge cases and thread-safety remarks:
* The `EnsureOutputDirectory` method will create the output directory if it does not exist, but it does not handle cases where the directory cannot be created due to permissions or other issues.
* The `ValidateAndThrow` method will throw an exception if any validation fails, but it does not provide detailed information about the specific validation failure.
* The `ToEfMigrationDiffOptions` method assumes that the current `AppSettings` instance is valid and does not perform any additional validation.
* The `AppSettings` type is not designed to be thread-safe, and concurrent access to its members may result in unexpected behavior. If thread-safety is required, consider using a thread-safe wrapper or synchronization mechanisms.
