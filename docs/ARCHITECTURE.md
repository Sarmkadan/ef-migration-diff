# Architecture

ef-migration-diff is a .NET 10 console tool that compares Entity Framework Core
migrations between git branches: it finds migrations that exist on only one side,
extracts the schema changes each migration makes, and flags conflicts before they
blow up in a merge or on the shared dev database.

This document describes how the code is actually laid out and why.

## High-level flow

```
git repo (LibGit2Sharp)
      │
      ▼
GitRepository ──► migration *.cs files per branch
      │
      ▼
MigrationParserService ──► Migration (id, name, content, DbContext)
      │
      ▼
MigrationDiffService ──► MigrationDiff (source-only / target-only / common)
      │                        │
      │                        ├─► SchemaChangeDetectorService ─► SchemaChange[]
      │                        └─► ConflictDetectionService   ─► ConflictInfo[]
      ▼
ReportGenerationService / Formatters (JSON, CSV, HTML, visual diff)
```

There is a second, newer pipeline ("v2") layered on top:

```
BranchInfo ──► SchemaDiffPipelineService
                  ├─► MigrationDiffService (v1, reused for extraction)
                  ├─► SchemaDiffEngine : ISchemaDiffEngine, IMergeEditor
                  │     (two-way + three-way diff, merge planning)
                  └─► IVisualDiffRenderer (VisualDiffFormatter, HTML output)
```

The pipeline deliberately reuses v1 for parsing/extraction and only adds the
diff/merge computation on top - see "Key decisions" below.

## Module breakdown

| Folder | What lives there |
|---|---|
| `Program.cs` | `MigrationDiffApplication` - builds the service provider, dispatches `compare`/`validate`/`report`/`visual-diff` commands, prints usage. |
| `CLI/` | `CommandParser` (short/long option parsing), `CommandExecutor` (middleware pipeline around commands), `Commands/` (Compare, Validate, VisualDiff, Help). |
| `Middleware/` | `ICommandMiddleware` chain used by `CommandExecutor`: error handling, request logging, validation. Console-app analogue of ASP.NET middleware. |
| `Repositories/` | `GitRepository` (LibGit2Sharp wrapper: branches, file contents per branch), `MigrationRepository` (in-memory, lock-guarded store of parsed migrations), `DbContextRepository`. |
| `Services/` | The core: `MigrationParserService`, `MigrationDiffService`, `SchemaChangeDetectorService`, `ConflictDetectionService`, `MigrationDependencyGraphService`, `MigrationAutoResolverService`, `ReportGenerationService`, plus v2 `SchemaDiffEngine` and `SchemaDiffPipelineService`. |
| `Analysis/` | `MigrationImpactAnalyzer` (breaking-change / data-loss risk scoring), `ConflictResolutionEngine` (per-conflict-type resolution strategies). |
| `Models/` | POCOs: `Migration`, `MigrationFile`, `MigrationDiff`, `SchemaChange`, `ConflictInfo`, `BranchInfo`, `MergeResult`, dependency graph types, enums. |
| `Formatters/` | Output rendering: `JsonFormatter`, `CsvFormatter`, `HtmlFormatter`, `VisualDiffFormatter` (implements `IVisualDiffRenderer`, side-by-side and unified HTML). |
| `Reports/` | `ReportEngine` - higher-level report assembly on top of formatters. |
| `Configuration/` | `AppSettings`, `EfMigrationDiffOptions` (IOptions-bound from the `EfMigrationDiff` config section), `DependencyInjection` (composition root). |
| `Interfaces/` | `ISchemaDiffEngine`, `IMergeEditor` - the seam between v1 and v2. |
| `Plugins/` | `PluginSystem` - reflection-based `IPlugin` loading from registered directories. |
| `Caching/` | `CacheService` - in-memory TTL cache with `ReaderWriterLockSlim` and a cleanup timer. |
| `Extensions/`, `Utilities/`, `Exceptions/` | Cross-cutting helpers, typed exceptions (`GitRepositoryException` etc.), `Constants`. |
| `tests/`, `ef-migration-diff.Benchmarks/`, `examples/` | Excluded from the main compile via `<Compile Remove>` in the csproj; the benchmarks project is a separate BenchmarkDotNet exe. |

