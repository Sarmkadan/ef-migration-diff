# Getting Started with ef-migration-diff

This guide will help you get up and running with ef-migration-diff in minutes.

## Prerequisites

Before you begin, ensure you have:

- **.NET 10 SDK** or later ([Download](https://dotnet.microsoft.com/download))
- **Git** (for repository operations)
- A **.NET project** with Entity Framework Core migrations
- **Basic understanding** of EF Core migrations

## Installation

### Step 1: Clone the Repository

```bash
git clone https://github.com/Sarmkadan/ef-migration-diff.git
cd ef-migration-diff
```

### Step 2: Build the Project

```bash
dotnet restore
dotnet build -c Release
```

### Step 3: Install Locally

Option A - Global Tool:
```bash
dotnet publish -c Release -o ./publish
dotnet tool install --global --add-source ./publish ef-migration-diff
```

Option B - Direct Usage:
```bash
dotnet run --project ef-migration-diff.csproj -- compare --help
```

### Step 4: Verify Installation

```bash
ef-migration-diff --version
# Output: ef-migration-diff version 1.2.0
```

## Your First Comparison

### Step 1: Navigate to Your Project

```bash
cd /path/to/your/dotnet/project
```

### Step 2: Run a Basic Comparison

Compare migrations between main and a feature branch:

```bash
ef-migration-diff compare --branch1 main --branch2 feature/add-users
```

You should see output like:

```
📊 Comparison Results:
   Has Differences: true
   Conflicts Found: 0
   Schema Changes: 2
```

### Step 3: Generate an HTML Report

```bash
ef-migration-diff compare \
  --branch1 main \
  --branch2 feature/add-users \
  --output html \
  --output-path ./migration-report.html
```

Open the generated HTML file in your browser to see a detailed report.

## Common Tasks

### Detect Migration Conflicts

```bash
ef-migration-diff compare \
  --branch1 main \
  --branch2 develop \
  --strict-mode
```

The tool will fail (exit code 1) if any conflicts are found.

### Preview Schema Changes

```bash
ef-migration-diff compare \
  --branch1 main \
  --branch2 feature/database-refactor \
  --include-schema-preview \
  --output json \
  --output-path ./schema-diff.json
```

### Validate Current Branch

```bash
ef-migration-diff validate
```

This checks for:
- Duplicate migration names
- Orphaned migrations
- Syntax errors

## Configuration

### Using appsettings.json

Create an `appsettings.json` file in your project:

```json
{
  "EfMigrationDiff": {
    "DefaultOutputFormat": "console",
    "MigrationsPath": "Data/Migrations",
    "CacheEnabled": true,
    "CacheTtlSeconds": 3600,
    "StrictModeDefault": false
  }
}
```

### Environment Variables

```bash
export EFMIGDIFF_MIGRATIONS_PATH="./src/Data/Migrations"
export EFMIGDIFF_CACHE_ENABLED=true
export EFMIGDIFF_STRICT_MODE=false

ef-migration-diff compare --branch1 main --branch2 feature/users
```

## Next Steps

- **Read** [API Reference](./api-reference.md) for detailed API documentation
- **Explore** [Architecture Guide](./architecture.md) to understand how ef-migration-diff works
- **Check** [Examples](../examples/) for real-world usage patterns
- **Deploy** using [Deployment Guide](./deployment.md)
- **Troubleshoot** with [FAQ](./faq.md)

## Getting Help

### Command Help

```bash
# Show all available commands
ef-migration-diff help

# Show help for specific command
ef-migration-diff help compare
ef-migration-diff help validate
```

### Common Issues

| Problem | Solution |
|---------|----------|
| Migrations not found | Specify correct path with `--migrations-path` |
| Git branch not found | Ensure branch exists with `git branch -a` |
| Permission denied | Try running with elevated privileges |
| Out of memory | Enable caching with `--use-cache` |

For more help, see [FAQ](./faq.md).
