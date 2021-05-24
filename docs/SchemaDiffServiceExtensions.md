# SchemaDiffServiceExtensions

Static class containing extension methods for registering schema‑diff services and for converting `SchemaChange` collections into various report formats.

## API

### AddSchemaDiffServices(IServiceCollection services, Action<SchemaDiffOptions> configure = null)

Registers the core schema‑diff services (e.g., change calculators, reporters) with the DI container.

- **parameters**
  - `services`: The `IServiceCollection` to which services are added.
  - `configure`: Optional delegate to further configure `SchemaDiffOptions`.
- **return value**: The same `IServiceCollection` instance, allowing method chaining.
- **exceptions**: Throws `ArgumentNullException` if `services` is `null`.

### AddSchemaDiffServices(IServiceCollection services, IConfiguration configuration)

Registers schema‑diff services using settings read from an `IConfiguration` instance.

- **parameters**
  - `services`: The `IServiceCollection` to which services are added.
  - `configuration`: Configuration provider containing schema‑diff options.
- **return value**: The same `IServiceCollection` instance.
- **exceptions**: Throws `ArgumentNullException` if either `services` or `configuration` is `null`.

### ToSideBySideHtml(IEnumerable<SchemaChange> changes)

Produces an HTML string that shows schema changes in a side‑by‑side view.

- **parameters**
  - `changes`: Collection of `SchemaChange` objects to render.
- **return value**: HTML markup representing the side‑by‑side diff.
- **exceptions**: Throws `ArgumentNullException` if `changes` is `null`.

### ToUnifiedHtml(IEnumerable<SchemaChange> changes)

Produces an HTML string that shows schema changes in a unified (inline) view.

- **parameters**
  - `changes`: Collection of `SchemaChange` objects to render.
- **return value**: HTML markup representing the unified diff.
- **exceptions**: Throws `ArgumentNullException` if `changes` is `null`.

### GetDestructiveChanges(IEnumerable<SchemaChange> changes)

Filters the supplied changes to those that would cause data loss or breaking schema modifications.

- **parameters**
  - `changes`: Collection of `SchemaChange` objects to evaluate.
- **return value**: An `IEnumerable<SchemaChange>` containing only destructive changes.
- **exceptions**: Throws `ArgumentNullException` if `changes` is `null`.

### ToTextSummary(IEnumerable<SchemaChange> changes)

Creates a plain‑text summary of the supplied changes.

- **parameters**
  - `changes`: Collection of `SchemaChange` objects to summarize.
- **return value**: A multi‑line string describing the changes.
- **exceptions**: Throws `ArgumentNullException` if `changes` is `null`.

### ToMergeEditorHtml(IEnumerable<SchemaChange> changes)

Generates HTML suitable for embedding in a merge‑editor UI, highlighting conflicts and resolutions.

- **parameters**
  - `changes`: Collection of `SchemaChange` objects to render.
- **return value**: HTML markup for the merge editor.
- **exceptions**: Throws `ArgumentNullException` if `changes` is `null`.

### IsCleanMerge(IEnumerable<SchemaChange> changes)

Determines whether the supplied changes can be merged without any conflicts.

- **parameters**
  - `changes`: Collection of `SchemaChange` objects to evaluate.
- **return value**: `true` if the merge is clean (no conflicts); otherwise `false`.
- **exceptions**: Throws `ArgumentNullException` if `changes` is `null`.

### TryAutoResolve(IEnumerable<SchemaChange> changes)

Attempts to automatically resolve conflicts among the changes and returns a resolution plan.

- **parameters**
  - `changes`: Collection of `SchemaChange` objects to resolve.
- **return value**: A `MergeResolutionPlan` indicating whether auto‑resolution succeeded and, if so, the resolved changes.
- **exceptions**: Throws `ArgumentNullException` if `changes` is `null`.

### GetConflictSummary(IEnumerable<SchemaChange> changes)

Aggregates the supplied changes into a summary of conflict types and their frequencies.

- **parameters**
  - `changes`: Collection of `SchemaChange` objects to analyse.
- **return value**: An `IReadOnlyDictionary<string, int>` where the key is a conflict description and the value is the count of occurrences.
- **exceptions**: Throws `ArgumentNullException` if `changes` is `null`.

### AddSchemaDiffPipeline(IServiceCollection services)

Adds the middleware pipeline components required for processing schema‑diff requests (e.g., handlers, formatters).

- **parameters**
  - `services`: The `IServiceCollection` to which pipeline services are added.
- **return value**: The same `IServiceCollection` instance.
- **exceptions**: Throws `ArgumentNullException` if `services` is `null`.

## Usage

### Registering services in an ASP.NET Core application

```csharp
using Microsoft.Extensions.DependencyInjection;
using EfMigrationDiff; // namespace containing SchemaDiffServiceExtensions

var builder = WebApplication.CreateBuilder(args);

// Register schema‑diff services with optional configuration
builder.Services.AddSchemaDiffServices(options =>
{
    options.IncludeIndexes = true;
    options.MaxDiffSize = 10_000;
});

// Alternatively, bind from IConfiguration
// builder.Services.AddSchemaDiffServices(builder.Configuration.GetSection("SchemaDiff"));

// Add the processing pipeline
builder.Services.AddSchemaDiffPipeline();

var app = builder.Build();
app.Run();
```

### Generating a side‑by‑side HTML report and checking for destructive changes

```csharp
using System.Collections.Generic;
using System.Linq;
using EfMigrationDiff; // namespace containing SchemaDiffServiceExtensions and SchemaChange

IEnumerable<SchemaChange> changes = GetSchemaChangesFromSomewhere(); // hypothetical source

// Produce HTML for display in a web view
string sideBySideHtml = SchemaDiffServiceExtensions.ToSideBySideHtml(changes);

// Identify any destructive changes that may require manual review
IEnumerable<SchemaChange> destructive = SchemaDiffServiceExtensions.GetDestructiveChanges(changes);
if (destructive.Any())
{
    // Log or present the destructive changes to the user
    foreach (var change in destructive)
    {
        // handle each destructive change
    }
}
```

## Notes

- All extension methods are **stateless** and safe to invoke concurrently from multiple threads, provided the input collections are not modified during enumeration.
- Passing `null` for any `IServiceCollection` or `IEnumerable<SchemaChange>` argument results in an `ArgumentNullException`; callers should validate arguments beforehand if null values are possible.
- The `AddSchemaDiffServices` overloads are additive; calling both will not duplicate registrations because the underlying services are registered as singletons/scoped as appropriate.
- Empty change collections yield empty or default outputs (e.g., empty HTML, `false` for `IsCleanMerge`, empty dictionary for `GetConflictSummary`).
- `TryAutoResolve` returns a `MergeResolutionPlan` whose `Success` property indicates whether auto‑resolution completed without user intervention; inspect this property before applying the plan.
- The methods do not perform any I/O; they operate purely in‑memory on the supplied `SchemaChange` instances. Any latency observed is due to the size of the change set.
