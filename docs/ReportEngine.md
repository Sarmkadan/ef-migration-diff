# ReportEngine

`ReportEngine` is an abstract base class that defines a common contract for generating migration-difference reports in multiple output formats. It provides built-in support for JSON, CSV, plain text, and HTML reports, while allowing derived classes to supply a unique report name and a custom report-generation strategy. Templates can be registered and retrieved to influence formatting or content.

## API

### `public ReportEngine`

Default constructor. Initializes a new instance of a concrete `ReportEngine` subclass. No initialization logic is performed at this level beyond the runtime’s object construction.

### `public abstract string Name`

Gets the unique, human-readable name of this report engine. Derived classes must override this property to return a non-null, non-empty string that identifies the report type (e.g. `"SchemaDiff"`).

### `public abstract string GenerateReport`

Generates a report using the default format defined by the concrete implementation. The return value is the complete report content as a string. Derived classes must override this member to produce the report; the base implementation throws `NotImplementedException` if called directly.

### `public string GenerateJsonReport`

Generates the report formatted as JSON. Returns a string containing valid JSON. Relies on the same underlying data as `GenerateReport` but serializes it to JSON. May throw `InvalidOperationException` if the data cannot be serialized, or `NotSupportedException` if JSON output is not supported by the concrete engine.

### `public string GenerateCsvReport`

Generates the report formatted as comma-separated values. Returns a string containing CSV data with a header row followed by data rows. Throws `InvalidOperationException` when the underlying data lacks a tabular structure, or `NotSupportedException` if CSV output is not supported.

### `public string GenerateTextReport`

Generates the report as plain, human-readable text. Returns a string suitable for console output or log files. Throws `NotSupportedException` if the engine does not support plain-text rendering.

### `public string GenerateHtmlReport`

Generates the report as an HTML document fragment or full page. Returns a string containing HTML markup. Throws `NotSupportedException` if HTML output is not supported by the engine.

### `public void RegisterTemplate(string name, IReportTemplate template)`

Registers a report template under the given unique name. The `name` parameter must not be null or empty; an `ArgumentException` is thrown otherwise. The `template` parameter must not be null; an `ArgumentNullException` is thrown otherwise. If a template with the same name already exists, it is silently overwritten.

### `public IReportTemplate? GetTemplate(string name)`

Retrieves a previously registered template by its unique name. Returns the `IReportTemplate` instance if found, or `null` if no template is registered under that name. The `name` parameter must not be null or empty; an `ArgumentException` is thrown otherwise.

## Usage

**Example 1: Using a concrete engine to generate all supported formats**

```csharp
var engine = new SchemaDiffReportEngine(); // hypothetical derived class
engine.RegisterTemplate("summary", new SummaryTemplate());

string jsonReport = engine.GenerateJsonReport();
string csvReport = engine.GenerateCsvReport();
string textReport = engine.GenerateTextReport();
string htmlReport = engine.GenerateHtmlReport();

Console.WriteLine($"Engine: {engine.Name}");
File.WriteAllText("diff_report.json", jsonReport);
```

**Example 2: Retrieving a template and falling back to a default report**

```csharp
var engine = new MigrationReportEngine();
engine.RegisterTemplate("detailed", new DetailedDiffTemplate());

IReportTemplate? template = engine.GetTemplate("detailed");
string report;

if (template != null)
{
    report = template.Apply(engine.GenerateReport());
}
else
{
    report = engine.GenerateTextReport();
}

Console.WriteLine(report);
```

## Notes

- `ReportEngine` is abstract; direct instantiation is not possible. Consumers must use a derived class that implements `Name` and `GenerateReport`.
- The format-specific methods (`GenerateJsonReport`, `GenerateCsvReport`, `GenerateTextReport`, `GenerateHtmlReport`) may throw `NotSupportedException` if a derived engine does not implement that format. Callers should check documentation of the concrete engine or wrap calls in try-catch blocks when format support is uncertain.
- `RegisterTemplate` overwrites existing templates without warning. Use `GetTemplate` first to check for collisions if uniqueness is required.
- `GetTemplate` returns `null` for missing templates; callers must handle the null case to avoid `NullReferenceException`.
- Thread safety is not guaranteed. Concurrent calls to `RegisterTemplate` and `GetTemplate` from multiple threads may lead to race conditions. External synchronization is recommended if templates are mutated after initial registration.
- The default `GenerateReport` override in the base class throws `NotImplementedException`. Derived classes that fail to override it will cause runtime failures.
