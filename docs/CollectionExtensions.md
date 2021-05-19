# CollectionExtensions

A utility class providing extension methods for common collection operations, including null-safety, batching, grouping, and conditional filtering. Designed to simplify LINQ-like operations while maintaining readability and performance.

## API

### `IsNullOrEmpty<T>(this IEnumerable<T>? source)`
Determines whether the specified collection is either `null` or empty.

- **Parameters**
  - `source` – The collection to check.
- **Returns**
  - `true` if `source` is `null` or contains no elements; otherwise, `false`.
- **Throws**
  - Never throws.

---

### `OrEmpty<T>(this IEnumerable<T>? source)`
Returns an empty enumerable if the source is `null`; otherwise, returns the source.

- **Parameters**
  - `source` – The collection to evaluate.
- **Returns**
  - An `IEnumerable<T>` that is either the original collection or an empty sequence.
- **Throws**
  - Never throws.

---

### `DistinctBy<T, TKey>(this IEnumerable<T> source, Func<T, TKey> keySelector)`
Returns distinct elements from the collection based on a key selector function.

- **Parameters**
  - `source` – The collection to process.
  - `keySelector` – A function to extract the key for each element.
- **Returns**
  - An `IEnumerable<T>` containing distinct elements.
- **Throws**
  - Throws `ArgumentNullException` if `source` or `keySelector` is `null`.

---

### `Batch<T>(this IEnumerable<T> source, int size)`
Splits the source collection into batches of the specified size.

- **Parameters**
  - `source` – The collection to batch.
  - `size` – The maximum number of elements per batch.
- **Returns**
  - An `IEnumerable<IEnumerable<T>>` of batches.
- **Throws**
  - Throws `ArgumentNullException` if `source` is `null`.
  - Throws `ArgumentOutOfRangeException` if `size` is less than 1.

---

### `ForEach<T>(this IEnumerable<T> source, Action<T> action)`
Invokes the specified action on each element of the collection.

- **Parameters**
  - `source` – The collection to iterate.
  - `action` – The action to perform on each element.
- **Returns**
  - The original `source` for method chaining.
- **Throws**
  - Throws `ArgumentNullException` if `source` or `action` is `null`.

---

### `Chunk<T>(this IEnumerable<T> source, int size)`
Splits the source collection into chunks of the specified size, returning a list of lists.

- **Parameters**
  - `source` – The collection to chunk.
  - `size` – The maximum number of elements per chunk.
- **Returns**
  - A `List<List<T>>` of chunks.
- **Throws**
  - Throws `ArgumentNullException` if `source` is `null`.
  - Throws `ArgumentOutOfRangeException` if `size` is less than 1.

---

### `ToDict<T, TKey>(this IEnumerable<T> source, Func<T, TKey> keySelector)`
Converts the collection into a dictionary using the specified key selector.

- **Parameters**
  - `source` – The collection to convert.
  - `keySelector` – A function to extract the key for each element.
- **Returns**
  - A `Dictionary<TKey, T>` mapping keys to elements.
- **Throws**
  - Throws `ArgumentNullException` if `source` or `keySelector` is `null`.
  - Throws `ArgumentException` if duplicate keys are encountered.

---
### `GroupByDict<T, TKey>(this IEnumerable<T> source, Func<T, TKey> keySelector)`
Groups elements of the collection into a dictionary where each key maps to a list of values.

- **Parameters**
  - `source` – The collection to group.
  - `keySelector` – A function to extract the key for each element.
- **Returns**
  - A `Dictionary<TKey, List<T>>` mapping keys to grouped elements.
- **Throws**
  - Throws `ArgumentNullException` if `source` or `keySelector` is `null`.

---
### `WhereIf<T>(this IEnumerable<T> source, bool condition, Func<T, bool> predicate)`
Applies the predicate to the collection only if the condition is `true`.

- **Parameters**
  - `source` – The collection to filter.
  - `condition` – Determines whether to apply the predicate.
  - `predicate` – The filtering function.
- **Returns**
  - An `IEnumerable<T>` that is either filtered or unchanged.
- **Throws**
  - Throws `ArgumentNullException` if `source` or `predicate` is `null`.

---
### `FirstOrNull<T>(this IEnumerable<T> source)`
Returns the first element of the collection or `null` if the collection is empty or `null`.

- **Parameters**
  - `source` – The collection to evaluate.
- **Returns**
  - The first element or `null`.
- **Throws**
  - Never throws.

---
### `Flatten<T>(this IEnumerable<IEnumerable<T>> source)`
Flattens a collection of collections into a single enumerable.

- **Parameters**
  - `source` – The nested collections to flatten.
- **Returns**
  - An `IEnumerable<T>` containing all elements from all sub-collections.
- **Throws**
  - Throws `ArgumentNullException` if `source` is `null`.

---
### `TakeSafe<T>(this IEnumerable<T> source, int count)`
Returns a specified number of contiguous elements from the start of the collection, or all elements if the count exceeds the collection size.

- **Parameters**
  - `source` – The collection to take from.
  - `count` – The number of elements to take.
- **Returns**
  - An `IEnumerable<T>` containing up to `count` elements.
- **Throws**
  - Throws `ArgumentNullException` if `source` is `null`.
  - Throws `ArgumentOutOfRangeException` if `count` is less than 0.

---
### `SkipLast<T>(this IEnumerable<T> source, int count)`
Returns a new enumerable that skips the last `count` elements of the source.

- **Parameters**
  - `source` – The collection to skip from.
  - `count` – The number of elements to skip.
- **Returns**
  - An `IEnumerable<T>` containing all but the last `count` elements.
- **Throws**
  - Throws `ArgumentNullException` if `source` is `null`.
  - Throws `ArgumentOutOfRangeException` if `count` is less than 0.

## Usage
