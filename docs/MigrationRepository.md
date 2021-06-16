# MigrationRepository

The `MigrationRepository` provides an in‑memory collection for managing `Migration` entities associated with Entity Framework Core migrations. It supports basic CRUD operations, filtering, pagination, and querying by various criteria such as DbContext, status, date range, and name.

## API

### `public void Add(Migration migration)`
Adds a new migration to the repository.  
**Parameters**  
- `migration`: The migration instance to add.  
**Return value**  
- None.  
**Exceptions**  
- `ArgumentNullException` if `migration` is `null`.  
- `InvalidOperationException` if a migration with the same identifier already exists.

### `public Migration? GetById(Guid id)`
Retrieves a migration by its unique identifier.  
**Parameters**  
- `id`: The `Guid` of the migration to fetch.  
**Return value**  
- The matching `Migration` instance, or `null` if no migration with the specified id exists.  
**Exceptions**  
- None.

### `public List<Migration> GetByDbContext(string dbContextName)`
Returns all migrations associated with a specific DbContext.  
**Parameters**  
- `dbContextName`: The name of the DbContext (as used in the migration history table).  
**Return value**  
- A list of `Migration` objects for the given DbContext; empty list if none are found.  
**Exceptions**  
- `ArgumentNullException` if `dbContextName` is `null`.  
- `ArgumentException` if `dbContextName` is empty or whitespace.

### `public List<Migration> GetByStatus(MigrationStatus status)`
Filters migrations by their current status (e.g., Applied, Pending).  
**Parameters**  
- `status`: The `MigrationStatus` enum value to match.  
**Return value**  
- List of migrations having the specified status; empty list if none match.  
**Exceptions**  
- None.

### `public void Update(Migration migration)`
Updates an existing migration entry with the values from the supplied instance.  
**Parameters**  
- `migration`: The migration containing updated data; its `Id` must correspond to an existing entry.  
**Return value**  
- None.  
**Exceptions**  
- `ArgumentNullException` if `migration` is `null`.  
- `InvalidOperationException` if no migration with the given `Id` exists.

### `public bool Delete(Guid id)`
Removes a migration from the repository.  
**Parameters**  
- `id`: The `Guid` of the migration to delete.  
**Return value**  
- `true` if a migration was found and removed; `false` if no migration with that id exists.  
**Exceptions**  
- None.

### `public List<Migration> GetAll()`
Retrieves every migration stored in the repository.  
**Parameters**  
- None.  
**Return value**  
- A list containing all migrations; empty list if the repository contains none.  
**Exceptions**  
- None.

### `public List<Migration> GetPaginated(int pageIndex, int pageSize)`
Returns a subset of migrations for paging scenarios.  
**Parameters**  
- `pageIndex`: Zero‑based index of the page to retrieve.  
- `pageSize`: Maximum number of migrations per page.  
**Return value**  
- List of migrations for the requested page; empty list if the index is out of range.  
**Exceptions**  
- `ArgumentOutOfRangeException` if `pageIndex` is negative or `pageSize` is less than or equal to zero.

### `public List<Migration> SearchByName(string nameFragment)`
Finds migrations whose `MigrationId` (or name) contains the supplied fragment, case‑insensitively.  
**Parameters**  
- `nameFragment`: The string to search for within migration names.  
**Return value**  
- List of matching migrations; empty list if no matches are found.  
**Exceptions**  
- `ArgumentNullException` if `nameFragment` is `null`.  
- `ArgumentException` if `nameFragment` is empty.

### `public int Count()`
Gets the total number of migrations currently stored.  
**Parameters**  
- None.  
**Return value**  
- Integer count of migrations.  
**Exceptions**  
- None.

### `public List<Migration> GetByDbContexts(IEnumerable<string> dbContextNames)`
Returns migrations that belong to any of the supplied DbContext names.  
**Parameters**  
- `dbContextNames`: Collection of DbContext names to match.  
**Return value**  
- List of migrations for the specified DbContexts; empty list if none match or the input collection is empty.  
**Exceptions**  
- `ArgumentNullException` if `dbContextNames` is `null`.  
- `ArgumentException` if any element in the collection is `null`, empty, or whitespace.

