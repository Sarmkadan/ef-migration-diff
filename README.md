## EfMigrationDiff

A library for analyzing and comparing database migrations in .NET applications.

## EfMigrationDiffException

The `EfMigrationDiffException` class is a custom exception type used to represent errors that occur during the migration diff process. It provides additional information about the error, such as the file path and line number where the error occurred.

Here's an example of how to use this exception:

```csharp
try
{
    // code that may throw EfMigrationDiffException
}
catch (EfMigrationDiffException ex)
{
    Console.WriteLine($"Error occurred at file path: {ex.FilePath}, line number: {ex.LineNumber}");
    Console.WriteLine(ex.Message);
}
```

This exception can be thrown in various scenarios, such as when there's a conflict between migrations or when there's an issue parsing migration data.

```csharp
throw new EfMigrationDiffException("Migration conflict detected", new List<string> { "conflict1", "conflict2" });
throw new EfMigrationDiffException("Error parsing migration data", "path/to/migration/file.cs", 10);
```

These exceptions can be caught and handled accordingly to provide a better user experience and to facilitate debugging.

## MigrationDiffExtensions

The `MigrationDiffExtensions` class provides a set of extension methods for working with migration data. These methods allow you to analyze and summarize migration information, such as the total number of migrations, migrations needing attention, and schema changes.

Here's an example of how to use these extension methods:

```csharp
var migrationDiff = new[] { /* some migration data */ };
var totalMigrations = MigrationDiffExtensions.GetTotalMigrations(migrationDiff);
var migrationsNeedingAttention = MigrationDiffExtensions.GetMigrationsNeedingAttention(migrationDiff);
var hasDestructiveChanges = MigrationDiffExtensions.HasDestructiveChanges(migrationDiff);

Console.WriteLine($"Total Migrations: {totalMigrations}");
Console.WriteLine($"Migrations Needing Attention: {migrationsNeedingAttention}");
Console.WriteLine($"Has Destructive Changes: {hasDestructiveChanges}");

var summary = MigrationDiffExtensions.GetFormattedSummary(migrationDiff);
Console.WriteLine(summary);
```

These extension methods can be used to gain insights into migration data and make it easier to work with migration information in your application.

## MigrationDiffServiceExtensions

The `MigrationDiffServiceExtensions` class provides extension methods for analyzing migration differences and generating reports. It includes methods for checking destructive changes, generating quick and conflict reports, finding common migration names, and determining if migrations can be merged safely.

Example usage:

```csharp
var migrationDiff = new[] { /* some migration data */ };
var migrations1 = new[] { "Migration1", "Migration2" };
var migrations2 = new[] { "Migration2", "Migration3" };

bool hasDestructive = MigrationDiffServiceExtensions.HasDestructiveChanges(migrationDiff);
string quickReport = MigrationDiffServiceExtensions.GenerateQuickReport(migrationDiff);
List<string> commonMigrations = MigrationDiffServiceExtensions.GetCommonMigrationNames(migrations1, migrations2);
string conflictReport = MigrationDiffServiceExtensions.GenerateConflictReport(migrationDiff);
bool canMerge = MigrationDiffServiceExtensions.CanMergeSafely(migrationDiff);

Console.WriteLine($"Has Destructive Changes: {hasDestructive}");
Console.WriteLine($"Quick Report:\n{quickReport}");
Console.WriteLine($"Common Migrations: {string.Join(", ", commonMigrations)}");
Console.WriteLine($"Conflict Report:\n{conflictReport}");
Console.WriteLine($"Can Merge Safely: {canMerge}");
```

These methods help streamline migration analysis by providing actionable insights and reports for different scenarios.

## MigrationAutoResolverServiceExtensions

The `MigrationAutoResolverServiceExtensions` class provides configuration and resolution capabilities for automated migration conflict resolution. It supports strategy-based conflict resolution patterns like skip, first-wins, last-wins, and combine strategies. The service can be configured with custom logging and reset to default settings.

Example usage:
```csharp
var resolver = MigrationAutoResolverServiceExtensions.ConfigureFirstWinsStrategy()
    .CreateWithLogger(new ConsoleLogger())
    .ConfigureCombineStrategy();

bool success = await resolver.TryResolveAllAsync(migrationConflicts);
var currentStrategy = resolver.GetConfiguredStrategy();
Console.WriteLine($"Current strategy: {currentStrategy.Name}");

if (!success)
{
    resolver.ResetToDefaults();
    // Re-attempt resolution with default settings
}
```

This example demonstrates configuring a resolver with a combination of strategies, setting up logging, resolving conflicts, and handling fallback scenarios.

