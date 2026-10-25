#nullable enable

namespace EfMigrationDiff.Caching;

/// <summary>
/// Convenience extension methods for <see cref="CacheService"/>.
/// </summary>
public static class CacheServiceExtensions
{
    /// <summary>
    /// Gets a cached value, or the result of <paramref name="defaultFactory"/> when the key is missing,
    /// expired, or holds a value of a different type. The factory runs only on a miss and its result is not cached.
    /// </summary>
    /// <typeparam name="T">The expected type of the cached value.</typeparam>
    /// <param name="cache">The cache to read from.</param>
    /// <param name="key">The cache key to look up.</param>
    /// <param name="defaultFactory">Produces the fallback value when no valid entry is found.</param>
    /// <returns>The cached value, or the fallback value.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="cache"/>, <paramref name="key"/> or <paramref name="defaultFactory"/> is <see langword="null"/></exception>
    public static T GetOrDefault<T>(this CacheService cache, string key, Func<T> defaultFactory)
    {
        ArgumentNullException.ThrowIfNull(cache);
        ArgumentNullException.ThrowIfNull(defaultFactory);

        return cache.TryGet<T>(key, out var value) ? value! : defaultFactory();
    }
}
