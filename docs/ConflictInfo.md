# ConflictInfo

Represents a detected conflict between two Entity Framework migrations, capturing its identity, type, severity, and resolution state. It is used by the migration diff analyzer to report inconsistencies that may require manual intervention.

## API

### ConflictInfo()
Initializes a new instance of the `ConflictInfo` class with default values.

- **Parameters:** none  
- **Return:** a new `ConflictInfo` object  
- **Exceptions:** none  

### ConflictInfo(string id, string firstMigrationId, string secondMigrationId, ConflictType conflictType, ConflictSeverity severity, string description, List<string> affectedElements, Dictionary<string, string> details, DateTime detectedAt, bool isResolved, string? resolutionStrategy, bool isValid, bool isBlocking)
Initializes a new instance of the `ConflictInfo` class with the specified values.

- **Parameters:**  
  - `id` – Unique identifier for the conflict.  
  - `firstMigrationId` – Identifier of the first migration involved.  
  - `secondMigrationId` – Identifier of the second migration involved.  
  - `conflictType` – The type of conflict (e.g., schema, data).  
  - `severity` – The severity level of the conflict.  
  - `description` – Human‑readable description of the conflict.  
  - `affectedElements` – Collection of element names affected by the conflict.  
  - `details` – Additional key‑value pairs providing context.  
  - `detectedAt` – Timestamp when the conflict was detected.  
  - `isResolved` – Indicates whether the conflict has been resolved.  
  - `resolutionStrategy` – Optional description of how the conflict was resolved.  
  - `isValid` – Indicates whether the instance is in a valid state.  
  - `isBlocking` – Indicates whether the conflict blocks further migrations.  
- **Return:** a new `ConflictInfo` object  
- **Exceptions:**  
  - `ArgumentNullException` if `id`, `firstMigrationId`, `secondMigrationId`, `description`, `affectedElements`, or `details` is `null`.  
  - `ArgumentException` if `affectedElements` or `details` contains a `null` entry.  

### Id
Gets or sets the unique identifier for the conflict.

- **Type:** `string`  
- **Purpose:** Provides a stable key for tracking the conflict across runs.  
- **Exceptions:** none  

### FirstMigrationId
Gets or sets the identifier of the first migration involved in the conflict.

- **Type:** `string`  
- **Purpose:** References the migration that appears first in the history.  
- **Exceptions:** none  

### SecondMigrationId
Gets or sets the identifier of the second migration involved in the conflict.

- **Type:** `string`  
- **Purpose:** References the migration that appears later in the history.  
- **Exceptions:** none  

### ConflictType
Gets or sets the type of the conflict.

- **Type:** `ConflictType`  
- **Purpose:** Categorizes the conflict (e.g., schema mismatch, data divergence).  
- **Exceptions:** none  

### Severity
Gets or sets the severity level of the conflict.

- **Type:** `ConflictSeverity`  
- **Purpose:** Indicates how critical the conflict is to the migration process.  
- **Exceptions:** none  

### Description
Gets or sets a human‑readable description of the conflict.

- **Type:** `string`  
- **Purpose:** Provides details useful for logging or user feedback.  
- **Exceptions:** none  

### AffectedElements
Gets the collection of element names (tables, columns, constraints, etc.) affected by the conflict.

- **Type:** `List<string>`  
- **Purpose:** Allows callers to enumerate which parts of the model are involved.  
- **Exceptions:** none  

### Details
Gets the dictionary of additional metadata associated with the conflict.

- **Type:** `Dictionary<string, string>`  
- **Purpose:** Stores arbitrary key‑value pairs for extended diagnostics.  
- **Exceptions:** none  

### DetectedAt
Gets or sets the date and time when the conflict was detected.

- **Type:** `DateTime`  
- **Purpose:** Audits when the conflict was first observed.  
- **Exceptions:** none  

### IsResolved
Gets or sets a flag indicating whether the conflict has been resolved.

- **Type:** `bool`  
- **Purpose:** Used by UI or automation to skip resolved conflicts.  
- **Exceptions:** none  

### ResolutionStrategy
Gets or sets an optional description of the strategy used to resolve the conflict.

- **Type:** `string?`  
- **Purpose:** Records how the conflict was addressed (e.g., “manual merge”, “drop column”).  
- **Exceptions:** none  

