# DbContextRepository

Centralizes access to `DbContextMetadata` records stored in the migration-diff store. It provides CRUD operations and specialized queries to locate contexts by identity, assembly, provider, entity types, and migration state, while maintaining a small in-memory cache of recently scanned items.

## API

### `void Add(DbContextMetadata metadata)`

Persists the supplied metadata record into the backing store. If a record with the same primary key already exists, it is overwritten. Throws `ArgumentNullException` when `metadata` is `null`.

### `DbContextMetadata? GetById(string id)`

Returns the single metadata record whose primary key matches `id`, or `null` if no such record exists. The lookup is performed against the backing store; the in-memory cache is not consulted.

### `DbContextMetadata? GetByName(string name)`

Returns the single metadata record whose logical name matches `name` (case-sensitive), or `null` if no match is found. Throws `ArgumentNullException` when `name` is `null`.

### `List<DbContextMetadata> GetByAssembly(string assemblyName)`

Returns every metadata record whose assembly simple name equals `assemblyName` (case-sensitive). The result is unordered and may be empty.

### `List<DbContextMetadata> GetByProvider(string providerName)`

Returns every metadata record whose provider invariant name equals `providerName` (case-sensitive). The result is unordered and may be empty.

### `void Update(DbContextMetadata metadata)`

Replaces the persisted record identified by `metadata.Id` with the supplied values. Throws `ArgumentNullException` when `metadata` is `null` or when the identifier is missing. Throws `KeyNotFoundException` when no record with the given identifier exists.

### `bool Delete(string id)`

Removes the record whose primary key matches `id`. Returns `true` if a record was found and deleted, otherwise `false`. Throws `ArgumentNullException` when `id` is `null`.

### `List<DbContextMetadata> GetAll()`

Returns an unordered list containing every metadata record in the backing store. The list may be empty.

### `int Count()`

Returns the total number of metadata records currently stored.

### `List<DbContextMetadata> SearchByName(string pattern)`

Returns every metadata record whose logical name contains the substring `pattern` (case-sensitive). The result is unordered and may be empty. Throws `ArgumentNullException` when `pattern` is `null`.

### `List<DbContextMetadata> GetRecentlyScanned(int limit = 10)`

Returns the most recently persisted or updated records, limited by `limit`. The order is descending by internal timestamp; the result may be truncated or empty.

### `List<DbContextMetadata> GetWithMigrations()`

Returns every metadata record that has at least one associated migration entry in the store. The result is unordered and may be empty.

### `List<DbContextMetadata> GetByEntityType(string entityTypeName)`

Returns every metadata record that references the CLR type named `entityTypeName` (case-sensitive, fully qualified or simple). The result is unordered and may be empty.

### `void Clear()`

Removes every metadata record from the backing store. Subsequent calls to retrieval methods will return empty collections until new records are added.

### `bool Exists(string id)`

Returns `true` if a record with primary key `id` exists, otherwise `false`. Throws `ArgumentNullException` when `id` is `null`.

### `List<DbContextMetadata> GetByProviderAndAssembly(string providerName, string assemblyName)`

Returns every metadata record whose provider invariant name equals `providerName` and whose assembly simple name equals `assemblyName` (both case-sensitive). The result is unordered and may be empty.
