# EfMigrationDiffException

`EfMigrationDiffException` is the base exception type for errors encountered during Entity Framework migration diff operations in the `ef-migration-diff` project. It serves as a root exception for specialized exceptions related to migration parsing, repository operations, and migration conflicts, providing structured error information including file paths, line numbers, and conflict details where applicable.

## API

### Constructors

#### `EfMigrationDiffException(string message)`
- **Purpose**: Initializes a new instance of `EfMigrationDiffException` with a specified error message.
- **Parameters**:
  - `message` (string): The error message that explains the reason for the exception.
- **Throws**: Nothing.
- **Remarks**: This is the base constructor for all derived exceptions in the hierarchy.

#### `EfMigrationDiffException(string message, Exception innerException)`
- **Purpose**: Initializes a new instance of `EfMigrationDiffException` with a specified error message and a reference to the inner exception that is the cause of this exception.
- **Parameters**:
  - `message` (string): The error message that explains the reason for the exception.
  - `innerException` (Exception): The exception that is the cause of the current exception.
- **Throws**: Nothing.

---

### `RepositoryException` (derived from `EfMigrationDiffException`)

#### `RepositoryException(string message)`
- **Purpose**: Initializes a new instance of `RepositoryException` with a specified error message.
- **Parameters**:
  - `message` (string): The error message describing the repository-related error.
- **Throws**: Nothing.

#### `RepositoryException(string message, Exception innerException)`
- **Purpose**: Initializes a new instance of `RepositoryException` with a specified error message and inner exception.
- **Parameters**:
  - `message` (string): The error message describing the repository-related error.
  - `innerException` (Exception): The exception that caused the current exception.
- **Throws**: Nothing.

#### `string? RepositoryPath`
- **Purpose**: Gets or sets the path to the repository associated with the exception.
- **Return Value**: A nullable string representing the repository path, or `null` if not applicable.
- **Remarks**: Used in `GitRepositoryException` to provide context about the repository where the error occurred.

---

### `GitRepositoryException` (derived from `RepositoryException`)

#### `GitRepositoryException(string message)`
- **Purpose**: Initializes a new instance of `GitRepositoryException` with a specified error message.
- **Parameters**:
  - `message` (string): The error message describing the Git repository-related error.
- **Throws**: Nothing.

#### `GitRepositoryException(string message, string repositoryPath)`
- **Purpose**: Initializes a new instance of `GitRepositoryException` with a specified error message and repository path.
- **Parameters**:
  - `message` (string): The error message describing the Git repository-related error.
  - `repositoryPath` (string): The path to the Git repository where the error occurred.
- **Throws**: Nothing.

#### `GitRepositoryException(string message, Exception innerException)`
- **Purpose**: Initializes a new instance of `GitRepositoryException` with a specified error message and inner exception.
- **Parameters**:
  - `message` (string): The error message describing the Git repository-related error.
  - `innerException` (Exception): The exception that caused the current exception.
- **Throws**: Nothing.

---

### `MigrationParsingException` (derived from `EfMigrationDiffException`)

#### `MigrationParsingException(string message)`
- **Purpose**: Initializes a new instance of `MigrationParsingException` with a specified error message.
- **Parameters**:
  - `message` (string): The error message describing the parsing error.
- **Throws**: Nothing.

#### `MigrationParsingException(string message, string filePath)`
- **Purpose**: Initializes a new instance of `MigrationParsingException` with a specified error message and file path.
- **Parameters**:
  - `message` (string): The error message describing the parsing error.
  - `filePath` (string): The path to the file where the parsing error occurred.
- **Throws**: Nothing.
- **Remarks**: Sets the `FilePath` property.

#### `MigrationParsingException(string message, string filePath, int lineNumber)`
- **Purpose**: Initializes a new instance of `MigrationParsingException` with a specified error message, file path, and line number.
- **Parameters**:
  - `message` (string): The error message describing the parsing error.
  - `filePath` (string): The path to the file where the parsing error occurred.
  - `lineNumber` (int): The line number in the file where the parsing error occurred.
- **Throws**: Nothing.
- **Remarks**: Sets the `FilePath` and `LineNumber` properties.

#### `string? FilePath`
- **Purpose**: Gets or sets the path to the file associated with the parsing error.
- **Return Value**: A nullable string representing the file path, or `null` if not applicable.

#### `int? LineNumber`
- **Purpose**: Gets or sets the line number in the file where the parsing error occurred.
- **Return Value**: A nullable integer representing the line number, or `null` if not applicable.

---

### `MigrationConflictException` (derived from `EfMigrationDiffException`)

#### `MigrationConflictException(string message)`
- **Purpose**: Initializes a new instance of `MigrationConflictException` with a specified error message.
- **Parameters**:
  - `message` (string): The error message describing the migration conflict.
- **Throws**: Nothing.

#### `MigrationConflictException(string message, List<string> conflicts)`
- **Purpose**: Initializes a new instance of `MigrationConflictException` with a specified error message and a list of conflicting migrations.
- **Parameters**:
  - `message` (string): The error message describing the migration conflict.
  - `conflicts` (List<string>): A list of migration identifiers involved in the conflict.
- **Throws**: Nothing.
- **Remarks**: Sets the `ConflictingMigrations` property.

#### `List<string> ConflictingMigrations`
- **Purpose**: Gets or sets the list of migration identifiers involved in the conflict.
- **Return Value**: A list of strings representing the conflicting migrations.
- **Remarks**: Populated when the exception is constructed with conflicts.

#### `string? MigrationId`
- **Purpose**: Gets or sets the identifier of the migration associated with the conflict.
- **Return Value**: A nullable string representing the migration identifier, or `null` if not applicable.

#### `List<string> ValidationErrors`
- **Purpose**: Gets or sets a list of validation errors associated with the migration conflict.
- **Return Value**: A list of strings representing validation errors.
- **Remarks**: Used to provide detailed validation failure reasons.

---

### `BranchNotFoundException` (derived from `EfMigrationDiffException`)

#### `BranchNotFoundException(string branchName)`
- **Purpose**: Initializes a new instance of `BranchNotFoundException` with the name of the branch that was not found.
- **Parameters**:
  - `branchName` (string): The name of the branch that could not be found.
- **Throws**: Nothing.
- **Remarks**: Sets the `BranchName` property and constructs a default error message.

#### `string? BranchName`
- **Purpose**: Gets or sets the name of the branch associated with the exception.
- **Return Value**: A nullable string representing the branch name, or `null` if not applicable.

## Usage

### Example 1: Handling a Migration Parsing Error
