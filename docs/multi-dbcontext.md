# Multiple DbContexts

This guide explains how ef-migration-diff handles projects that contain more than one `DbContext`.

## How Migration Folders Are Discovered

ef-migration-diff scans for migration files using a combination of:

1. **Folder convention** — it walks the repository tree looking for directories named `Migrations` (or the path you provide via `--migrations-path`).
2. **DbContext association** — each migration file carries a `DbContext` annotation in its designer file (`*_Migration.Designer.cs`). The tool reads that annotation to associate each migration with a specific context.
3. **Assembly metadata** — when the `MigrationsAssembly` fluent configuration is used, that assembly name is stored in `DbContextMetadata.AssemblyName` and is used to scope discovery to the correct assembly.

> **Note:** ef-migration-diff analyses migration *files*, not a live database. You do not need a running database or a valid connection string to compare branches.

## Comparing a Specific DbContext

Use the `--db-context` flag to isolate analysis to one context:

```bash
# Compare only AppDbContext migrations
ef-migration-diff compare \
  --branch1 main \
  --branch2 feature/users \
  --db-context AppDbContext

# Compare only IdentityDbContext migrations
ef-migration-diff compare \
  --branch1 main \
  --branch2 feature/identity-refactor \
  --db-context IdentityDbContext
```

### Programmatic API

```csharp
var diff = diffService.CompareDbContextMigrations(
    sourceBranch: source,
    targetBranch: target,
    dbContextName: "AppDbContext");   // simple or fully-qualified name
```

`CompareDbContextMigrations` filters migration lists to only those whose `Migration.DbContextName` matches the supplied value before running conflict detection and schema change analysis.

## Comparing All Contexts in One Pass

For projects with many contexts, script one comparison per context:

```bash
for ctx in AppDbContext AuditDbContext IdentityDbContext; do
  echo "=== $ctx ==="
  ef-migration-diff compare \
    --branch1 main \
    --branch2 "$FEATURE_BRANCH" \
    --db-context "$ctx" \
    --format json \
    --output-path "./reports/${ctx}.json"
done
```

## MigrationsAssembly Support

When a project configures EF Core migrations in a separate assembly:

```csharp
// In Program.cs / Startup.cs
options.UseSqlServer(connectionString,
    x => x.MigrationsAssembly("MyApp.Migrations"));
```

ef-migration-diff respects this via `DbContextMetadata.AssemblyName`. When the tool scans the repository it creates a `DbContextMetadata` entry per discovered context and stores the assembly name. Calls to `DbContextRepository.GetByAssembly("MyApp.Migrations")` will return only contexts whose migrations live in that assembly.

## Cross-Context Table Conflicts

When migrations from different contexts touch the **same table** (e.g., both `AppDbContext` and `AuditDbContext` have migrations that alter a shared `Users` table), the `ConflictDetectionService` will raise a conflict with `ConflictType = TableConflict` and list the affected table under `AffectedElements`.

To see cross-context conflicts explicitly, run a comparison without the `--db-context` filter so migrations from all contexts are included:

```bash
ef-migration-diff compare \
  --branch1 main \
  --branch2 feature/shared-table-change \
  --format json | jq '.Conflicts[] | select(.AffectedElements | length > 0)'
```

## Example: Three-Context Project

Given this project layout:

```
src/
  App.Data/
    Migrations/           ← AppDbContext migrations
  App.Audit/
    Migrations/           ← AuditDbContext migrations
  App.Identity/
    Migrations/           ← IdentityDbContext migrations
```

```bash
# All three contexts in one full comparison
ef-migration-diff compare --branch1 main --branch2 develop

# Scope to a single context
ef-migration-diff compare \
  --branch1 main \
  --branch2 develop \
  --db-context AuditDbContext \
  --migrations-path ./src/App.Audit/Migrations
```

## Programmatic Multi-Context Analysis

```csharp
var contexts = new[] { "AppDbContext", "AuditDbContext", "IdentityDbContext" };

foreach (var ctx in contexts)
{
    var diff = diffService.CompareDbContextMigrations(sourceBranch, targetBranch, ctx);

    Console.WriteLine($"[{ctx}] conflicts={diff.Conflicts.Count} " +
                      $"schemaChanges={diff.GetTotalSchemaChanges()}");

    if (diff.HasBlockingConflicts())
    {
        Console.WriteLine($"  ⚠ Blocking conflicts in {ctx}!");
    }
}
```

## See Also

- [`Services/MigrationDiffService.cs`](../Services/MigrationDiffService.cs) — `CompareDbContextMigrations` implementation
- [`Models/DbContextMetadata.cs`](../Models/DbContextMetadata.cs) — metadata model
- [`Repositories/DbContextRepository.cs`](../Repositories/DbContextRepository.cs) — context discovery and lookup
- [Getting Started](./getting-started.md)
