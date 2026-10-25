# CustomExceptions

`Exceptions/CustomExceptions.cs` defines the application-specific exception hierarchy for `ef-migration-diff`. All types derive from `EfMigrationDiffException`, which is the single base type that `Program.cs` catches to report errors. This document lists every type in that file, what it is for, and where the code actually throws it.

Each constructor throws `ArgumentNullException` when a non-optional argument is `null`. That is not repeated for each constructor below.

## Hierarchy

```
Exception
└── EfMigrationDiffException
    ├── RepositoryException
    │   └── GitRepositoryException
    ├── MigrationParsingException
    ├── MigrationConflictException
    ├── BranchNotFoundException
    ├── InvalidMigrationException
    ├── DbContextNotFoundException
    ├── ConfigurationException
    ├── ConfigurationValidationException
    ├── ValidationException
    ├── FileOperationException
    └── ConfigFileException
```

## Summary

| Type | Base | Purpose | Thrown from (production code) | Caught by |
|---|---|---|---|---|
| `EfMigrationDiffException` | `Exception` | Root of the hierarchy | Not thrown directly | `Program.cs` (exit code 1) |
| `RepositoryException` | `EfMigrationDiffException` | Repository operation failed | `Program.cs:238` | `Program.cs` (exit code 1) |
| `GitRepositoryException` | `RepositoryException` | Git repository operation failed | `Program.cs:424` | `Program.cs` (exit code 1) |
| `MigrationParsingException` | `EfMigrationDiffException` | Migration file cannot be parsed | Not thrown yet | `Program.cs` (exit code 1) |
| `MigrationConflictException` | `EfMigrationDiffException` | Migration conflicts detected | Not thrown yet | `Program.cs` (exit code 1) |
| `BranchNotFoundException` | `EfMigrationDiffException` | Branch cannot be resolved | `Program.cs:429-430` | `Program.cs` (exit code 1) |
| `InvalidMigrationException` | `EfMigrationDiffException` | Migration is invalid or malformed | Not thrown yet | `Program.cs` (exit code 1) |
| `DbContextNotFoundException` | `EfMigrationDiffException` | Named DbContext not found | Not thrown yet | `Program.cs` (exit code 1) |
| `ConfigurationException` | `EfMigrationDiffException` | Application configuration is invalid | Not thrown yet | `Program.cs` (exit code 1) |
| `ConfigurationValidationException` | `EfMigrationDiffException` | One or more configuration rules violated, all reported together | `Configuration/DiffOptions.cs:76`, `Configuration/SqlGeneratorOptions.cs:100` | `Program.cs` (exit code `UsageError`) |
| `ValidationException` | `EfMigrationDiffException` | Validation failed | `Configuration/AppSettings.cs:169` | `Program.cs` (exit code 1) |
| `FileOperationException` | `EfMigrationDiffException` | File or directory operation failed | `Utilities/FileHelper.cs`, `Services/MigrationParserService.cs:178` | `Program.cs` (exit code 1) |
| `ConfigFileException` | `EfMigrationDiffException` | Config file cannot be read or is invalid | `Configuration/ConfigFileLoader.cs` | `Program.cs` (exit code 1) |

"Caught by `Program.cs`" means the exception reaches the generic `catch (EfMigrationDiffException)` handler, which prints `Error: <message>` and exits with code 1. `ConfigurationValidationException` has its own handler, which exits with `Constants.ExitCodes.UsageError`. It is also caught at startup (`Program.cs:16`), around the `MigrationDiffApplication` constructor, where it prints `Configuration error: ...` and returns the same exit code.

---

## Types

### `EfMigrationDiffException`

Base class for all application errors. Catch this type to handle any error the tool raised on purpose.

- **Constructors**
  - `EfMigrationDiffException(string message)`
  - `EfMigrationDiffException(string message, Exception innerException)`
- **Thrown when**: Never thrown directly. Use a derived type.
- **Extension helpers**: `Exceptions/EfMigrationDiffExceptionExtensions.cs` walks the `InnerException` chain of this type. It has `FormatDetailedMessage`, `GetRootCause` and `HasMigrationConflict`.

---

### `RepositoryException`

Base type for failures in the repository layer.

- **Constructors**
  - `RepositoryException(string message)`
  - `RepositoryException(string message, Exception innerException)`
