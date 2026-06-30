# ef-migration-diff

Compare Entity Framework Core migrations between Git branches - detect conflicts, preview schema changes, and block bad merges before they hit production.

![Build](https://github.com/sarmkadan/ef-migration-diff/actions/workflows/ci.yml/badge.svg)
![License](https://img.shields.io/github/license/sarmkadan/ef-migration-diff)
![.NET](https://img.shields.io/badge/.NET-10.0-512BD4)

## Installation

```bash
# Clone the repository
git clone https://github.com/Sarmkadan/ef-migration-diff.git
cd ef-migration-diff

# Build and install
dotnet build -c Release
dotnet publish -c Release -o ./publish
dotnet tool install --global --add-source ./publish ef-migration-diff
```

## Quick Start

```bash
# Compare migrations between two branches
ef-migration-diff compare main feature/add-users

# Validate migration files on the current branch
ef-migration-diff validate
```

## Usage

```bash
# Generate HTML side-by-side visual diff
ef-migration-diff visual-diff main feature/add-users

# Show migration dependency graph
ef-migration-diff graph

# Suggest auto-merge resolutions for conflicts
ef-migration-diff auto-merge main feature/add-users
```

## Examples

For more practical usage, including programmatic access and DI integration, check the [examples](examples) directory:

- [BasicUsage.cs](examples/BasicUsage.cs): Simple setup and migration comparison.
- [AdvancedUsage.cs](examples/AdvancedUsage.cs): Custom configuration and error handling.
- [IntegrationExample.cs](examples/IntegrationExample.cs): Dependency injection setup for ASP.NET applications.
- [basic-comparison.cs](examples/basic-comparison.cs): Git branch comparison.
- [conflict-detection.cs](examples/conflict-detection.cs): Detailed conflict analysis.

## Docker

To run the tool using Docker, you can use the provided `docker-compose.yml` file:

```bash
# Build and run the tool
docker-compose run --rm ef-migration-diff compare main feature/add-users
```

The Docker image includes `git` and the necessary dependencies to run the tool against your local repository.

## License

This project is licensed under the MIT License - see the [LICENSE](LICENSE) file for details.
Copyright (c) 2026 Vladyslav Zaiets.
