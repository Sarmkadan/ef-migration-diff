# MigrationRepositoryExtensions

The `MigrationRepositoryExtensions` class provides a set of static extension methods designed to streamline common operations on migration repositories within the `ef-migration-diff` ecosystem. By encapsulating logic for batch additions, status-based filtering, pattern matching, and counting, this utility reduces boilerplate code and ensures consistent interaction patterns when managing database migration records.

## API

### AddRange
Adds a collection of migration entities to the specified repository in a single operation.
*   **Parameters**: Accepts the target repository instance and an `IEnumerable<Migration>` containing the migrations to be added.
*   **Return Value**: Returns an `int` representing the total number of migrations successfully added to the repository.
*   **Exceptions**: Throws an `ArgumentNullException` if the repository or the input collection is null. May throw a repository-specific exception if the underlying storage mechanism fails during the batch operation.

### GetByStatuses
Retrieves a list of migrations that match any of the provided status flags.
*   **Parameters**: Accepts the target repository instance and a collection of status enums or flags to filter against.
*   **Return Value**: Returns a `List<Migration>` containing all entities whose status matches one of the specified criteria. Returns an empty list if no matches are found.
*   **Exceptions**: Throws an `ArgumentNullException` if the repository or the status collection is null.

### SearchByNamePattern
Finds migrations whose names match a specific string pattern, typically supporting wildcard or substring logic depending on the underlying repository implementation.
*   **Parameters**: Accepts the target repository instance and a `string` representing the search pattern.
*   **Return Value**: Returns a `List<Migration>` containing all entities with names matching the provided pattern.
*   **Exceptions**: Throws an `ArgumentNullException` if the repository is null. Throws an `ArgumentException` if the search pattern is null or empty.

### CountByStatus
Calculates the total number of migrations currently held in the repository that possess a specific status.
*   **Parameters**: Accepts the target repository instance and a single status enum or flag to count.
*   **Return Value**: Returns an `int` representing the count of matching migrations. Returns 0 if no migrations match the status.
*   **Exceptions**: Throws an `ArgumentNullException` if the repository is null.

## Usage

The following example demonstrates how to batch add new migrations and subsequently retrieve only those that are pending application.

```csharp
using EfMigrationDiff.Core;
using EfMigrationDiff.Extensions;
using System.Collections.Generic;
using System.Linq;

// Assume 'repository' is an initialized IMigrationRepository instance
var newMigrations = new List<Migration>
{
    new Migration { Name = "AddUsersTable", Status = MigrationStatus.Pending },
    new Migration { Name = "AddOrdersTable", Status = MigrationStatus.Pending },
    new Migration { Name = "FixIndexes", Status = MigrationStatus.Applied }
};

// Add multiple migrations and get the count of added items
int addedCount = repository.AddRange(newMigrations);

// Retrieve only the pending migrations
var pendingMigrations = repository.GetByStatuses(new[] { MigrationStatus.Pending });

Console.WriteLine($"Added {addedCount} migrations. Found {pendingMigrations.Count} pending.");
```

The next example illustrates searching for specific migrations by name pattern and counting the applied ones.

```csharp
using EfMigrationDiff.Core;
using EfMigrationDiff.Extensions;

// Assume 'repository' is an initialized IMigrationRepository instance

// Find all migrations related to "User" updates
var userMigrations = repository.SearchByNamePattern("*User*");

// Count how many migrations have already been applied
int appliedCount = repository.CountByStatus(MigrationStatus.Applied);

foreach (var migration in userMigrations)
{
    Console.WriteLine($"Found user-related migration: {migration.Name}");
}

Console.WriteLine($"Total applied migrations: {appliedCount}");
```

## Notes

*   **Thread Safety**: As these are static extension methods operating on an external repository instance, thread safety is dependent on the implementation of the underlying `IMigrationRepository`. If the repository is not thread-safe, concurrent calls to these methods from multiple threads accessing the same instance may result in race conditions.
*   **Null Handling**: All methods strictly validate input arguments. Passing a null repository instance will consistently result in an `ArgumentNullException`. Callers should ensure the repository is initialized before invoking these extensions.
*   **Empty Results**: Methods returning lists (`GetByStatuses`, `SearchByNamePattern`) will return an empty collection rather than null if no matches are found, preventing null-reference exceptions in calling code.
*   **Pattern Matching**: The behavior of `SearchByNamePattern` (e.g., support for `*` wildcards vs. simple substring containment) relies on the specific regex or string comparison logic implemented in the concrete repository class.
