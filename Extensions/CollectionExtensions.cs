// =============================================================================
// Author: Vladyslav Zaiets | https://sarmkadan.com
// CTO & Software Architect
// =============================================================================

namespace EfMigrationDiff.Extensions;

/// <summary>
/// Extension methods for IEnumerable types providing LINQ-style operations.
/// Includes null-safe checks, filtering, transformation, and aggregation utilities.
/// </summary>
public static class CollectionExtensions
{
    /// <summary>
    /// Checks if collection is null or empty.
    /// </summary>
    public static bool IsNullOrEmpty<T>(this IEnumerable<T>? collection)
    {
        return collection == null || !collection.Any();
    }

    /// <summary>
    /// Returns the collection if not null/empty, otherwise returns an empty collection.
    /// </summary>
    public static IEnumerable<T> OrEmpty<T>(this IEnumerable<T>? collection)
    {
        return collection ?? Enumerable.Empty<T>();
    }

    /// <summary>
    /// Returns distinct items based on a key selector function.
    /// </summary>
    public static IEnumerable<T> DistinctBy<T, TKey>(this IEnumerable<T> items, Func<T, TKey> keySelector)
    {
        var seen = new HashSet<TKey>();
        foreach (var item in items)
        {
            var key = keySelector(item);
            if (seen.Add(key))
                yield return item;
        }
    }

    /// <summary>
    /// Batches items into groups of specified size.
    /// </summary>
    public static IEnumerable<IEnumerable<T>> Batch<T>(this IEnumerable<T> items, int batchSize)
    {
        if (batchSize <= 0)
            throw new ArgumentException("Batch size must be greater than 0", nameof(batchSize));

        var batch = new List<T>(batchSize);
        foreach (var item in items)
        {
            batch.Add(item);
            if (batch.Count == batchSize)
            {
                yield return batch;
                batch = new List<T>(batchSize);
            }
        }

        if (batch.Count > 0)
            yield return batch;
    }

    /// <summary>
    /// Performs an action on each item in the collection.
    /// </summary>
    public static IEnumerable<T> ForEach<T>(this IEnumerable<T> items, Action<T> action)
    {
        foreach (var item in items)
        {
            action(item);
            yield return item;
        }
    }

    /// <summary>
    /// Chunks collection into groups of specified size. Similar to Batch.
    /// </summary>
    public static List<List<T>> Chunk<T>(this IEnumerable<T> items, int chunkSize)
    {
        if (chunkSize <= 0)
            throw new ArgumentException("Chunk size must be greater than 0", nameof(chunkSize));

        var result = new List<List<T>>();
        var currentChunk = new List<T>(chunkSize);

        foreach (var item in items)
        {
            currentChunk.Add(item);
            if (currentChunk.Count == chunkSize)
            {
                result.Add(currentChunk);
                currentChunk = new List<T>(chunkSize);
            }
        }

        if (currentChunk.Count > 0)
            result.Add(currentChunk);

        return result;
    }

    /// <summary>
    /// Converts IEnumerable to dictionary, using a key selector function.
    /// Throws if duplicate keys exist.
    /// </summary>
    public static Dictionary<TKey, T> ToDict<T, TKey>(this IEnumerable<T> items, Func<T, TKey> keySelector)
        where TKey : notnull
    {
        var dict = new Dictionary<TKey, T>();
        foreach (var item in items)
        {
            var key = keySelector(item);
            if (dict.ContainsKey(key))
                throw new ArgumentException($"Duplicate key: {key}");
            dict[key] = item;
        }
        return dict;
    }

    /// <summary>
    /// Groups items by a key and returns a dictionary.
    /// </summary>
    public static Dictionary<TKey, List<T>> GroupByDict<T, TKey>(this IEnumerable<T> items, Func<T, TKey> keySelector)
        where TKey : notnull
    {
        var dict = new Dictionary<TKey, List<T>>();
        foreach (var item in items)
        {
            var key = keySelector(item);
            if (!dict.ContainsKey(key))
                dict[key] = new List<T>();
            dict[key].Add(item);
        }
        return dict;
    }

    /// <summary>
    /// Returns items that match the predicate, or empty if predicate is null.
    /// </summary>
    public static IEnumerable<T> WhereIf<T>(this IEnumerable<T> items, Func<T, bool>? predicate)
    {
        return predicate == null ? items : items.Where(predicate);
    }

    /// <summary>
    /// Returns first item or a default value if collection is empty.
    /// </summary>
    public static T? FirstOrNull<T>(this IEnumerable<T>? items) where T : class
    {
        return items?.FirstOrDefault();
    }

    /// <summary>
    /// Flattens a collection of collections into a single collection.
    /// </summary>
    public static IEnumerable<T> Flatten<T>(this IEnumerable<IEnumerable<T>> items)
    {
        foreach (var subCollection in items)
        {
            foreach (var item in subCollection)
            {
                yield return item;
            }
        }
    }

    /// <summary>
    /// Returns specified count of items, or fewer if collection has fewer items.
    /// </summary>
    public static IEnumerable<T> TakeSafe<T>(this IEnumerable<T> items, int count)
    {
        return items.Take(Math.Max(0, count));
    }

    /// <summary>
    /// Returns all items except the specified count from the end.
    /// </summary>
    public static IEnumerable<T> SkipLast<T>(this IEnumerable<T> items, int count)
    {
        var list = items.ToList();
        return list.Take(Math.Max(0, list.Count - count));
    }
}
