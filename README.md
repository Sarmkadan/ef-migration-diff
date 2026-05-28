# ef-migration-diff

![CI](https://github.com/sarmkadan/ef-migration-diff/actions/workflows/ci.yml/badge.svg)
![License](https://img.shields.io/github/license/sarmkadan/ef-migration-diff)
![.NET](https://img.shields.io/badge/.NET-10.0-512BD4)

Compare Entity Framework Core migrations between Git branches - detect conflicts, preview schema changes, and block bad merges before they hit production.

## Install

```bash
git clone https://github.com/Sarmkadan/ef-migration-diff.git
cd ef-migration-diff
dotnet build -c Release
dotnet publish -c Release -o ./publish
dotnet tool install --global --add-source ./publish ef-migration-diff
```

## Usage

```bash
# Compare migrations between two branches
ef-migration-diff compare main feature/add-users

# Validate migration files on current branch
ef-migration-diff validate

# Generate HTML side-by-side visual diff
ef-migration-diff visual-diff main feature/add-users

# Show migration dependency graph
ef-migration-diff graph

# Suggest auto-merge resolutions for conflicts
ef-migration-diff auto-merge main feature/add-users
```

## CI/CD Integration

Use the reusable workflow in your own repository:

```yaml
jobs:
  migration-check:
    uses: sarmkadan/ef-migration-diff/.github/workflows/pr-migration-check.yml@main
    with:
      base-branch: main
      head-branch: ${{ github.head_ref }}
    secrets:
      github-token: ${{ secrets.GITHUB_TOKEN }}
```

Posts a summary comment on PRs and blocks merges when blocking conflicts are detected.

## Output Formats

- **console** - quick summary (default)
- **json** - machine-readable, suitable for automation
- **html** - interactive report with side-by-side diff and conflict highlighting
- **csv** - spreadsheet-friendly

```bash
ef-migration-diff compare main develop --output html --output-path report.html
```

## What gets detected

- Migration name conflicts between branches
- Schema changes: CREATE/ALTER/DROP TABLE, ADD/DROP/ALTER COLUMN, indexes, foreign keys
- Breaking changes: non-nullable column additions, table/column drops
- Dependency graph cycles that would prevent a clean migration run

## License

MIT - Vladyslav Zaiets
