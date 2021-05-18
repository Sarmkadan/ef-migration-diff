# MigrationRepositoryTests

Unit test class for the `MigrationRepository` class, verifying all CRUD operations, filtering, concurrency, and edge cases. Each test method exercises a specific behavior of the repository to ensure correctness and robustness.

## API

### `Add_WithValidMigration_StoresMigration`
Verifies that a valid migration is successfully stored in the repository.  
**Parameters:** None.  
**Returns:** `void`.  
**Throws:** None (test passes when no exception occurs).

### `Add_WithInvalidMigration_ThrowsException`
Verifies that adding an invalid migration (e.g., missing required fields) causes an exception.  
**Parameters:** None.  
**Returns:** `void`.  
**Throws:** The test expects an exception to be thrown; otherwise the test fails.

### `Add_WithDuplicateId_ThrowsException`
Verifies that adding a migration with an ID that already exists throws an exception (e.g., `InvalidOperationException` or `ArgumentException`).  
**Parameters:** None.  
**Returns:** `void`.  
**Throws:** The test expects an exception to be thrown.

### `GetById_WithValidId_ReturnsMigration`
Verifies that retrieving a migration by a valid ID returns the correct migration object.  
**Parameters:** None.  
**Returns:** `void`.  
**Throws:** None.

### `GetById_WithNonexistentId_ReturnsNull`
Verifies that retrieving a migration by a nonexistent ID returns `null`.  
**Parameters:** None.  
**Returns:** `void`.  
**Throws:** None.

### `GetByDbContext_WithMultipleMigrations_ReturnsOnlyMatchingContext`
Verifies that filtering migrations by a specific DbContext type returns only migrations associated with that context, excluding others.  
**Parameters:** None.  
**Returns:** `void`.  
**Throws:** None.

### `GetByDbContext_WithNonexistentContext_ReturnsEmptyList`
Verifies that filtering by a DbContext type that has no migrations returns an empty list.  
**Parameters:** None.  
**Returns:** `void`.  
**Throws:** None.

### `GetByStatus_WithMigrationsHavingDifferentStatuses_ReturnsFiltered`
Verifies that filtering by a migration status (e.g., `Pending`, `Applied`) returns only migrations with that status.  
**Parameters:** None.  
**Returns:** `void`.  
**Throws:** None.

### `Update_WithExistingMigration_UpdatesContent`
Verifies that updating an existing migration modifies its content (e.g., description, SQL) and persists the change.  
**Parameters:** None.  
**Returns:** `void`.  
**Throws:** None.

### `Update_WithNonexistentMigration_ThrowsException`
Verifies that attempting to update a migration that does not exist throws an exception.  
**Parameters:** None.  
**Returns:** `void`.  
**Throws:** The test expects an exception to be thrown.

### `Delete_WithExistingMigration_RemovesMigration`
Verifies that deleting an existing migration removes it from the repository.  
**Parameters:** None.  
**Returns:** `void`.  
**Throws:** None.

### `Delete_WithNonexistentId_ReturnsFalse`
Verifies that attempting to delete a migration with a nonexistent ID returns `false` (or throws no exception, depending on implementation).  
**Parameters:** None.  
**Returns:** `void`.  
**Throws:** None.

### `GetAll_ReturnsAllAddedMigrations`
Verifies that retrieving all migrations returns every migration that has been added.  
**Parameters:** None.  
**Returns:** `void`.  
**Throws:** None.

### `ConcurrentAdd_WithMultipleThreads_AllMigrationsStored`
Verifies that multiple threads can add migrations concurrently without data loss or corruption.  
**Parameters:** None.  
**Returns:** `void`.  
**Throws:** None (test ensures all migrations are stored after concurrent operations).

### `ConcurrentGet_WithMultipleThreads_ReturnsConsistentResults`
Verifies that multiple threads can read migrations concurrently and always obtain a consistent view of the repository state.  
**Parameters:** None.  
**Returns:** `void`.  
**Throws:** None.

### `Clear_RemovesAllMigrations`
Verifies that clearing the repository removes all stored migrations.  
**Parameters:** None.  
**Returns:** `void`.  
**Throws:** None.

## Usage

The following examples demonstrate how to use `MigrationRepositoryTests` in a test suite. The test class is typically instantiated by a test framework (e.g., xUnit, NUnit) and each test method is run independently.

### Example 1: Basic CRUD verification

```csharp
[TestClass]
public class MigrationRepositoryIntegrationTests
{
    [TestMethod]
    public void AddAndRetrieveMigration()
    {
        // Arrange
        var repository = new MigrationRepository();
        var migration = new Migration { Id = "001", DbContext = "AppDbContext", Status = "Pending" };

        // Act
        repository.Add(migration);
        var retrieved = repository.GetById("001");

        // Assert
        Assert.IsNotNull(retrieved);
        Assert.AreEqual("AppDbContext", retrieved.DbContext);
    }
}
```

### Example 2: Concurrency stress test

```csharp
[TestMethod]
public void ConcurrentAddStressTest()
{
    // Arrange
    var repository = new MigrationRepository();
    var migrations = Enumerable.Range(1, 100).Select(i => new Migration { Id = i.ToString() }).ToList();

    // Act
    Parallel.ForEach(migrations, m => repository.Add(m));

    // Assert
    var all = repository.GetAll();
    Assert.AreEqual(100, all.Count);
}
```

## Notes

- **Duplicate IDs:** The repository enforces unique migration IDs. Attempting to add a migration with an existing ID throws an exception, as verified by `Add_WithDuplicateId_ThrowsException`.
- **Nonexistent references:** Operations on nonexistent IDs (get, update, delete) return `null` or `false` (or throw) depending on the method. The tests confirm the expected behavior.
- **Filtering:** `GetByDbContext` and `GetByStatus` return only migrations matching the given criteria. Empty results are returned when no matches exist.
- **Thread safety:** The concurrent tests (`ConcurrentAdd_WithMultipleThreads_AllMigrationsStored`, `ConcurrentGet_WithMultipleThreads_ReturnsConsistentResults`) indicate that the repository is designed to be thread-safe. Underlying synchronization mechanisms (e.g., locks, concurrent collections) are assumed to be in place to prevent race conditions and ensure consistency.
- **Clearing state:** `Clear` removes all migrations, leaving the repository empty. This is useful for resetting state between tests or after bulk operations.
