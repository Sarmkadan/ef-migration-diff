# CacheService

A lightweight in-memory caching component that stores key-value pairs with optional expiration, supporting type-safe access, pattern-based removal, and statistics tracking.

## API

### `CacheService`

Initializes a new instance of the `CacheService` class with default settings.

### `Set<T>(string key, T value, TimeSpan? expiresIn = null)`

Stores a value in the cache under the specified key.

- **Parameters**
  - `key`: The unique identifier for the cached value.
  - `value`: The value to cache.
  - `expiresIn`: Optional duration after which the entry expires. If `null`, the entry does not expire.
- **Throws**
  - `ArgumentNullException`: If `key` is `null`.
  - `ArgumentNullException`: If `value` is `null`.

### `TryGet<T>(string key, out T value)`

Attempts to retrieve a value from the cache by key.

- **Parameters**
  - `key`: The key of the cached value.
  - `value`: When this method returns, contains the cached value if found; otherwise, the default value for type `T`.
- **Returns**
  - `true` if the key was found and the value was retrieved; otherwise, `false`.
- **Throws**
  - `ArgumentNullException`: If `key` is `null`.

### `Get<T>(string key)`

Retrieves a value from the cache by key. Throws if the key is not found.

- **Parameters**
  - `key`: The key of the cached value.
- **Returns**
  - The cached value.
- **Throws**
  - `ArgumentNullException`: If `key` is `null`.
  - `KeyNotFoundException`: If the key does not exist in the cache.

### `GetOrDefault<T>(string key)`

Retrieves a value from the cache by key, or returns the default value if the key is not found.

- **Parameters**
  - `key`: The key of the cached value.
- **Returns**
  - The cached value if found; otherwise, the default value for type `T`.
- **Throws**
  - `ArgumentNullException`: If `key` is `null`.

### `Remove(string key)`

Removes a single entry from the cache by key.

- **Parameters**
  - `key`: The key of the entry to remove.
- **Returns**
  - `true` if the entry was found and removed; otherwise, `false`.
- **Throws**
  - `ArgumentNullException`: If `key` is `null`.

### `RemoveByPattern(string pattern)`

Removes all entries whose keys match the specified regex pattern.

- **Parameters**
  - `pattern`: A regex pattern to match against cache keys.
- **Returns**
  - The number of entries removed.

### `Clear()`

Removes all entries from the cache.

### `RemoveExpiredEntries()`

Removes all expired entries from the cache.

- **Returns**
  - The number of entries removed.

### `GetStatistics()`

Retrieves statistics about the current state of the cache.

- **Returns**
  - A `CacheStatistics` object containing counts of total, valid, and expired entries.

### `Dispose()`

Releases all resources used by the `CacheService` instance.

### `CacheEntry.Value`

Gets the cached value.

- **Type**: `object?`

### `CacheEntry.ExpiresAt`

Gets the absolute expiration date and time of the entry, if set.

- **Type**: `DateTime?`

### `CacheEntry.CreatedAt`

Gets the date and time when the entry was created.

- **Type**: `DateTime`

### `CacheEntry.LastAccessedAt`

Gets the date and time when the entry was last accessed.

- **Type**: `DateTime?`

### `CacheStatistics.TotalEntries`

Gets the total number of entries in the cache, including expired ones.

- **Type**: `int`

### `CacheStatistics.ValidEntries`

Gets the number of entries in the cache that have not expired.

- **Type**: `int`

### `CacheStatistics.ExpiredEntries`

Gets the number of entries in the cache that have expired.

- **Type**: `int`

### `CacheEntry.OldestEntry`

Gets the date and time of the oldest entry in the cache, if any.

- **Type**: `DateTime?`

## Usage
