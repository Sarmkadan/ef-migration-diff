// =============================================================================
// Author: Vladyslav Zaiets | https://sarmkadan.com
// CTO & Software Architect
// =============================================================================

namespace EfMigrationDiff.Caching;

/// <summary>
/// In-memory caching service with expiration support.
/// Stores cached values with optional TTL, supports generic types, and provides cache statistics.
/// Thread-safe for concurrent access.
/// </summary>
public class CacheService : IDisposable
{
    private readonly Dictionary<string, CacheEntry> _cache = new();
    private readonly ReaderWriterLockSlim _lock = new();
    private Timer? _cleanupTimer;

    public CacheService(TimeSpan? cleanupInterval = null)
    {
        // Start cleanup timer if specified
        if (cleanupInterval.HasValue && cleanupInterval.Value.TotalSeconds > 0)
        {
            _cleanupTimer = new Timer(
                _ => RemoveExpiredEntries(),
                null,
                cleanupInterval.Value,
                cleanupInterval.Value);
        }
    }

    /// <summary>
    /// Sets a value in cache with optional expiration.
    /// </summary>
    public void Set<T>(string key, T value, TimeSpan? expiration = null)
    {
        if (string.IsNullOrEmpty(key))
            throw new ArgumentException("Cache key cannot be null or empty", nameof(key));

        _lock.EnterWriteLock();
        try
        {
            _cache[key] = new CacheEntry
            {
                Value = value,
                ExpiresAt = expiration.HasValue ? DateTime.UtcNow.Add(expiration.Value) : null,
                CreatedAt = DateTime.UtcNow
            };
        }
        finally
        {
            _lock.ExitWriteLock();
        }
    }

    /// <summary>
    /// Tries to get a value from cache. Returns false if key not found or expired.
    /// </summary>
    public bool TryGet<T>(string key, out T? value)
    {
        value = default;

        if (string.IsNullOrEmpty(key))
            return false;

        _lock.EnterReadLock();
        try
        {
            if (_cache.TryGetValue(key, out var entry))
            {
                if (entry.IsExpired)
                {
                    // Exit read lock and acquire write lock to remove expired entry
                    _lock.ExitReadLock();
                    _lock.EnterWriteLock();
                    try
                    {
                        _cache.Remove(key);
                    }
                    finally
                    {
                        _lock.ExitWriteLock();
                    }
                    return false;
                }

                if (entry.Value is T typedValue)
                {
                    value = typedValue;
                    entry.LastAccessedAt = DateTime.UtcNow;
                    return true;
                }
            }

            return false;
        }
        finally
        {
            _lock.ExitReadLock();
        }
    }

    /// <summary>
    /// Gets a value from cache, throws if not found.
    /// </summary>
    public T Get<T>(string key)
    {
        if (TryGet(key, out var value))
            return value!;

        throw new KeyNotFoundException($"Cache key not found: {key}");
    }

    /// <summary>
    /// Gets a value or default if not found.
    /// </summary>
    public T? GetOrDefault<T>(string key, T? defaultValue = default)
    {
        return TryGet(key, out var value) ? value : defaultValue;
    }

    /// <summary>
    /// Removes a key from cache.
    /// </summary>
    public bool Remove(string key)
    {
        if (string.IsNullOrEmpty(key))
            return false;

        _lock.EnterWriteLock();
        try
        {
            return _cache.Remove(key);
        }
        finally
        {
            _lock.ExitWriteLock();
        }
    }

    /// <summary>
    /// Removes all keys matching a pattern.
    /// </summary>
    public int RemoveByPattern(string pattern)
    {
        _lock.EnterWriteLock();
        try
        {
            var keys = _cache.Keys.Where(k => k.Contains(pattern, StringComparison.OrdinalIgnoreCase)).ToList();
            foreach (var key in keys)
                _cache.Remove(key);
            return keys.Count;
        }
        finally
        {
            _lock.ExitWriteLock();
        }
    }

    /// <summary>
    /// Clears all cache entries.
    /// </summary>
    public void Clear()
    {
        _lock.EnterWriteLock();
        try
        {
            _cache.Clear();
        }
        finally
        {
            _lock.ExitWriteLock();
        }
    }

    /// <summary>
    /// Removes all expired entries from cache.
    /// </summary>
    public int RemoveExpiredEntries()
    {
        _lock.EnterWriteLock();
        try
        {
            var expiredKeys = _cache
                .Where(kv => kv.Value.IsExpired)
                .Select(kv => kv.Key)
                .ToList();

            foreach (var key in expiredKeys)
                _cache.Remove(key);

            return expiredKeys.Count;
        }
        finally
        {
            _lock.ExitWriteLock();
        }
    }

    /// <summary>
    /// Gets cache statistics.
    /// </summary>
    public CacheStatistics GetStatistics()
    {
        _lock.EnterReadLock();
        try
        {
            var validEntries = _cache.Values.Where(e => !e.IsExpired).ToList();

            return new CacheStatistics
            {
                TotalEntries = _cache.Count,
                ValidEntries = validEntries.Count,
                ExpiredEntries = _cache.Count - validEntries.Count,
                OldestEntry = _cache.Values.OrderBy(e => e.CreatedAt).FirstOrDefault()?.CreatedAt
            };
        }
        finally
        {
            _lock.ExitReadLock();
        }
    }

    public void Dispose()
    {
        _cleanupTimer?.Dispose();
        _lock?.Dispose();
    }

    /// <summary>
    /// Internal cache entry structure.
    /// </summary>
    private class CacheEntry
    {
        public object? Value { get; set; }
        public DateTime? ExpiresAt { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime? LastAccessedAt { get; set; }

        public bool IsExpired => ExpiresAt.HasValue && DateTime.UtcNow > ExpiresAt.Value;
    }
}

/// <summary>
/// Statistics about cache performance.
/// </summary>
public class CacheStatistics
{
    public int TotalEntries { get; set; }
    public int ValidEntries { get; set; }
    public int ExpiredEntries { get; set; }
    public DateTime? OldestEntry { get; set; }
}
