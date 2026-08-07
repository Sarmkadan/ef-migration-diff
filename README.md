

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
