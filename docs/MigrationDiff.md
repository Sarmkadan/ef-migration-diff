# MigrationDiff

The `MigrationDiff` class serves as the primary result container for comparing database migration histories and schema states between two distinct branches within the `ef-migration-diff` project. It aggregates discrepancies in migration files, identifies commonalities, captures schema-level differences, and records any conflicts detected during the comparison process, providing a comprehensive snapshot for resolution or reporting.

## API

### Properties

#### `public string Id`
A unique identifier assigned to this specific comparison instance. This value is typically generated upon instantiation to track the diff session.

#### `public string SourceBranchId`
The identifier or name of the source branch used as the baseline for the comparison.

#### `public string TargetBranchId`
The identifier or name of the target branch compared against the source.

#### `public List<Migration> OnlyInSource`
A collection of `Migration` objects that exist in the source branch but are absent in the target branch.

#### `public List<Migration> OnlyInTarget`
A collection of `Migration` objects that exist in the target branch but are absent in the source branch.

#### `public List<Migration> InBoth`
A collection of `Migration` objects that are present in both the source and target branches, indicating shared history.

#### `public List<SchemaChange> SourceSchemaChanges`
A list of schema modifications detected specifically within the source branch context.

#### `public List<SchemaChange> TargetSchemaChanges`
A list of schema modifications detected specifically within the target branch context.

#### `public List<ConflictInfo> Conflicts`
A collection of `ConflictInfo` objects detailing any incompatible changes or collision points identified between the two branches.

#### `public DateTime CreatedAt`
The timestamp indicating when this `MigrationDiff` instance was created and populated.

#### `public ComparisonResult Result`
An enumeration value representing the overall outcome of the comparison (e.g., compatible, has conflicts, identical).

#### `public Dictionary<string, object> Summary`
A key-value pair collection containing aggregated metrics or high-level summaries of the diff operation.

#### `public bool IsValid`
A boolean flag indicating whether the current state of the `MigrationDiff` object is consistent and valid for processing.

### Constructors

#### `public MigrationDiff()`
Initializes a new instance of the `MigrationDiff` class with default values. Lists are typically initialized to empty collections, and `CreatedAt` is set to the current time.

#### `public MigrationDiff(string sourceBranchId, string targetBranchId)`
Initializes a new instance of the `MigrationDiff` class, explicitly setting the `SourceBranchId` and `TargetBranchId`. Other properties are initialized to their default states.

### Methods

#### `public void AddSourceOnlyMigration(Migration migration)`
Adds a specific `Migration` object to the `OnlyInSource` list.
*   **Parameters**: `migration` - The migration instance found only in the source.
*   **Throws**: May throw `ArgumentNullException` if `migration` is null.

#### `public void AddTargetOnlyMigration(Migration migration)`
Adds a specific `Migration` object to the `OnlyInTarget` list.
*   **Parameters**: `migration` - The migration instance found only in the target.
*   **Throws**: May throw `ArgumentNullException` if `migration` is null.

#### `public void AddCommonMigration(Migration migration)`
Adds a specific `Migration` object to the `InBoth` list.
*   **Parameters**: `migration` - The migration instance present in both branches.
*   **Throws**: May throw `ArgumentNullException` if `migration` is null.

#### `public void AddConflict(ConflictInfo conflict)`
Adds a `ConflictInfo` object to the `Conflicts` list and typically updates the `Result` status to reflect the presence of conflicts.
*   **Parameters**: `conflict` - The details of the detected conflict.
*   **Throws**: May throw `ArgumentNullException` if `conflict` is null.

#### `public int GetTotalSchemaChanges()`
Calculates and returns the total count of schema changes across both branches.
*   **Returns**: The sum of the counts of `SourceSchemaChanges` and `TargetSchemaChanges`.
*   **Remarks**: This method performs a runtime calculation; it does not return a cached property value.

## Usage

### Example 1: Initializing and Populating a Diff Manually
This example demonstrates creating a diff instance for two specific branches and programmatically adding discovered migrations and conflicts.

```csharp
using EfMigrationDiff;
using System;
using System.Collections.Generic;

public class DiffBuilder
{
    public MigrationDiff BuildDiff(string source, string target)
    {
        var diff = new MigrationDiff(source, target);
        
        // Simulate adding a migration found only in source
        var sourceOnly = new Migration { Id = "20231010_AddUserTable", Name = "AddUserTable" };
        diff.AddSourceOnlyMigration(sourceOnly);

        // Simulate adding a common migration
        var common = new Migration { Id = "20230901_InitialCreate", Name = "InitialCreate" };
        diff.AddCommonMigration(common);

        // Simulate a detected conflict
        var conflict = new ConflictInfo 
        { 
            Description = "Conflicting column types on 'Users.Email'", 
            Severity = ConflictSeverity.High 
        };
        diff.AddConflict(conflict);

        // Validate state
        if (diff.Conflicts.Count > 0)
        {
            Console.WriteLine($"Conflict detected in diff {diff.Id}: {conflict.Description}");
        }

        return diff;
    }
}
```

### Example 2: Analyzing Results and Schema Metrics
This example shows how to consume a populated `MigrationDiff` object to generate a summary report and calculate total schema impact.

```csharp
using System;
using System.Linq;

public class DiffReporter
{
    public void Report(MigrationDiff diff)
    {
        if (!diff.IsValid)
        {
            Console.WriteLine("Invalid diff object. Aborting report.");
            return;
        }

        Console.WriteLine($"Diff Report: {diff.SourceBranchId} vs {diff.TargetBranchId}");
        Console.WriteLine($"Created At: {diff.CreatedAt}");
        Console.WriteLine($"Overall Result: {diff.Result}");

        // Output counts
        Console.WriteLine($"Migrations only in Source: {diff.OnlyInSource.Count}");
        Console.WriteLine($"Migrations only in Target: {diff.OnlyInTarget.Count}");
        Console.WriteLine($"Common Migrations: {diff.InBoth.Count}");

        // Calculate total schema changes using the helper method
        int totalChanges = diff.GetTotalSchemaChanges();
        Console.WriteLine($"Total Schema Changes Detected: {totalChanges}");

        // Access summary data if available
        if (diff.Summary.ContainsKey("ExecutionTimeMs"))
        {
            Console.WriteLine($"Comparison took: {diff.Summary["ExecutionTimeMs"]}ms");
        }
    }
}
```

## Notes

*   **Thread Safety**: The `MigrationDiff` class is not thread-safe. The internal lists (`OnlyInSource`, `Conflicts`, etc.) are standard `List<T>` implementations. Concurrent calls to modification methods like `AddSourceOnlyMigration` or `AddConflict` from multiple threads without external synchronization may result in data corruption or runtime exceptions.
*   **Collection Initialization**: While the public API exposes `List<T>` properties, consumers should treat these as mutable collections owned by the instance. Clearing or reassigning these lists externally may invalidate the `IsValid` flag or disrupt internal consistency checks.
*   **Edge Cases**:
    *   If `GetTotalSchemaChanges()` is called before schema change lists are populated, it will correctly return 0, assuming the internal lists are initialized to empty collections by the constructor.
    *   The `IsValid` property should be checked before relying on the `Result` or `Summary` data, as partial population of the object (e.g., during an interrupted comparison process) may leave the instance in an indeterminate state.
    *   Duplicate entries in migration lists are not automatically prevented by the `Add` methods; deduplication logic, if required, must be handled by the caller or the service populating the `MigrationDiff`.
