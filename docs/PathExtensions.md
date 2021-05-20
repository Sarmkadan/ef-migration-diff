# PathExtensions

`PathExtensions` is a static utility class that provides normalization, conversion, and validation operations for file system paths. It abstracts common path manipulation tasks—such as resolving relative paths, detecting directory containment, and sanitizing file names—into a consistent, predictable API that handles edge cases like trailing separators and cross-platform separators uniformly.

## API

### NormalizePath
```csharp
public static string NormalizePath(string path)
```
Returns a normalized version of the given path by resolving relative segments (`.` and `..`), converting separators to the platform-preferred form, and trimming redundant separators. The input path does not need to exist on disk.  
**Parameters:**  
- `path` — The raw path string to normalize.  
**Returns:** A fully normalized path string.  
**Throws:** `ArgumentNullException` if `path` is `null`. `ArgumentException` if the path contains invalid characters.

### ToAbsolutePath
```csharp
public static string ToAbsolutePath(string path)
```
Converts a potentially relative path into an absolute path using the current working directory as the base. If the input is already absolute, it is normalized and returned unchanged.  
**Parameters:**  
- `path` — The path to resolve.  
**Returns:** An absolute, normalized path string.  
**Throws:** `ArgumentNullException` if `path` is `null`. `ArgumentException` if the path contains invalid characters.

### ToRelativePath
```csharp
public static string ToRelativePath(string path, string relativeTo)
```
Computes a relative path from the `relativeTo` directory to the target `path`. Both inputs are normalized before calculation.  
**Parameters:**  
- `path` — The target path.  
- `relativeTo` — The base directory from which the relative path is computed.  
**Returns:** A relative path string that, when resolved from `relativeTo`, points to `path`.  
**Throws:** `ArgumentNullException` if either argument is `null`. `ArgumentException` if either path contains invalid characters. `InvalidOperationException` if a relative path cannot be computed (e.g., paths on different volumes on Windows).

### IsUnderDirectory
```csharp
public static bool IsUnderDirectory(string path, string directory)
```
Determines whether `path` resides within the `directory` tree. Both inputs are normalized before comparison. The check is purely lexical—no disk access is performed.  
**Parameters:**  
- `path` — The path to test.  
- `directory` — The potential parent directory.  
**Returns:** `true` if `path` is equal to `directory` or is a descendant of it; otherwise `false`.  
**Throws:** `ArgumentNullException` if either argument is `null`.

### EnsureTrailingSeparator
```csharp
public static string EnsureTrailingSeparator(string path)
```
Appends a platform-appropriate directory separator character to the end of the path if one is not already present.  
**Parameters:**  
- `path` — The path to modify.  
**Returns:** The path with a guaranteed trailing separator.  
**Throws:** `ArgumentNullException` if `path` is `null`.

### RemoveTrailingSeparator
```csharp
public static string RemoveTrailingSeparator(string path)
```
Removes a trailing directory separator character from the path if one is present, unless the path consists solely of a root (e.g., `/` or `C:\`), in which case it is returned unchanged.  
**Parameters:**  
- `path` — The path to modify.  
**Returns:** The path without a trailing separator (except for root paths).  
**Throws:** `ArgumentNullException` if `path` is `null`.

### GetCommonDirectory
```csharp
public static string GetCommonDirectory(IEnumerable<string> paths)
```
Finds the longest common ancestor directory shared by all provided paths. Paths are normalized before comparison.  
**Parameters:**  
- `paths` — A collection of path strings.  
**Returns:** The common directory path, or `string.Empty` if no common ancestor exists.  
**Throws:** `ArgumentNullException` if `paths` is `null`. `ArgumentException` if the collection is empty or contains a `null` element.

### CombinePathSafely
```csharp
public static string CombinePathSafely(string basePath, string relativePath)
```
Combines a base path with a relative path, ensuring that the resulting path does not escape the base directory. If the relative path attempts to traverse above the base (via `..` segments), the result is clamped to the base path.  
**Parameters:**  
- `basePath` — The base directory.  
- `relativePath` — The relative path to append.  
**Returns:** A combined path that is guaranteed to be within `basePath`.  
**Throws:** `ArgumentNullException` if either argument is `null`. `ArgumentException` if `basePath` contains invalid characters.

### GetSafeFileName
```csharp
public static string GetSafeFileName(string fileName)
```
Sanitizes a string intended for use as a file name by removing or replacing characters that are invalid on the current platform. Reserved device names (e.g., `CON`, `PRN` on Windows) are also escaped.  
**Parameters:**  
- `fileName` — The proposed file name.  
**Returns:** A sanitized file name string safe for file system use.  
**Throws:** `ArgumentNullException` if `fileName` is `null`.

### LooksLikeDirectory
```csharp
public static bool LooksLikeDirectory(string path)
```
Heuristically determines whether a path likely refers to a directory rather than a file. The decision is based on the presence of a trailing separator or the absence of a file extension in the last segment. No disk access is performed.  
**Parameters:**  
- `path` — The path to inspect.  
**Returns:** `true` if the path appears to represent a directory; otherwise `false`.  
**Throws:** `ArgumentNullException` if `path` is `null`.

## Usage

### Example 1: Safely combining user-supplied relative paths
```csharp
string baseDir = PathExtensions.NormalizePath("/var/app/data/");
string userInput = "../../etc/passwd";

string safePath = PathExtensions.CombinePathSafely(baseDir, userInput);
// safePath is clamped to "/var/app/data", preventing directory traversal

bool looksDir = PathExtensions.LooksLikeDirectory(safePath);
// looksDir is true because safePath has no file extension
```

### Example 2: Computing a relative path and checking containment
```csharp
string projectRoot = PathExtensions.ToAbsolutePath("~/projects/my-app");
string configFile = PathExtensions.ToAbsolutePath("~/projects/my-app/config/settings.json");

string relative = PathExtensions.ToRelativePath(configFile, projectRoot);
// relative is "config/settings.json"

bool isContained = PathExtensions.IsUnderDirectory(configFile, projectRoot);
// isContained is true

string common = PathExtensions.GetCommonDirectory(new[] { configFile, projectRoot });
// common is the normalized projectRoot path
```

## Notes

- All methods operate purely on string representations and do not access the file system. Paths need not exist on disk.
- Normalization uses the platform's preferred directory separator (`\` on Windows, `/` on Unix) but accepts both forms as input.
- `IsUnderDirectory` and `GetCommonDirectory` perform case-sensitive comparisons on Unix and case-insensitive comparisons on Windows, matching the behavior of the underlying file system.
- `CombinePathSafely` prevents path traversal attacks by resolving the combined path and clamping it if it escapes the base directory. It does not throw when traversal is attempted; it silently returns the base path.
- `EnsureTrailingSeparator` and `RemoveTrailingSeparator` treat root paths specially: a root path will never have its trailing separator removed, and adding a separator to a root that already has one is a no-op.
- `GetSafeFileName` replaces invalid characters with underscores and appends an underscore to reserved names. The exact set of reserved names and invalid characters depends on the operating system at runtime.
- All methods are thread-safe. The class maintains no mutable state and performs no I/O operations.