## Key design decisions

**Parse migration source text, not a live model.**
Migrations are read as file contents per branch via LibGit2Sharp and parsed with
string/regex heuristics (`MigrationParserService`, `SchemaChangeDetectorService`).
Trade-off: no Roslyn dependency and no need to build or load the target project -
the tool works on any checkout instantly - at the cost of missing changes hidden
behind helper methods or unusual formatting. For the dominant case (generated
`migrationBuilder.*` calls) the heuristics are reliable.

**v1 / v2 split behind `ISchemaDiffEngine`.**
The original branch-comparison path (`MigrationDiffService` and friends) uses
concrete classes wired directly. The visual-diff/merge work came later and was
put behind interfaces (`ISchemaDiffEngine`, `IMergeEditor`, `IVisualDiffRenderer`)
with `SchemaDiffPipelineService` as the bridge. Rationale: don't retrofit
interfaces onto stable v1 code for their own sake; introduce seams where new
behaviour (rendering, merge strategies) actually needs swapping. `SchemaDiffEngine`
implements both diff and merge interfaces in one class on purpose - the merge
planner needs the same conflict-region data the differ produces.

**Composition root in `Configuration/DependencyInjection.cs`.**
Everything is registered in one place (`AddApplicationServices`), with
`CreateServiceProvider` overloads for the CLI entry point and for embedding the
tool as a library. Services are singletons because the app is a one-shot CLI:
no request scoping needed, and `MigrationRepository` holds the parsed state for
the run. `GitRepository` is transient and constructed from
`AppSettings.RepositoryPath`, since a run may open repositories at different
paths.

**Command middleware pipeline.**
`CommandExecutor` runs commands through an `ICommandMiddleware` chain
(error handling, logging, validation). For a CLI this is arguably heavier than a
try/catch in `Main`, but it gives one place to map exception types to exit codes
(`ErrorHandlingMiddleware.HandleException`) and keeps the command classes free of
logging/validation noise.

**Configuration via IOptions with startup validation.**
`EfMigrationDiffOptions` binds from the `EfMigrationDiff` section with
`ValidateOnStart()`, so a bad config fails fast instead of mid-comparison.
`AppSettings` remains as the simpler, imperatively-configured object used by the
CLI entry point; both exist because the options pattern was added later without
breaking the embedding API.

## Extension points

- **`ISchemaDiffEngine` / `IMergeEditor`** - swap the diff/merge algorithm.
- **`IVisualDiffRenderer`** - alternative output renderers; `VisualDiffFormatter`
  (HTML) is the default registration in `AddSchemaDiffServices`.
- **`ICommandMiddleware`** - add cross-cutting behaviour around command execution.
- **`PluginSystem` / `IPlugin`** - load extra behaviour from assemblies in
  registered plugin directories.
- **`ConflictResolutionEngine`** - strategies are a `ConflictType -> strategy`
  dictionary; new conflict types get a new entry.

## Known limitations

- Schema-change detection is text-based; hand-written migrations that build SQL
  dynamically or call custom helpers can be missed or misclassified.
- `MigrationRepository` is in-memory only - state lives for one process run.
- Most v1 services are registered and consumed as concrete classes, so unit
  tests exercise them directly rather than through mocks; acceptable for now,
  but anything that needs test doubles should get an interface like the v2 code.
- `CacheService` exists but the hot paths don't lean on it much yet; a large
  monorepo with hundreds of migrations is untested territory performance-wise
  (see the Benchmarks project for the current numbers).
- Single target: `net10.0`. Older SDKs can't build it (see `global.json`).
