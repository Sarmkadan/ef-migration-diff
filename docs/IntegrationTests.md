# IntegrationTests

The `IntegrationTests` class contains end-to-end tests for the `ef-migration-diff` project, validating the complete workflow of parsing, comparing, and reporting migration differences across multiple database contexts and migration scenarios. These tests ensure that the tool correctly handles schema changes, conflict detection, concurrent processing, and output generation in various formats.

## API

### `EndToEnd_ParseParseCompareAndReport_CompletesSuccessfully`

Validates that the entire pipeline—from parsing migration files to generating reports—completes without errors. This test serves as a smoke test for the core functionality.

- **Parameters**: None
- **Return value**: `void`
- **Throws**: Any exception thrown during the pipeline execution

### `FullWorkflow_MultipleDbContexts_HandlesCorrectly`

Ensures that the tool correctly processes migrations across multiple independent database contexts without interference or incorrect cross-context comparisons.

- **Parameters**: None
- **Return value**: `void`
- **Throws**: Any exception during context processing or comparison

### `ConcurrentMigrationProcessing_MultipleThreadsProcessDifferentMigrations_AllProcessed`

Verifies that concurrent execution of the migration processing pipeline handles multiple migrations assigned to different threads without race conditions or missed operations.

- **Parameters**: None
- **Return value**: `void`
- **Throws**: Any exception indicating thread-safety issues or incomplete processing

### `ReportGeneration_WithDifferentFormats_AllFormatsProduceConsistentData`

Confirms that report generation produces consistent results across different output formats (e.g., JSON, Markdown, HTML) for the same input data.

- **Parameters**: None
- **Return value**: `void`
- **Throws**: Any exception during report serialization or formatting

### `SchemaChangeDetectionPipeline_ComplexMigration_DetectsAllOperations`

Tests that the schema change detection pipeline correctly identifies all types of operations (e.g., table creations, column alterations, index modifications) in a complex migration.

- **Parameters**: None
- **Return value**: `void`
- **Throws**: Any exception during schema analysis or operation detection

### `ConflictDetection_WithTableNameConflict_IdentifiesConflict`

Validates that the tool correctly identifies and reports conflicts when the same table name is referenced in conflicting migrations.

- **Parameters**: None
- **Return value**: `void`
- **Throws**: Any exception during conflict resolution or detection

### `MigrationValidation_WithValidAndInvalidMigrations_IdentifiesInvalidOnes`

Ensures that the validation pipeline correctly flags invalid migrations while allowing valid ones to proceed without false positives.

- **Parameters**: None
- **Return value**: `void`
- **Throws**: Any exception during migration validation

### `MultipleDbContextComparison_IndependentContexts_ProcessesWithoutInterference`

Confirms that comparing migrations from independent database contexts does not produce false positives or interference between contexts.

- **Parameters**: None
- **Return value**: `void`
- **Throws**: Any exception during context comparison

### `ReadmeExample_BasicComparison_WorksAsDocumented`

Validates that the example provided in the project's README executes successfully and produces the documented output.

- **Parameters**: None
- **Return value**: `void`
- **Throws**: Any exception during example execution or output verification

## Usage
