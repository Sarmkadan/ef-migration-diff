

## CommandParserTests

The CommandParserTests class contains tests for the CommandParser class. These tests cover various scenarios, including:

* Parsing valid commands with all flags
* Parsing commands with missing option values
* Parsing commands with unknown flags
* Parsing help invocations

Example usage:
```csharp
public CommandParserTests
public void Parse_ValidCommandWithAllFlags_ShouldPopulateOptionsAndArguments
public void Parse_MissingOptionValue_ShouldTreatAsFlag
public void Parse_UnknownFlag_ShouldBeAddedAsFlag
public void Parse_HelpInvocation_ShouldBeRecognizedAsFlag
```

## MarkdownFormatter

The `MarkdownFormatter` class is used to generate detailed, human-readable migration diff reports in Markdown format. It processes a `MigrationDiff` object to produce comprehensive tables summarizing migrations, conflicts, schema changes, and recommendations for branch management.

Example usage:
```csharp
using EfMigrationDiff.Formatters;
using EfMigrationDiff.Models;

// Assuming 'diff' is an existing MigrationDiff object
var formatter = new MarkdownFormatter(includeDestructiveWarnings: true);

// Generate the report as a string
string report = formatter.GenerateMarkdownReport(diff);

// Alternatively, write directly to a file
formatter.WriteToFile("migration-diff-report.md", diff);
```

## BreakingChangeDetector

The `BreakingChangeDetector` service analyses schema changes to identify breaking changes that could affect downstream applications. It classifies individual changes, provides a collection of classifications, and produces a summary of the overall diff.

Example usage:
```csharp
using EfMigrationDiff.Services;
using EfMigrationDiff.Models;

// Create the detector (parameter‑less constructor)
var detector = new BreakingChangeDetector();

// Classify a collection of schema changes (returns a list of classifications)
var classifications = detector.ClassifyChanges();

// Classify a single schema change (example with a default‑constructed SchemaChange)
var singleClassification = detector.ClassifyChange(new SchemaChange());

// Get a summary of the breaking‑change analysis for the whole diff
var summary = detector.ClassifyDiffResult();
```

## BreakingChangeDetectorTests

The `BreakingChangeDetectorTests` class contains tests for the breaking change detector service. These tests verify how schema changes are classified: dropping columns, tables, indexes, foreign keys, or stored procedures is reported as breaking, while adding nullable columns or widening data types is considered safe. They also cover warning-level classifications such as renames and newly added foreign keys, batch processing of multiple changes, and the correctness of the overall diff result counts and safety summary.

Example usage:
```csharp
using EfMigrationDiff.Tests;

// Create the test class instance
var tests = new BreakingChangeDetectorTests();

// Run the individual classification tests
tests.ClassifyChange_DropColumn_IsBreaking();
tests.ClassifyChange_DropTable_IsBreaking();
tests.ClassifyChange_AddNullableColumn_IsSafe();
tests.ClassifyChange_ModifyColumn_NarrowingIntToSmallint_IsBreaking();
tests.ClassifyChange_Rename_IsWarning();
tests.ClassifyChanges_ProcessesMultipleChanges();
tests.ClassifyDiffResult_CalculatesCorrectCounts();
tests.ClassifyDiffResult_IsSafe_WhenNoBreakingOrWarnings();
```

## SchemaChangeDetectorServiceTests

The `SchemaChangeDetectorServiceTests` class contains tests for the schema change detector service. These tests verify that creating and dropping tables, columns, indexes, and foreign keys are detected correctly, including table renames and raw SQL statements. They also confirm that the detector reports the affected tables as a distinct list.

Example usage:
```csharp
using EfMigrationDiff.Tests;

// Create the test class instance
var tests = new SchemaChangeDetectorServiceTests();

// Run the individual detection tests
tests.Detect_Create_And_Drop_Table();
tests.Detect_Add_And_Drop_Column_With_Metadata();
tests.Detect_Create_And_Drop_Index();
tests.Detect_Rename_Table();
tests.Detect_Add_And_Drop_ForeignKey();
tests.Detect_Raw_Sql_Create_Table();
tests.Get_Affected_Tables_Returns_Distinct_List();
```

## ConflictResolutionEngineTests

The `ConflictResolutionEngineTests` class contains tests for the conflict resolution engine. These tests verify that the engine initializes with default strategies and maps each conflict type (table, column, index, constraint, operation, dependency, and name) to the appropriate resolution strategy with the expected severity, including critical severity for column conflicts and high or medium severity for blocking and non-blocking conflicts. They also confirm that batch resolution produces complete reports, that custom strategies can be registered to override the defaults, and that every resolution includes recommendations.

Example usage:
```csharp
using EfMigrationDiff.Tests;

// Create the test class instance
var tests = new ConflictResolutionEngineTests();

// Run the individual resolution tests
tests.ConflictResolutionEngine_InitializesWithDefaultStrategies();
tests.ResolveConflict_WithTableConflict_ReturnsManualResolutionStrategy();
tests.ResolveConflict_WithColumnConflict_ReturnsReviewResolutionStrategyWithCriticalSeverity();
tests.ResolveConflict_WithIndexConflict_ReturnsAutomaticResolutionStrategy();
tests.ResolveConflict_WithConstraintConflict_ReturnsReviewResolutionStrategy();
tests.ResolveConflict_WithOperationConflict_ReturnsManualResolutionStrategy();
tests.ResolveConflict_WithDependencyConflict_ReturnsManualResolutionStrategy();
tests.ResolveConflict_WithNameConflict_ReturnsManualResolutionStrategy();
tests.ResolveConflict_WithUnknownConflictType_ReturnsDefaultManualStrategy();
tests.ResolveConflict_ColumnConflictAlwaysHasCriticalSeverity();
tests.ResolveConflict_WithBlockingConflict_ReturnsHighSeverity();
tests.ResolveConflict_WithNonBlockingConflict_ReturnsMediumSeverity();
tests.ResolveBatch_WithMultipleConflicts_ReturnsCompleteReport();
tests.ResolveBatch_WithEmptyList_ReturnsEmptyReport();
tests.ResolveBatch_CanProceedWithoutManualIntervention_ReturnsCorrectValue();
tests.RegisterStrategy_WithCustomStrategy_OverridesDefaultStrategy();
tests.ResolveConflict_AllConflictTypes_HaveResolutionStrategies();
tests.ResolveConflict_RecommendationsGeneratedForEachConflictType();
tests.ConflictResolution_AllPropertiesSetCorrectly();
```
