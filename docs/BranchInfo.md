# BranchInfo

Represents metadata about a Git branch relevant to EF Core migrations, including commit information, migration history, and associated DbContext types.

## API

### `string Id`
Unique identifier for the branch. Typically corresponds to the branch name or a derived value.

### `string BranchName`
Display name of the Git branch. May be null or empty if not available.

### `string CommitHash`
Full commit hash of the latest commit on the branch. Used to identify the exact state of the codebase.

### `string CommitMessage`
Commit message associated with the latest commit on the branch.

### `DateTime CommitDate`
Timestamp of the latest commit on the branch.

### `string Author`
Name of the author of the latest commit on the branch.

### `List<string> MigrationIds`
List of migration IDs present in the branch. May be empty if no migrations exist.

### `List<string> DbContexts`
List of fully qualified DbContext type names associated with the branch. May be empty if no DbContexts are defined.

### `string MigrationsPath`
Relative or absolute path where migrations are stored for this branch. May be null if migrations are stored in a default location.

### `bool IsRemote`
Indicates whether the branch is a remote branch (e.g., from a remote repository).

### `BranchInfo()`
Constructs a new, empty `BranchInfo` instance with default values.

### `BranchInfo(string id, string branchName, string commitHash, string commitMessage, DateTime commitDate, string author, List<string> migrationIds, List<string> dbContexts, string migrationsPath, bool isRemote)`
Constructs a `BranchInfo` instance with the specified metadata.

**Parameters:**
- `id`: Unique identifier for the branch.
- `branchName`: Display name of the branch.
- `commitHash`: Full commit hash of the latest commit.
- `commitMessage`: Commit message for the latest commit.
- `commitDate`: Timestamp of the latest commit.
- `author`: Name of the commit author.
- `migrationIds`: List of migration IDs present in the branch.
- `dbContexts`: List of DbContext type names.
- `migrationsPath`: Path where migrations are stored.
- `isRemote`: Whether the branch is remote.

### `bool IsValid`
Indicates whether the `BranchInfo` instance contains valid, non-null metadata. Returns `true` if required fields (e.g., `CommitHash`) are populated; otherwise, `false`.

### `void AddMigration(string migrationId)`
Adds a migration ID to the `MigrationIds` list if it is not already present.

**Parameters:**
- `migrationId`: Migration ID to add.

**Throws:**
- `ArgumentNullException`: If `migrationId` is null.

### `void AddDbContext(string dbContextTypeName)`
Adds a DbContext type name to the `DbContexts` list if it is not already present.

**Parameters:**
- `dbContextTypeName`: Fully qualified DbContext type name to add.

**Throws:**
- `ArgumentNullException`: If `dbContextTypeName` is null.

### `int GetMigrationCount()`
Returns the number of migration IDs in the `MigrationIds` list.

**Returns:**
- Count of migration IDs.

### `int GetDbContextCount()`
Returns the number of DbContext type names in the `DbContexts` list.

**Returns:**
- Count of DbContext type names.

### `bool HasMigration(string migrationId)`
Determines whether the specified migration ID exists in the `MigrationIds` list.

**Parameters:**
- `migrationId`: Migration ID to check.

**Returns:**
- `true` if the migration ID exists; otherwise, `false`.

**Throws:**
- `ArgumentNullException`: If `migrationId` is null.

### `bool HasDbContext(string dbContextTypeName)`
Determines whether the specified DbContext type name exists in the `DbContexts` list.

**Parameters:**
- `dbContextTypeName`: DbContext type name to check.

**Returns:**
- `true` if the DbContext type name exists; otherwise, `false`.

**Throws:**
- `ArgumentNullException`: If `dbContextTypeName` is null.

### `string GetShortCommitHash()`
Returns the first 7 characters of the `CommitHash` for brevity.

**Returns:**
- Shortened commit hash string.

**Throws:**
- `InvalidOperationException`: If `CommitHash` is null or empty.

## Usage

### Example: Creating and Populating a BranchInfo