### `public List<Migration> GetByDateRange(DateTime start, DateTime end)`
Filters migrations whose `AppliedOn` timestamp falls within the inclusive range `[start, end]`.  
**Parameters**  
- `start`: Lower bound of the date range.  
- `end`: Upper bound of the date range.  
**Return value**  
- List of migrations applied within the range; empty list if none match.  
**Exceptions**  
- `ArgumentOutOfRangeException` if `start` is after `end`.

### `public void Clear()`
Removes all migrations from the repository.  
**Parameters**  
- None.  
**Return value**  
- None.  
**Exceptions**  
- None.

### `public bool Exists(Guid id)`
Checks whether a migration with the given identifier is present.  
**Parameters**  
- `id`: The `Guid` to test.  
**Return value**  
- `true` if a migration with that id exists; otherwise `false`.  
**Exceptions**  
- None.

### `public Migration? GetLatestByDbContext(string dbContextName)`
Returns the most recently applied migration for a specific DbContext, based on the `AppliedOn` timestamp.  
**Parameters**  
- `dbContextName`: The DbContext name to query.  
**Return value**  
- The latest `Migration` for the DbContext, or `null` if no migrations exist for that context.  
**Exceptions**  
- `ArgumentNullException` if `dbContextName` is `null`.  
- `ArgumentException` if `dbContextName` is empty or whitespace.

## Usage

### Example 1: Adding and retrieving a migration
```csharp
var repo = new MigrationRepository();

var migration = new Migration
{
    Id = Guid.NewGuid(),
    MigrationId = "20230815_Init",
    DbContext = "AppDbContext",
    AppliedOn = DateTime.UtcNow,
    Status = MigrationStatus.Applied
};

repo.Add(migration);

var fetched = repo.GetById(migration.Id);
if (fetched != null)
{
    Console.WriteLine($"Migration {fetched.MigrationId} retrieved.");
}
```

### Example 2: Querying migrations with pagination and filtering
```csharp
var repo = new MigrationRepository();
// Assume repo is pre‑populated with data.

// Get all pending migrations for a specific DbContext, newest first.
var pending = repo.GetByDbContext("AppDbContext")
                  .Where(m => m.Status == MigrationStatus.Pending)
                  .OrderByDescending(m => m.AppliedOn)
                  .ToList();

// Retrieve the second page, 10 items per page.
var page = repo.GetPaginated(pageIndex: 1, pageSize: 10)
               .Where(m => m.Status == MigrationStatus.Pending)
               .ToList();

Console.WriteLine($"Pending migrations: {pending.Count}");
Console.WriteLine($"Page 2 items: {page.Count}");
```

## Notes

- The repository does **not** synchronize access to its internal collection. Concurrent calls from multiple threads may result in race conditions; external locking (e.g., `lock` statements) is required for thread‑safe usage.  
- Methods that accept string parameters (`GetByDbContext`, `GetByDbContexts`, `GetLatestByDbContext`, `SearchByName`) treat `null` or whitespace input as invalid and will throw `ArgumentException` or `ArgumentNullException`.  
- `GetByDateRange` expects the `start` date to be earlier than or equal to the `end` date; otherwise an `ArgumentOutOfRangeException` is thrown.  
- `Clear` removes all entries but does not dispose of any managed resources held by individual `Migration` objects; callers remain responsible for cleaning up any external references.  
- The `Count` property reflects the number of migrations currently stored and is updated automatically by `Add`, `Delete`, `Update`, and `Clear`.  
- `GetById`, `Exists`, and `GetLatestByDbContext` return `null` when no matching migration is found; callers should check for `null` before dereferencing the result.  
- Update operations require the migration’s `Id` to match an existing entry; supplying a migration with a non‑existent identifier results in an `InvalidOperationException`.  
- The repository is intended for in‑memory scenarios such as testing or lightweight tooling; it does not persist data to a database.
