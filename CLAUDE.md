# CLAUDE.md

.NET 10 console tool that compares Entity Framework Core migrations between git branches (via LibGit2Sharp), extracts schema changes from migration source text and flags conflicts before merge.

## Build

```bash
dotnet restore
dotnet build -c Release --no-restore      # or: make build
dotnet run -- --help                      # or: make run
dotnet publish -c Release -o ./publish    # or: make publish
```

SDK is pinned in `global.json` (10.0.100, rollForward latestMinor). CI (`.github/workflows/ci.yml`) builds on 8.0.x and 10.0.x.

## Test

```bash
dotnet test -c Release --no-build --verbosity normal   # or: make test (build first)
dotnet test tests/ef-migration-diff.Tests --filter "FullyQualifiedName~CommandParserTests"
```

Stack: xUnit + FluentAssertions + Moq (global usings for `Xunit` and `Moq` in the test csproj). Benchmarks: `dotnet run -c Release --project ef-migration-diff.Benchmarks` (BenchmarkDotNet).

## Lint / format

```bash
dotnet format                                   # make format
dotnet format --verify-no-changes               # make format-check
dotnet build --no-restore /p:TreatWarningsAsErrors=true   # make lint
make check                                      # format-check + lint + test
```

Style rules live in `.editorconfig` (4 spaces, max line 120, `var` when type apparent, System usings first). `TreatWarningsAsErrors` is off in `Directory.Build.props`; XML-doc warnings (CS1591 etc.) are suppressed.

## Layout

Single-project layout: the root directory is the main project (`ef-migration-diff.csproj`, root namespace `EfMigrationDiff`). `tests/`, `examples/` and `ef-migration-diff.Benchmarks/` are excluded from it via `<Compile Remove>`.

| Path | Role |
|---|---|
| `Program.cs` | Entry point. `MigrationDiffApplication` builds DI, maps command names (`compare`/`diff`, `validate`/`check`, `report`, `visual-diff`, `graph`, `auto-merge`, help/version) to handlers. |
| `CLI/` | `CommandParser`, `CommandExecutor` (middleware pipeline), `Commands/` (Compare, Validate, VisualDiff, Help). |
| `Middleware/` | `ICommandMiddleware` chain: error handling, request logging, validation. |
| `Repositories/` | `GitRepository` (LibGit2Sharp), `MigrationRepository` (in-memory store), `DbContextRepository`. |
| `Services/` | Core logic: `MigrationParserService`, `MigrationDiffService`, `SchemaChangeDetectorService`, `ConflictDetectionService`, `MigrationDependencyGraphService`, `MigrationAutoResolverService`, `ReportGenerationService`; v2 `SchemaDiffEngine` + `SchemaDiffPipelineService`. |
| `Analysis/` | `MigrationImpactAnalyzer`, `ConflictResolutionEngine`. |
| `Models/` | POCOs and enums (`Migration`, `MigrationDiff`, `SchemaChange`, `ConflictInfo`, `BranchInfo`, `MergeResult`). |
| `Formatters/` | JSON, CSV, HTML, Markdown, VisualDiff output. `Reports/ReportEngine` sits on top. |
| `Configuration/` | `AppSettings`, `EfMigrationDiffOptions`, `ConfigFileLoader`, `DependencyInjection` (composition root). Config file: `efmigrationdiff.json`, example `appsettings.example.json`. |
| `Interfaces/` | `ISchemaDiffEngine`, `IMergeEditor` - the v1/v2 seam. |
| `Plugins/`, `Caching/`, `Extensions/`, `Utilities/`, `Exceptions/` | Plugin loader, TTL cache, cross-cutting helpers, typed exceptions, `Constants`. |
| `tests/ef-migration-diff.Tests/` | xUnit tests, one file per service/feature. |
| `docs/` | `ARCHITECTURE.md` (read first for design rationale) plus per-class notes. |
| `Dockerfile`, `docker-compose*.yml` | Multi-stage image; `make docker`. |

Known clutter: `src/Services/SchemaChangeDetectorService.cs` (stray copy in namespace `Services`, not `EfMigrationDiff.Services`), `Services/*.cs.backup`, root file `}`, `commit.msg`/`CommitMsg.txt`. Do not extend these; the canonical code is in the root-level folders.

## Conventions

- Namespaces: file-scoped, `EfMigrationDiff.<Folder>` (e.g. `EfMigrationDiff.Services`). Nullable and implicit usings enabled.
- Classes: `*Service` for logic, `*Repository` for data access, `*Formatter` for output, `*Command` for CLI handlers, `*Middleware` for pipeline steps. Helper partials go in sibling files named `<Class>Extensions.cs`, `<Class>JsonExtensions.cs`, `<Class>Validation.cs`.
- Tests: `<Class>Tests.cs`, methods `Method_Scenario_ExpectedResult` (e.g. `Parse_MissingOptionValue_ShouldTreatAsFlag`), `[Fact]`/`[Theory]`, FluentAssertions `.Should()`.
- Errors: throw typed exceptions from `Exceptions/CustomExceptions.cs`; `ErrorHandlingMiddleware` maps them to exit codes.
- Migrations are parsed as source text with regex heuristics (no Roslyn) - keep new detectors in `SchemaChangeDetectorService` and cover them with tests.
- Commit messages: conventional prefixes (`feat:`, `fix:`, `docs:`, `chore:`).
