# SchemaDiffOptions

`SchemaDiffOptions` is a configuration class used to control the behavior of schema difference operations, such as generating diffs between database schemas, branches, or merge scenarios. It allows fine-tuning the comparison process by specifying labels, context lines, and other parameters to tailor the output to specific needs.

## API

### `BaseLabel`
- **Purpose**: Specifies the base label for the schema comparison. This label typically represents the common ancestor or reference point in branch or merge scenarios.
- **Type**: `string`
- **Default**: `null`
- **Usage**: Used to identify the baseline schema when comparing divergent branches or versions.

### `SourceLabel`
- **Purpose**: Specifies the source label for the schema comparison. This label usually represents the starting point or "from" state in the comparison.
- **Type**: `string`
- **Default**: `null`
- **Usage**: Used to identify the source schema in diff operations, such as comparing a development branch to a production branch.

### `TargetLabel`
- **Purpose**: Specifies the target label for the schema comparison. This label typically represents the ending point or "to" state in the comparison.
- **Type**: `string`
- **Default**: `null`
- **Usage**: Used to identify the target schema in diff operations, such as comparing a feature branch to the main branch.

### `ContextLines`
- **Purpose**: Controls the number of surrounding lines to include in the diff output for context. A higher value provides more surrounding context but may clutter the output.
- **Type**: `int`
- **Default**: `3`
- **Range**: Must be a non-negative integer.
- **Throws**: `ArgumentOutOfRangeException` if set to a negative value.

### `IncludeSqlContent`
- **Purpose**: Determines whether the diff output includes the SQL content of schema changes. When `true`, the generated diff includes the actual SQL statements for schema modifications.
- **Type**: `bool`
- **Default**: `true`
- **Usage**: Useful for reviewing exact changes but may be omitted for brevity in some scenarios.

### `IncludeMetadata`
- **Purpose**: Determines whether the diff output includes metadata about the schema objects, such as creation timestamps or author information.
- **Type**: `bool`
- **Default**: `true`
- **Usage**: Useful for auditing or tracking changes but may be excluded for performance reasons in large schemas.

### `IgnoreWhitespace`
- **Purpose**: Controls whether whitespace differences are ignored in the diff output. When `true`, whitespace-only changes are not reported as differences.
- **Type**: `bool`
- **Default**: `false`
- **Usage**: Useful for focusing on structural or semantic changes rather than formatting differences.

### `MaxHunkLines`
- **Purpose**: Limits the maximum number of lines in a single hunk of the diff output. Hunks larger than this value are split into smaller hunks.
- **Type**: `int`
- **Default**: `100`
- **Range**: Must be a positive integer.
- **Throws**: `ArgumentOutOfRangeException` if set to a non-positive value.

### `ForBranches`
- **Purpose**: Provides a pre-configured set of options optimized for comparing two branches. Sets default values suitable for branch comparison scenarios.
- **Type**: `static SchemaDiffOptions`
- **Returns**: A new `SchemaDiffOptions` instance with properties configured for branch comparisons.
- **Usage**: Convenience method to quickly obtain sensible defaults for branch diff operations.

### `ForMerge`
- **Purpose**: Provides a pre-configured set of options optimized for comparing schemas in a merge scenario. Sets default values suitable for merge operations.
- **Type**: `static SchemaDiffOptions`
- **Returns**: A new `SchemaDiffOptions` instance with properties configured for merge comparisons.
- **Usage**: Convenience method to quickly obtain sensible defaults for merge diff operations.

## Usage

### Example 1: Comparing Two Branches
