# Contributing to ef-migration-diff

We welcome contributions to `ef-migration-diff`! Please follow these guidelines to help us maintain a high-quality project.

## Getting Started

### Prerequisites

- .NET 10.0 SDK or later
- Git 2.30+
- A code editor with C# support (Visual Studio, Rider, or VS Code with C# Dev Kit)

### Development Setup

```bash
# Clone and build
git clone https://github.com/sarmkadan/ef-migration-diff.git
cd ef-migration-diff
dotnet restore
dotnet build

# Run the test suite
dotnet test

# Run with code coverage
dotnet test --collect:"XPlat Code Coverage"
```

## How to Contribute

### 1. Fork and Clone
- Fork the repository on GitHub.
- Clone your fork locally:
  ```bash
  git clone https://github.com/<your-username>/ef-migration-diff.git
  ```

### 2. Create a Branch
- Create a feature or bugfix branch from `main`:
  ```bash
  git checkout -b feature/my-new-feature
  ```
- Use descriptive branch names: `feature/add-mysql-support`, `fix/cache-expiry-race`, `docs/plugin-examples`.

### 3. Make Your Changes
- **Code Style:**
  - Follow existing conventions in the codebase (see `.editorconfig`).
  - Provide XML documentation for all public classes, methods, and properties.
  - Include `<param>`, `<returns>`, and `<exception>` tags on public API methods.
  - **IMPORTANT:** Keep all author headers intact. Do not remove or alter existing author headers in any files.
- **Architecture:**
  - Services go in `Services/`, data access in `Repositories/`, domain types in `Models/`.
  - New CLI commands go in `CLI/Commands/` and must inherit from the base command class.
  - Plugins should implement `IPlugin` from `Plugins/PluginSystem.cs`.

### 4. Run Tests
- Write tests for your changes. Place tests in `tests/` mirroring the source structure.
- Ensure all existing tests pass before submitting your PR:
  ```bash
  dotnet test
  ```
- For schema detection changes, include at least one test with a realistic EF migration file.

### 5. Verify Code Quality
- Run formatting and static analysis:
  ```bash
  dotnet format
  dotnet build /warnaserror
  ```
- Ensure zero warnings before submitting.

### 6. Submit a Pull Request
- Push your branch to your fork.
- Open a Pull Request against the `main` branch of this repository.
- Fill in the PR template with:
  - Summary of changes
  - Related issue numbers (if applicable)
  - Test plan describing how you verified the changes

## Code Review Checklist

Before submitting, verify:

- [ ] All public APIs have XML doc comments with `<summary>`, `<param>`, and `<returns>`
- [ ] No compiler warnings
- [ ] All existing tests pass
- [ ] New functionality has corresponding tests
- [ ] No hardcoded paths or credentials
- [ ] Thread safety considered for shared state (see `CacheService` and `EventBus` for patterns)
- [ ] Error cases handled with descriptive exceptions or validation

## Architecture Overview

```
Services/          - Business logic (diff, conflict detection, schema analysis)
Repositories/      - Data access (Git, migration files, DbContext discovery)
Models/            - Domain types and enums
Analysis/          - Impact analysis and conflict resolution
Caching/           - In-memory cache with TTL support
CLI/               - Command-line interface and argument parsing
Events/            - Event bus for decoupled communication
Plugins/           - Plugin loading and lifecycle management
Middleware/        - Request validation and error handling pipeline
Reports/           - Output formatters (JSON, CSV, HTML, Console)
Extensions/        - Utility extension methods
```

## Reporting Issues

Use GitHub Issues to report bugs or suggest features. When reporting a bug, please include:

- Clear title and description
- Steps to reproduce
- Expected vs actual behavior
- Your environment: OS, .NET SDK version, Git version
- Relevant error output or logs

## Adding a New Output Format

1. Create a formatter class in `Formatters/` implementing the formatter interface.
2. Register it in `DependencyInjection.cs`.
3. Add the format option to the CLI `compare` command.
4. Write tests covering edge cases (empty diffs, large datasets, special characters).
5. Update the README CLI reference table.

## License

By contributing to `ef-migration-diff`, you agree that your contributions will be licensed under the MIT License.
