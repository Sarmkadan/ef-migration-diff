# ReportGenerationService

The `ReportGenerationService` class provides functionality to generate various types of reports from EF Core migration differences, including text, JSON, and HTML formats, as well as a conflict summary report.

## API

### `GenerateTextReport`
Generates a human-readable plain text report of migration differences.

- **Parameters**: None
- **Return value**: `string` – The generated text report
- **Exceptions**: Throws `InvalidOperationException` if no migration differences are available to report

### `GenerateJsonReport`
Generates a machine-readable JSON report of migration differences.

- **Parameters**: None
- **Return value**: `string` – The generated JSON report
- **Exceptions**: Throws `InvalidOperationException` if no migration differences are available to report

### `GenerateHtmlReport`
Generates an HTML-formatted report of migration differences suitable for display in web contexts.

- **Parameters**: None
- **Return value**: `string` – The generated HTML report
- **Exceptions**: Throws `InvalidOperationException` if no migration differences are available to report

### `GenerateConflictSummary`
Generates a concise summary of conflicts detected during migration comparison.

- **Parameters**: None
- **Return value**: `string` – The conflict summary report
- **Exceptions**: Throws `InvalidOperationException` if no conflicts are present

## Usage
