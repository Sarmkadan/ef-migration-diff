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

## Configuration

The application can be configured via `appsettings.json` or environment variables to customize output paths, default comparison labels, or plugin behavior.

## License

This project is licensed under the MIT License - see the [LICENSE](LICENSE) file for details.
Copyright (c) 2026 Vladyslav Zaiets.
