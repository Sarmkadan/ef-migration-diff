# FileHelper

Utility class providing common file system operations for working with migration files in EF Core migration workflows. It offers asynchronous and synchronous methods for reading, writing, copying, and managing migration files and directories with additional helpers for size formatting and path manipulation.

## API

### `public static string? ReadFileAsync(string path)`

Reads the entire content of a text file asynchronously.

- **Parameters**
  - `path`: The file path to read.
- **Return value**
  - The file content as a string, or `null` if the file does not exist.
- **Exceptions**
  - Throws `ArgumentNullException` if `path` is `null`.
  - Throws `IOException` if the file cannot be read.
  - Throws `UnauthorizedAccessException` if access is denied.

---

### `public static void WriteFile(string path, string content)`

Writes text content to a file, overwriting if it exists.

- **Parameters**
  - `path`: The file path to write to.
  - `content`: The text content to write.
- **Exceptions**
  - Throws `ArgumentNullException` if `path` or `content` is `null`.
  - Throws `IOException` if the file cannot be written.
  - Throws `UnauthorizedAccessException` if access is denied.

---

### `public static List<string> GetMigrationFiles(string directory)`

Returns a sorted list of migration file names in the specified directory.

- **Parameters**
  - `directory`: The directory path to scan.
- **Return value**
  - A sorted list of migration file names (e.g., `20231001120000_AddUserTable.cs`).
- **Exceptions**
  - Throws `ArgumentNullException` if `directory` is `null`.
  - Throws `DirectoryNotFoundException` if the directory does not exist.
  - Throws `UnauthorizedAccessException` if access is denied.

---

### `public static bool IsValidMigrationDirectory(string directory)`

Checks whether a directory is a valid EF Core migrations directory.

- **Parameters**
  - `directory`: The directory path to validate.
- **Return value**
  - `true` if the directory exists and contains migration files; otherwise, `false`.
- **Exceptions**
  - Throws `ArgumentNullException` if `directory` is `null`.

---

### `public static long GetFileSize(string path)`

Returns the size of a file in bytes.

- **Parameters**
  - `path`: The file path.
- **Return value**
  - The file size in bytes, or `-1` if the file does not exist.
- **Exceptions**
  - Throws `ArgumentNullException` if `path` is `null`.
  - Throws `UnauthorizedAccessException` if access is denied.

---
### `public static string GetHumanReadableFileSize(string path)`

Returns a human-readable file size (e.g., "1.2 KB").

- **Parameters**
  - `path`: The file path.
- **Return value**
  - A formatted size string, or `"0 B"` if the file does not exist.
- **Exceptions**
  - Throws `ArgumentNullException` if `path` is `null`.
  - Throws `UnauthorizedAccessException` if access is denied.

---
### `public static void EnsureDirectoryExists(string path)`

Ensures that a directory exists, creating it if necessary.

- **Parameters**
  - `path`: The directory path to ensure.
- **Exceptions**
  - Throws `ArgumentNullException` if `path` is `null`.
  - Throws `IOException` if the directory cannot be created.
  - Throws `UnauthorizedAccessException` if access is denied.

---
### `public static List<string> GetSubdirectories(string directory)`

Returns a sorted list of subdirectory names in the specified directory.

- **Parameters**
  - `directory`: The parent directory path.
- **Return value**
  - A sorted list of subdirectory names.
- **Exceptions**
  - Throws `ArgumentNullException` if `directory` is `null`.
  - Throws `DirectoryNotFoundException` if the directory does not exist.
  - Throws `UnauthorizedAccessException` if access is denied.

---
### `public static bool DeleteFile(string path)`

Deletes a file if it exists.

- **Parameters**
  - `path`: The file path to delete.
- **Return value**
  - `true` if the file was deleted or did not exist; otherwise, `false`.
- **Exceptions**
  - Throws `ArgumentNullException` if `path` is `null`.
  - Throws `IOException` if the file cannot be deleted.
  - Throws `UnauthorizedAccessException` if access is denied.

---
### `public static void CopyFile(string sourcePath, string destinationPath)`

Copies a file from source to destination, overwriting if it exists.

- **Parameters**
  - `sourcePath`: The source file path.
  - `destinationPath`: The destination file path.
- **Exceptions**
  - Throws `ArgumentNullException` if either path is `null`.
  - Throws `FileNotFoundException` if the source file does not exist.
  - Throws `IOException` if the copy fails.
  - Throws `UnauthorizedAccessException` if access is denied.

---
### `public static DateTime GetLastModifiedTime(string path)`

Returns the last modified time of a file.

- **Parameters**
  - `path`: The file path.
- **Return value**
  - The last modified time, or `DateTime.MinValue` if the file does not exist.
- **Exceptions**
  - Throws `ArgumentNullException` if `path` is `null`.
  - Throws `UnauthorizedAccessException` if access is denied.

---
### `public static string CombinePath(params string[] paths)`

Combines multiple path segments into a single path.

- **Parameters**
  - `paths`: The path segments to combine.
- **Return value**
  - The combined path.
- **Exceptions**
  - Throws `ArgumentNullException` if `paths` is `null` or contains a `null` element.

---
### `public static string GetRelativePath(string relativeTo, string path)`

Computes the relative path from one path to another.

- **Parameters**
  - `relativeTo`: The base path.
  - `path`: The target path.
- **Return value**
  - The relative path from `relativeTo` to `path`.
- **Exceptions**
  - Throws `ArgumentNullException` if either parameter is `null`.

## Usage
