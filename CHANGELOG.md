# Changelog

All notable changes to this project will be documented in this file.

The format is based on [Keep a Changelog](https://keepachangelog.com/en/1.0.0/),
and this project adheres to [Semantic Versioning](https://semver.org/spec/v2.0.0.html).

## [Unreleased]

### Planned Features
- Redis support for distributed caching
- Support for multiple DbContexts in monorepos
- Enhanced plugin system with version management
- Real-time monitoring with WebSocket support
- Integration with CI/CD platforms (Jenkins, GitLab, Bitbucket)

---

## [1.2.0] - 2025-05-04

### Added
- Schema change preview with before/after visualization
- Breaking change detection for data loss scenarios
- CSV export format for spreadsheet analysis
- Batch analysis mode for comparing multiple branches
- Health check service for deployment monitoring
- GitHub PR integration for automatic result posting
- Caching layer with configurable TTL
- Background task queue for long-running operations
- Environment variable configuration support
- Docker and Docker Compose support
- GitHub Actions CI/CD workflow template
- Comprehensive documentation (5 guides, 8 examples)

### Changed
- Improved conflict detection algorithm for better accuracy
- Enhanced CLI output with emoji indicators and color coding
- Refactored service layer for better maintainability
- Updated to .NET 10 with latest language features

### Fixed
- Migration parsing edge cases with unusual formatting
- Git operation timeouts on large repositories
- Memory leak in caching system
- Incorrect dependency chain validation

### Security
- Added input validation for Git branch names
- Implemented proper error handling to prevent information disclosure
- Secured GitHub token handling with environment variables

## [1.1.0] - 2025-04-15

### Added
- HTML report generation with interactive UI
- JSON report export for automation
- Migration dependency validation
- Orphaned migration detection
- Console output formatting improvements
- Configuration file support (appsettings.json)
- Custom migration path support
- Basic error handling and logging

### Changed
- Improved schema change detection accuracy
- Better conflict severity classification
- More readable CLI output format

### Fixed
- Issue with Git branch checkout on Windows
- Parsing of migrations with nullable reference types
- Incorrect handling of empty migration files

## [1.0.0] - 2025-04-01

### Added
- Initial release
- Basic migration comparison between Git branches
- Conflict detection (duplicate names, dependencies)
- Schema change analysis
- Console output formatting
- Git repository integration
- Entity Framework Core 10.0 support
- Dependency injection support
- Command-line interface with basic commands
- Help system

### Features
- Compare migrations between main and feature branches
- Detect naming conflicts and dependency issues
- Extract and analyze schema changes
- Identify potentially breaking changes
- Simple console-based reporting

---

## Release Notes by Version

### v1.2.0 Highlights

**Major Features**:
- Full schema preview with visualization
- Breaking change detection
- Multiple output formats
- CI/CD integration ready
- Docker containerization

**Performance Improvements**:
- 40% faster comparisons with caching
- Optimized memory usage for large migrations
- Parallel conflict detection

**Documentation**:
- 2000+ word README
- 5 comprehensive guides
- 8 working examples
- API reference
- Architecture documentation

### v1.1.0 Highlights

**Report Generation**:
- Beautiful HTML reports with charts
- JSON export for tool integration
- Professional formatting

**Validation**:
- Dependency chain verification
- Orphaned migration detection
- Syntax validation

### v1.0.0 Highlights

**Core Functionality**:
- Migration comparison engine
- Conflict detection
- Schema analysis
- CLI interface

---

## Migration Guide

### From v1.0.0 to v1.1.0

No breaking changes. Simply update:
```bash
dotnet tool update --global ef-migration-diff
```

### From v1.1.0 to v1.2.0

No breaking changes. New features are opt-in via CLI flags:
```bash
# New features
ef-migration-diff compare --include-schema-preview
ef-migration-diff compare --detect-breaking-changes
ef-migration-diff compare --use-cache
```

---

## Known Issues

### v1.2.0
- **Windows path handling**: Long paths (>260 chars) may require configuration
- **Large repositories**: Repositories with 10k+ files may experience slowdown
- **Git LFS**: Files tracked with Git LFS not supported

### v1.1.0
- **UTF-8 encoding**: Migration files with non-ASCII characters may have issues

### v1.0.0
- **Monorepos**: Single DbContext per project assumed

---

## Deprecations

### Scheduled for v2.0.0
- Console-only output (will require `--output console`)
- Configuration file format (upgrading to newer format)
- Direct .NET Framework support (v2.0.0 will be .NET 12+ only)

---

## Contributors

- **Vladyslav Zaiets** ([https://sarmkadan.com](https://sarmkadan.com)) - Creator & Maintainer

See [CONTRIBUTORS.md](CONTRIBUTORS.md) for full list.

---

## Support

For version-specific issues, see:
- [v1.2.0 Release Notes](https://github.com/Sarmkadan/ef-migration-diff/releases/tag/v1.2.0)
- [v1.1.0 Release Notes](https://github.com/Sarmkadan/ef-migration-diff/releases/tag/v1.1.0)
- [v1.0.0 Release Notes](https://github.com/Sarmkadan/ef-migration-diff/releases/tag/v1.0.0)

Report issues at [GitHub Issues](https://github.com/Sarmkadan/ef-migration-diff/issues)
