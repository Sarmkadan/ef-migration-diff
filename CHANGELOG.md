# Changelog

All notable changes to this project will be documented in this file.

The format is based on [Keep a Changelog](https://keepachangelog.com/en/1.0.0/),
and this project adheres to [Semantic Versioning](https://semver.org/spec/v2.0.0.html).

## [Unreleased]

---

## [2.0.0] - 2025-08-09

### Added
- Add schema comparison tool with visual diff and merge editor
- Docker support with multi-stage builds
- Health check endpoints (/health, /health/ready)
- Integration test suite with xUnit
- Migration guide from v1.x

### Changed
- Upgraded to .NET 10.0
- Modern C# features (records, primary constructors)
- Improved API consistency

### Fixed
- Various edge cases found through testing

---

## [1.0.0] - 2025-06-09

### Added
- Docker and Docker Compose support
- GitHub Actions CI/CD workflow template
- Environment variable configuration support
- Health check service for deployment monitoring
- Background task queue for long-running analysis
- Comprehensive documentation: getting-started, architecture, API reference, deployment, FAQ
- Eight working code examples in `examples/` directory

### Changed
- Promoted from pre-release: API surfaces are now stable and covered by semantic versioning
- Improved conflict detection algorithm for edge cases in timestamped migration names
- Refactored service layer to reduce coupling between repositories and analysis services

### Fixed
- Memory leak in caching system when TTL expired during active comparison
- Incorrect dependency chain validation for migrations with shared base classes
- Git operation timeouts on repositories with large binary assets

### Security
- Input validation for Git branch names (reject shell-injection characters)
- GitHub token handling moved exclusively to environment variables

---

## [0.9.0] - 2025-05-21

### Added
- Plugin system (`Plugins/PluginSystem.cs`) for third-party analysis extensions
- Middleware pipeline: error handling, request logging, validation
- Event bus (`Events/EventBus.cs`) for decoupled cross-component notifications
- `MigrationImpactAnalyzer` for estimating row-level impact of schema changes

### Changed
- Replaced ad-hoc error propagation with structured `CustomExceptions` hierarchy
- CLI now emits structured exit codes (0 = success, 1 = conflicts found, 2 = error)

### Fixed
- Parsing of migrations with nullable reference type annotations
- Incorrect handling of empty migration files

---

## [0.8.0] - 2025-05-05

### Added
- GitHub PR integration: post comparison results as a PR comment
- `HttpClientWrapper` with retry policy and timeout configuration
- `ConflictResolutionEngine` with auto-resolution suggestions for common conflict patterns
- `MigrationAutoResolverService` for timestamp-based rename recommendations

### Changed
- Extended `CompareCommand` with `--post-to-github`, `--github-token`, `--github-repo`, `--github-pr` flags
- Report engine now streams output for HTML reports over 1 MB

### Fixed
- Git branch checkout failure on Windows when branch name contained slashes
- UTF-8 BOM in migration files caused parser to skip first operation

---

## [0.7.0] - 2025-04-18

### Added
- Caching layer (`Caching/CacheService.cs`) with configurable TTL
- `--use-cache` and `--cache-ttl` flags on the `compare` command
- Performance metrics collection (`Utilities/PerformanceMetrics.cs`)
- Batch analysis: compare multiple feature branches against a base in one invocation

### Changed
- Cached repeat comparisons now complete in under 20 ms regardless of dataset size
- Dependency injection wiring moved to `Configuration/DependencyInjection.cs`

### Fixed
- Race condition in parallel conflict detection when two threads accessed the same migration list

---

## [0.6.0] - 2025-04-03

### Added
- HTML report generation with summary table and per-migration details
- CSV export format for spreadsheet analysis
- `ReportGenerationService` and `ReportEngine` with pluggable formatter interface
- `--output` and `--output-path` flags on the `compare` command

### Changed
- Improved schema change detection accuracy for `AlterColumn` operations
- Better conflict severity classification (Critical / Warning / Info)

### Fixed
- Console formatter dropped the last migration entry in lists with odd counts

---

## [0.5.0] - 2025-03-20

### Added
- Breaking change detection: identifies DROP TABLE, DROP COLUMN, and NOT NULL constraint additions
- `SchemaChangeDetectorService` with per-operation categorisation (CREATE, ALTER, DROP)
- `--detect-breaking-changes` flag and dedicated section in console output
- `--include-schema-preview` flag for before/after schema state display

### Changed
- `MigrationDiffService` now returns a structured `MigrationDiff` model instead of raw strings
- Orphaned migration detection incorporated into the standard comparison pipeline

---

## [0.4.0] - 2025-03-06

### Added
- JSON output format for machine-readable results and CI/CD integration
- `ValidateCommand` with `--check-duplicates`, `--check-orphans`, `--check-syntax` flags
- `ValidationHelper` for reusable input sanitisation across CLI and API surfaces
- `DbContextRepository` for discovery and metadata extraction of DbContext classes

### Changed
- `MigrationRepository` now resolves relative migration paths from project root
- `HelpCommand` auto-generates option tables from command metadata

### Fixed
- `--branch` flag on `validate` was silently ignored when combined with `--strict-mode`

---

## [0.3.0] - 2025-02-19

### Added
- `ConflictDetectionService` with detection for duplicate migration names and broken dependency chains
- `MigrationParserService` for extracting `Up` / `Down` operations from C# migration files
- `ConflictInfo` and `SchemaChange` models with severity levels
- Custom migration path support via `--migrations-path` flag

### Changed
- Improved `GitRepository` to support detached HEAD states and shallow clones
- More readable console output with aligned columns and conflict count summary

### Fixed
- Parsing failure on migrations that used expression-bodied `Up` methods

---

## [0.2.0] - 2025-02-04

### Added
- Command-line interface: `compare` and `help` commands via `CommandParser` and `CommandExecutor`
- `AppSettings` and `ConfigurationBuilder` for `appsettings.json` support
- `StringExtensions`, `CollectionExtensions`, and `PathExtensions` utility helpers
- Basic unit test project with xUnit and FluentAssertions

### Changed
- Entry point refactored from a single `Program.cs` block to the `CLI/` layer
- `GitRepository` now validates branch existence before checkout

---

## [0.1.0] - 2025-01-22

### Added
- Initial release
- Core migration comparison between two Git branches
- `MigrationDiffService` with side-by-side migration list comparison
- `GitRepository` for branch checkout and file enumeration
- `MigrationRepository` for loading `.cs` migration files from a folder
- `Migration`, `MigrationFile`, and `BranchInfo` models
- Entity Framework Core 10.0 support
- Dependency injection wiring via `Microsoft.Extensions.DependencyInjection`
- Console output with plain-text diff summary

---

## Contributors

- **Vladyslav Zaiets** ([https://sarmkadan.com](https://sarmkadan.com)) - Creator & Maintainer

---

## Support

Report issues at [GitHub Issues](https://github.com/Sarmkadan/ef-migration-diff/issues)