- **Thrown when**: The migrations directory for a run does not exist. `Program.cs:238` throws `RepositoryException` with the message `Migrations directory not found: <path>`.
- **Subtypes**: `GitRepositoryException`.

---

### `GitRepositoryException`

A git repository could not be opened or read. Derives from `RepositoryException`, so a `catch (RepositoryException)` also catches it.

- **Properties**
  - `string? RepositoryPath`: path of the repository that failed.
- **Constructors**
  - `GitRepositoryException(string message)`
  - `GitRepositoryException(string message, string repositoryPath)`: sets `RepositoryPath`.
  - `GitRepositoryException(string message, Exception innerException)`
- **Thrown when**: Repository initialization fails. `Program.cs:424` throws it with the message `Failed to initialize git repository` and `_appSettings.RepositoryPath`.

---

### `MigrationParsingException`

A migration source file could not be parsed. This is the type for the regex-based parser in `Services/MigrationParserService.cs`.

- **Properties**
  - `string? FilePath`: file that failed to parse.
  - `int? LineNumber`: line where parsing failed. Only set by the three-argument constructor.
- **Constructors**
  - `MigrationParsingException(string message)`
  - `MigrationParsingException(string message, string filePath)`: sets `FilePath`.
  - `MigrationParsingException(string message, string filePath, int lineNumber)`: sets `FilePath` and `LineNumber`.
- **Thrown when**: Currently not thrown anywhere in the repository. The parser reports problems through other paths. The type exists so that a parse failure can carry the file and line.

---

### `MigrationConflictException`

Two or more migrations conflict, for example when two branches add migrations that the tool cannot merge.

- **Properties**
  - `List<string> ConflictingMigrations`: identifiers of the migrations involved. Defaults to an empty list.
- **Constructors**
  - `MigrationConflictException(string message)`
  - `MigrationConflictException(string message, List<string> conflicts)`: sets `ConflictingMigrations`.
- **Thrown when**: Currently not constructed anywhere in the code. The conflict detector (`Services/ConflictDetectionService.cs`) returns conflict data and does not throw. `EfMigrationDiffExceptionExtensions.HasMigrationConflict` checks for this type in an exception chain, so callers can detect it if it is thrown in the future.

---

### `BranchNotFoundException`

A branch name given on the command line does not resolve to a branch in the repository.

- **Properties**
  - `string? BranchName`: the branch name that was not found.
- **Constructors**
  - `BranchNotFoundException(string branchName)`: the message is generated as `Branch '<name>' not found`.
- **Thrown when**: The source or target branch of a diff cannot be resolved. `Program.cs:429` throws it for the source branch and `Program.cs:430` for the target branch.

---

### `InvalidMigrationException`

A migration is invalid or malformed. Used for migrations that parse but break a rule.

- **Properties**
  - `string? MigrationId`: the invalid migration.
  - `List<string> ValidationErrors`: the individual problems. Defaults to an empty list.
- **Constructors**
  - `InvalidMigrationException(string message)`
  - `InvalidMigrationException(string migrationId, List<string> errors)`: the message is generated as `Migration <id> is invalid`. Sets `MigrationId` and `ValidationErrors`.
- **Thrown when**: Currently not thrown anywhere in the repository.

---

### `DbContextNotFoundException`

The DbContext named by the user does not exist in the scanned code.

- **Properties**
  - `string? ContextName`: the DbContext name that was not found.
- **Constructors**
  - `DbContextNotFoundException(string contextName)`: the message is generated as `DbContext '<name>' not found`.
- **Thrown when**: Currently not thrown anywhere in the repository. `Repositories/DbContextRepository.cs` does not use it.

---

### `ConfigurationException`

Application configuration is invalid. This is the older single-message type. Newer validation code uses `ConfigurationValidationException`.

- **Properties**
  - `List<string> ValidationErrors`: the individual problems. Defaults to an empty list.
- **Constructors**
  - `ConfigurationException(string message)`
  - `ConfigurationException(List<string> errors)`: the message is always `Configuration validation failed`. Sets `ValidationErrors`.
- **Thrown when**: Currently not thrown anywhere in the repository. Use `ConfigurationValidationException` for new configuration checks.

---

### `ConfigurationValidationException`

One or more configuration values violate their rules. All violations are collected first and then reported in a single exception.