### IsValid
Gets or sets a flag indicating whether the instance contains a consistent set of values.

- **Type:** `bool`  
- **Purpose:** Allows callers to validate the object before further processing.  
- **Exceptions:** none  

### GetTitle()
Returns a concise title summarizing the conflict.

- **Parameters:** none  
- **Return:** `string` – A formatted title, typically combining IDs and conflict type.  
- **Exceptions:** none  

### AddAffectedElement(string element)
Adds an element identifier to the `AffectedElements` list.

- **Parameters:**  
  - `element` – The name of the element to add.  
- **Return:** `void`  
- **Exceptions:**  
  - `ArgumentNullException` if `element` is `null`.  

### AddDetail(string key, string value)
Adds a key‑value pair to the `Details` dictionary.

- **Parameters:**  
  - `key` – The metadata key.  
  - `value` – The associated value.  
- **Return:** `void`  
- **Exceptions:**  
  - `ArgumentNullException` if `key` or `value` is `null`.  

### GetDetail(string key)
Retrieves the value associated with the specified key from `Details`.

- **Parameters:**  
  - `key` – The metadata key to look up.  
- **Return:** `string` – The value if found; otherwise `null`.  
- **Exceptions:**  
  - `ArgumentNullException` if `key` is `null`.  

### MarkResolved()
Marks the conflict as resolved.

- **Parameters:** none  
- **Return:** `void`  
- **Exceptions:**  
  - `InvalidOperationException` if `IsResolved` is already `true`.  

### IsBlocking
Gets or sets a flag indicating whether the conflict blocks further migration execution.

- **Type:** `bool`  
- **Purpose:** When `true`, the migration pipeline should halt until the conflict is addressed.  
- **Exceptions:** none  

## Usage

### Creating and populating a conflict instance
```csharp
var conflict = new ConflictInfo(
    id: "conf-001",
    firstMigrationId: "20230401_AddUserTable",
    secondMigrationId: "20230402_AddEmailColumn",
    conflictType: ConflictType.Schema,
    severity: ConflictSeverity.Error,
    description: "The 'Email' column is defined in the second migration but missing in the first.",
    affectedElements: new List<string> { "Users.Email" },
    details: new Dictionary<string, string>
    {
        ["ColumnName"] = "Email",
        ["TableName"] = "Users"
    },
    detectedAt: DateTime.UtcNow,
    isResolved: false,
    resolutionStrategy: null,
    isValid: true,
    isBlocking: true
);

// Add additional affected element
conflict.AddAffectedElement("Users.Index_Email");

// Store extra diagnostic info
conflict.AddDetail("MigrationSource", "dev branch");

// Retrieve a detail
string source = conflict.GetDetail("MigrationSource");

// Check if the conflict blocks migration
if (conflict.IsBlocking)
{
    Console.WriteLine($"Blocking conflict: {conflict.GetTitle()}");
}
```

### Marking a conflict as resolved
```csharp
// Assume 'conflict' is an existing ConflictInfo instance
conflict.MarkResolved();
conflict.ResolutionStrategy = "Added missing column via manual script";

if (conflict.IsResolved)
{
    Console.WriteLine($"Conflict resolved using: {conflict.ResolutionStrategy}");
}
```

## Notes
- All string‑based members (`Id`, `FirstMigrationId`, `SecondMigrationId`, `Description`, `ResolutionStrategy`) accept `null` only if explicitly allowed (e.g., `ResolutionStrategy`). Passing `null` to required string parameters in the constructor or to `AddAffectedElement`, `AddDetail`, or `GetDetail` will throw `ArgumentNullException`.
- The `AffectedElements` list and `Details` dictionary are initialized by the constructor; replacing them with `null` after construction is not recommended and may lead to `NullReferenceException` when accessing the members.
- The type is **not thread‑safe**. Concurrent calls to `AddAffectedElement`, `AddDetail`, or `MarkResolved` from multiple threads without external synchronization can result in inconsistent state.
- `DetectedAt` is intended to be set once at detection time; modifying it after creation may obscure audit trails.
- `IsValid` is a convenience flag; it does not guarantee semantic correctness beyond non‑null checks for required fields.
- `IsBlocking` does not automatically halt migration pipelines; consumers must inspect this property and act accordingly.
