# AutoResolverExtensions

The `AutoResolverExtensions` class provides a set of static extension methods and utility functions designed to streamline the detection, analysis, and resolution of Entity Framework Core migration conflicts. It facilitates the integration of automatic conflict resolution strategies into the dependency injection container, identifies candidates suitable for non-interactive merging, and offers detailed diagnostic summaries to assess the safety of merging divergent migration histories.

## API

### `AddMigrationAutoResolver`

Registers the automatic migration conflict resolution services into the specified `IServiceCollection`. This method configures the necessary internal handlers required to detect and resolve compatible migration changes automatically during application startup or tooling execution.

*   **Parameters**:
    *   `services` (`IServiceCollection`): The service collection to which the resolver services are added.
*   **Returns**: `IServiceCollection` — The same service collection instance to allow for method chaining.
*   **Throws**:
    *   `ArgumentNullException`: Thrown if the `services` parameter is null.

### `GetAutoResolvableCandidates`

Analyzes a provided sequence of migration conflicts and filters them to return only those that the system determines can be resolved automatically without manual intervention. This is typically used to separate trivial conflicts (e.g., additive changes) from complex ones requiring user review.

*   **Parameters**:
    *   `conflicts` (`IEnumerable<ConflictInfo>`): The complete list of detected migration conflicts.
*   **Returns**: `IEnumerable<ConflictInfo>` — A subset of the input containing only conflicts deemed safe for automatic resolution.
*   **Throws**:
    *   `ArgumentNullException`: Thrown if the `conflicts` parameter is null.

### `ToDetailedSummary`

Generates a comprehensive textual report describing the state of a specific conflict or a collection of conflicts. The output includes detailed metadata about the divergent operations, affected entities, and the specific nature of the incompatibility, intended for logging or user display.

*   **Parameters**:
    *   `conflict` (`ConflictInfo`): The specific conflict instance to summarize.
*   **Returns**: `string` — A formatted multi-line string containing the detailed analysis.
*   **Throws**:
    *   `ArgumentNullException`: Thrown if the `conflict` parameter is null.

### `GroupUnresolvedByType`

Categorizes a list of unresolved conflicts based on their `ConflictType`. This utility aids in prioritizing resolution efforts by grouping similar issues (e.g., schema mismatches, data seeding conflicts) together.

*   **Parameters**:
    *   `conflicts` (`IEnumerable<ConflictInfo>`): The list of unresolved conflicts to categorize.
*   **Returns**: `IReadOnlyDictionary<ConflictType, List<ConflictInfo>>` — A dictionary where keys are conflict types and values are lists of associated conflicts.
*   **Throws**:
    *   `ArgumentNullException`: Thrown if the `conflicts` parameter is null.

### `IsSafeToMerge`

Evaluates a set of conflicts to determine if the current state of the migration history allows for a safe merge operation. This method returns `true` only if all detected conflicts are either auto-resolvable or represent non-breaking changes that do not compromise data integrity.

*   **Parameters**:
    *   `conflicts` (`IEnumerable<ConflictInfo>`): The collection of conflicts to evaluate.
*   **Returns**: `bool` — `true` if the merge operation is considered safe; otherwise, `false`.
*   **Throws**:
    *   `ArgumentNullException`: Thrown if the `conflicts` parameter is null.

## Usage

### Registering the Auto-Resolver in Dependency Injection

The following example demonstrates how to configure the automatic migration resolver within the application's service collection during startup. This enables the application to attempt automatic resolution of compatible migration differences when the context is initialized.

```csharp
using Microsoft.Extensions.DependencyInjection;
using EfMigrationDiff;

public class Startup
{
    public void ConfigureServices(IServiceCollection services)
    {
        // Register EF Core context
        services.AddDbContext<MyDbContext>(options =>
            options.UseSqlServer(Configuration.GetConnectionString("DefaultConnection")));

        // Enable automatic migration conflict resolution
        services.AddMigrationAutoResolver();

        services.AddControllers();
    }
}
```

### Analyzing and Reporting Conflicts Programmatically

This example illustrates how to retrieve potential conflicts, filter for those that can be resolved automatically, and generate a detailed report for any remaining critical issues before deciding whether to proceed with a merge.

```csharp
using System;
using System.Linq;
using EfMigrationDiff;
using EfMigrationDiff.Models;

public class MigrationAnalyzer
{
    public void AssessMergeSafety(IEnumerable<ConflictInfo> allConflicts)
    {
        if (allConflicts == null || !allConflicts.Any())
        {
            Console.WriteLine("No conflicts detected. Merge is trivial.");
            return;
        }

        // Identify conflicts that can be handled automatically
        var autoCandidates = allConflicts.GetAutoResolvableCandidates();
        
        // Determine remaining manual conflicts
        var manualConflicts = allConflicts.Except(autoCandidates);

        if (manualConflicts.Any())
        {
            // Group remaining issues by type for targeted fixing
            var grouped = manualConflicts.GroupUnresolvedByType();
            
            Console.WriteLine($"Found {manualConflicts.Count()} unresolved conflicts requiring attention:");
            foreach (var group in grouped)
            {
                Console.WriteLine($"- Type: {group.Key} ({group.Value.Count} instances)");
                // Log detailed summary for the first item in each group as a sample
                Console.WriteLine(group.Value.First().ToDetailedSummary());
            }
        }

        // Final safety check
        bool isSafe = allConflicts.IsSafeToMerge();
        if (isSafe)
        {
            Console.WriteLine("Analysis complete: It is safe to proceed with the merge.");
        }
        else
        {
            Console.WriteLine("Analysis complete: Merge aborted due to unsafe conflicts.");
        }
    }
}
```

## Notes

*   **Null Argument Handling**: All public methods in this class strictly enforce argument validation. Passing `null` for any `IEnumerable<ConflictInfo>` or `IServiceCollection` parameter will result in an immediate `ArgumentNullException`. Callers should ensure collections are initialized, even if empty, before invoking these methods.
*   **Thread Safety**: As this class consists entirely of static methods that operate on provided input parameters without maintaining internal mutable state, it is inherently thread-safe. Multiple threads may safely call these methods concurrently provided the input objects themselves are not being modified by other threads during enumeration.
*   **Enumeration Behavior**: Methods accepting `IEnumerable<ConflictInfo>` (such as `GetAutoResolvableCandidates` and `IsSafeToMerge`) may enumerate the source collection multiple times depending on the implementation of the underlying logic. If the input source represents an expensive stream or a deferred execution query, it is recommended to materialize the collection (e.g., via `.ToList()`) before passing it to these extensions to prevent performance degradation or inconsistent results.
*   **Merge Safety Semantics**: The `IsSafeToMerge` method returns `false` if *any* conflict exists that is not classified as auto-resolvable. A return value of `true` guarantees that all present conflicts fall within the defined safety criteria for automatic application, but it does not guarantee that the resulting database schema matches a specific manual expectation beyond the defined resolution rules.