- **Properties**
  - `IReadOnlyList<string> Violations`: every violation, in the order they were found.
- **Constructors**
  - `ConfigurationValidationException(IReadOnlyList<string> violations)`: the message lists the count and each violation on its own line, indented with `  - `.
- **Thrown when**: Options fail validation before a command runs.
  - `Configuration/DiffOptions.cs:76` (`ValidateAndThrow`). `Program.cs` calls it once for each diff command, before the handler runs.
  - `Configuration/SqlGeneratorOptions.cs:100`.
- **Caught by**: `Program.cs:16` and `Program.cs:159`. Both print `Configuration error: ...` and exit with `Constants.ExitCodes.UsageError`. This is the only custom type with its own exit code.

---

### `ValidationException`

Generic validation failure.

- **Properties**
  - `List<string> ValidationErrors`: the individual problems. Defaults to an empty list.
- **Constructors**
  - `ValidationException(string message)`
  - `ValidationException(string message, List<string> errors)`: sets `ValidationErrors`.
- **Thrown when**: `Configuration/AppSettings.cs:169` throws it when the settings are invalid. That call uses the single-message constructor with all errors joined by newlines, so `ValidationErrors` stays empty on that path. Read the message to see the problems.

---

### `FileOperationException`

A file or directory operation failed. Use it for I/O errors in `Utilities/FileHelper.cs` and for directory reads in the parser.

- **Properties**
  - `string? FilePath`: the file or directory involved.
  - `string? Operation`: the operation name, such as `read`, `write`, `list`, `create` or `delete`.
- **Constructors**
  - `FileOperationException(string message)`
  - `FileOperationException(string filePath, string operation)`: the message is generated as `Failed to <operation> file: <path>`. Sets `FilePath` and `Operation`.
  - `FileOperationException(string message, Exception innerException)`
  - `FileOperationException(string filePath, string operation, Exception innerException)`: the same generated message, with the cause attached. Sets `FilePath` and `Operation`.
- **Thrown when**
  - `Utilities/FileHelper.cs:29`: a file is too large to read. The two-argument constructor is used with the operation `read - file too large`, so the generated message reads awkwardly (`Failed to read - file too large file: <path>`).
  - `Utilities/FileHelper.cs:37, 60, 81, 157, 177, 198`: read, write, list, create and delete failures. These wrap the underlying `Exception` and use the message and inner-exception constructors. `FileHelper.cs:35` lets an existing `FileOperationException` pass through without re-wrapping it.
  - `Services/MigrationParserService.cs:178`: the migrations directory does not exist. The message is generated by the two-argument constructor, so it reads `Failed to read directory file: <path>`.

---

### `ConfigFileException`

The configuration file cannot be read, is not valid JSON, or is missing required fields.

- **Properties**
  - `string? FilePath`: the config file that failed.
- **Constructors**
  - `ConfigFileException(string message)`
  - `ConfigFileException(string message, Exception innerException)`
  - `ConfigFileException(string filePath, string message)`: the message is `<message> (file: <path>)`. Sets `FilePath`. Note that the argument order is path first, then message.
- **Thrown when**: `Configuration/ConfigFileLoader.cs` fails:
  - `:96` the file does not exist.
  - `:106` the file cannot be deserialized.
  - `:112` the file does not contain `RepositoryPath`.
  - `:119` the JSON is invalid. The inner `JsonException` is attached.
  - `:123` any other load error. The inner exception is attached.
- **Also documented in**: `Configuration/AppSettings.cs` (XML doc on the unknown-properties check). The code there does not throw it directly.

---

## Using the hierarchy

Catch the most specific type you can handle, and let the rest reach `Program.cs`.

```csharp
try
{
    if (source is null) throw new BranchNotFoundException(sourceBranch);
    // ... run the diff
}
catch (BranchNotFoundException ex)
{
    // Handle the missing branch, using ex.BranchName
}
catch (EfMigrationDiffException ex)
{
    // Any other application error
    Console.Error.WriteLine(ex.Message);
}
```

Because `GitRepositoryException` derives from `RepositoryException`, a `catch (RepositoryException)` handles both. Check derived types first.

When an exception is wrapped (for example, `FileOperationException` around an `IOException`), walk `InnerException` to find the root cause, or call `GetRootCause()` from `EfMigrationDiffExceptionExtensions`.