## VisualDiffOutputTests

The `VisualDiffOutputTests` class provides a set of test methods for verifying the correctness of visual diff output. It includes tests for computing diff with identical changes, source-only changes, target-only changes, destructive changes, and empty inputs. These tests can be used to ensure that the visual diff output is accurate and reliable.

Here's an example of how to use these test methods:

```csharp
var visualDiffOutput = new VisualDiffOutput();
visualDiffOutput.ComputeDiff(new[] { /* some migration data */ }, new[] { /* some migration data */ });

visualDiffOutput.ComputeDiff_WithIdenticalChanges_ReturnsIdenticalResult();
visualDiffOutput.ComputeDiff_WithSourceOnlyChange_PopulatesSourceOnlyList();
visualDiffOutput.ComputeDiff_WithTargetOnlyChange_PopulatesTargetOnlyList();
visualDiffOutput.ComputeDiff_WithDestructiveChange_ReportsDestructive();
visualDiffOutput.ComputeDiff_WithEmptyInputs_ReturnsIdentical();
visualDiffOutput.AcceptSource_BuildsPlanWithAllSourceResolutions();
visualDiffOutput.AutoMerge_WithTriviallyResolvableConflicts_ResolvesAll();
```

These test methods can be used to ensure that the visual diff output is accurate and reliable, and to catch any regressions or bugs that may be introduced in the future.

## SchemaChangeDetectorExtendedTests

The `SchemaChangeDetectorExtendedTests` class provides a set of test methods for verifying the correctness of schema change detection. It includes tests for detecting changes with drop table, alter table, add column, drop column, create index, multiple different operations, empty content, unrelated content, create table only, drop table, drop column, add column non-nullable, add column nullable, rename table operation, extracting table name from create table, and case sensitive table names.

Here's an example of how to use these test methods:

```csharp
var detector = new SchemaChangeDetector();
var changes = detector.DetectChanges(new[] { /* some migration data */ });

detector.DetectChanges_WithDropTableContent_DetectsOneDropTableChange();
detector.DetectChanges_WithAlterTableContent_DetectsAlterTableChange();
detector.DetectChanges_WithAddColumnContent_DetectsAddColumnChange();
detector.DetectChanges_WithDropColumnContent_DetectsDropColumnChange();
detector.DetectChanges_WithCreateIndexContent_DetectsCreateIndexChange();
detector.DetectChanges_WithMultipleDifferentOperations_DetectsAllChanges();
detector.DetectChanges_WithEmptyContent_ReturnsEmptyList();
detector.DetectChanges_WithUnrelatedContent_ReturnsEmptyList();
detector.IsMigrationSafe_WithCreateTableOnly_ReturnsTrue();
detector.IsMigrationSafe_WithDropTable_ReturnsFalse();
detector.IsMigrationSafe_WithDropColumn_ReturnsFalse();
detector.IsMigrationSafe_WithAddColumnNonNullable_ReturnsFalse();
detector.IsMigrationSafe_WithAddColumnNullable_ReturnsTrue();
detector.DetectChanges_WithRenameTableOperation_DetectsRenameChange();
detector.DetectChanges_ExtractsTableNameFromCreateTable();
detector.DetectChanges_WithCaseSensitiveTableNames_PreservesCase();
```

These test methods can be used to ensure that the schema change detection is accurate and reliable, and to catch any regressions or bugs that may be introduced in the future.

## IntegrationTests

The `IntegrationTests` class provides a set of integration tests for verifying the correctness of the migration diff process. It includes tests for parsing and comparing migrations, generating reports, and detecting conflicts.

Here's an example of how to use these tests:

```csharp
var tests = new IntegrationTests();
tests.EndToEnd_ParseParseCompareAndReport_CompletesSuccessfully();
tests.FullWorkflow_MultipleDbContexts_HandlesCorrectly();
tests.ConcurrentMigrationProcessing_MultipleThreadsProcessDifferentMigrations_AllProcessed();
tests.ReportGeneration_WithDifferentFormats_AllFormatsProduceConsistentData();
tests.SchemaChangeDetectionPipeline_ComplexMigration_DetectsAllOperations();
tests.ConflictDetection_WithTableNameConflict_IdentifiesConflict();
tests.MigrationValidation_WithValidAndInvalidMigrations_IdentifiesInvalidOnes();
tests.MultipleDbContextComparison_IndependentContexts_ProcessesWithoutInterference();
tests.ReadmeExample_BasicComparison_WorksAsDocumented();
```

These tests can be used to ensure that the migration diff process is accurate and reliable, and to catch any regressions or bugs that may be introduced in the future.
