# Frequently Asked Questions

Answers to common questions about ef-migration-diff.

## Installation & Setup

### Q: Can I use this tool without .NET 10?

**A**: No, ef-migration-diff requires .NET 10 SDK or later. This ensures access to the latest EF Core features and language improvements. You can download .NET 10 from [dotnet.microsoft.com](https://dotnet.microsoft.com/download).

### Q: Do I need to install it globally?

**A**: No, you have options:
- **Global**: `dotnet tool install --global ef-migration-diff` - Use anywhere
- **Local**: `dotnet run -- compare ...` - Use in project directory
- **Docker**: No installation needed - runs in container

### Q: How do I uninstall the tool?

**A**: 
```bash
dotnet tool uninstall --global ef-migration-diff
```

## Usage & Configuration

### Q: Why aren't my migrations being found?

**A**: Common causes:
1. **Wrong path**: Migrations are in custom directory
   ```bash
   ef-migration-diff compare --migrations-path ./Data/Migrations
   ```
2. **Branch doesn't exist**: Verify with `git branch -a`
3. **Files not committed**: Ensure migrations are in Git

### Q: Can I use custom migration folder names?

**A**: Yes, specify the path:
```bash
ef-migration-diff compare \
  --branch1 main \
  --branch2 develop \
  --migrations-path ./src/Infrastructure/Migrations
```

### Q: What if my DbContext is in a different project?

**A**: The tool will discover DbContext automatically. If it doesn't:
1. Ensure DbContext inherits from `DbContext`
2. Place it in a discoverable location
3. Configure the path in `appsettings.json`

### Q: Can I compare more than two branches?

**A**: Not directly, but you can:
```bash
# Compare each feature branch to main
for branch in feature/* ; do
  ef-migration-diff compare -b1 main -b2 "$branch"
done
```

## Performance & Optimization

### Q: The tool is running very slowly. How can I speed it up?

**A**: Try these optimizations:
1. **Enable caching**:
   ```bash
   ef-migration-diff compare \
     --branch1 main \
     --branch2 feature/users \
     --use-cache --cache-ttl 3600
   ```

2. **Increase available memory** (Docker):
   ```bash
   docker run --memory 2g ef-migration-diff:latest
   ```

3. **Reduce analysis scope**: Don't analyze if you don't need breaking change detection

### Q: What's the cache doing?

**A**: Caching stores analysis results for repeated comparisons:
- Results cached by branch pair and timestamp
- TTL (time-to-live) controls how long cache is valid
- Default 1 hour, adjustable via `--cache-ttl`

### Q: How do I clear the cache?

**A**: 
```bash
# Remove cache directory
rm -rf .cache

# Or disable caching temporarily
# (caching is still enabled, just not used this run)
```

### Q: Can I use Redis for distributed caching?

**A**: Not yet, but it's on the roadmap. Currently, caching is file-based.

## Output & Reporting

### Q: What output formats are supported?

**A**: 
- `console`: Summary in terminal (default)
- `json`: Machine-readable format
- `csv`: Spreadsheet-friendly
- `html`: Interactive report with charts

### Q: Can I generate multiple report formats at once?

**A**: Not in one command, but you can script it:
```bash
for format in json csv html; do
  ef-migration-diff compare \
    -b1 main -b2 develop \
    -o "$format" -op "report.$format"
done
```

### Q: How do I automate report generation in CI/CD?

**A**: Use the GitHub Actions workflow:
```yaml
- name: Generate migration report
  run: ef-migration-diff compare \
    --branch1 origin/main \
    --branch2 HEAD \
    --output html \
    --output-path ./report.html
```

### Q: Can I customize the HTML report?

**A**: Currently limited customization. You can:
- Choose light/dark theme (in report options)
- Modify CSS after generation
- Generate JSON and create custom report template

## Troubleshooting

### Q: I get "Git branch not found" error

**A**: Solutions:
1. **Check branch exists**: `git branch -a`
2. **Fetch remote**: `git fetch origin`
3. **Use full branch name**: `origin/branch-name` for remote branches

### Q: I get "Permission denied" errors

**A**: Solutions:
1. **Check file permissions**: `ls -la Migrations/`
2. **Run with elevated privileges**: `sudo ef-migration-diff compare ...`
3. **Fix permissions**: `chmod -R 755 ./Migrations`

### Q: I get "Out of memory" error

**A**: Solutions:
1. **Enable caching**:
   ```bash
   ef-migration-diff compare --use-cache
   ```
2. **Increase system memory** or Docker memory limit
3. **Reduce batch size** if processing many branches

### Q: Migrations appear to be missing

**A**: Verify:
1. **Files are committed**: `git status`
2. **Correct branch checked out**: `git branch`
3. **Correct path specified**: `--migrations-path ./path/to/migrations`
4. **Files follow naming pattern**: `[timestamp]_[name].cs`

### Q: Comparison results seem incorrect

**A**: Try:
1. **Clear cache**: `rm -rf .cache`
2. **Run without options**: Check default behavior
3. **Enable debug logging**: `LOG_LEVEL=Debug`
4. **Manually verify**: Check the migrations directly

## Integration & Automation

### Q: How do I integrate with GitHub?

**A**: Use the built-in GitHub integration:
```bash
ef-migration-diff compare \
  --branch1 main \
  --branch2 feature/users \
  --post-to-github \
  --github-token $GITHUB_TOKEN \
  --github-repo owner/repo \
  --github-pr 42
```

### Q: Can I use this in Azure DevOps?

**A**: Yes, use in Azure Pipelines:
```yaml
- task: DotNetCoreCLI@2
  inputs:
    command: 'custom'
    custom: 'run'
    arguments: '-- compare --branch1 main --branch2 $(Build.SourceBranchName)'
```

### Q: Can I integrate with Jenkins?

**A**: Yes, using shell script step:
```bash
#!/bin/bash
cd /path/to/dotnet/project
dotnet run --project ef-migration-diff.csproj -- \
  compare --branch1 main --branch2 $GIT_BRANCH
```

### Q: How do I fail a build if migrations have conflicts?

**A**: Use strict mode, which returns exit code 1 on conflicts:
```bash
ef-migration-diff compare --branch1 main --branch2 develop --strict-mode
if [ $? -ne 0 ]; then
  echo "Migration conflicts detected!"
  exit 1
fi
```

## Advanced Features

### Q: What are "breaking changes" exactly?

**A**: Operations that could cause data loss:
- Dropping tables
- Dropping columns
- Changing column types incompatibly
- Removing constraints

The tool flags these for review before deployment.

### Q: How does conflict detection work?

**A**: It checks for:
- Duplicate migration names
- Circular dependencies
- Broken migration chains
- Incompatible schema modifications

### Q: Can I write custom analysis rules?

**A**: Yes, using the plugin system or by extending services:
```csharp
public class CustomAnalyzer : IAnalyzer
{
    public Task<List<ConflictInfo>> AnalyzeAsync(...)
    {
        // Your custom logic
    }
}
```

### Q: What databases are supported?

**A**: EF Core migrations work with all supported databases:
- SQL Server
- PostgreSQL
- MySQL
- SQLite
- Oracle
- Others (via provider)

The tool analyzes migrations, not database-specific syntax.

## Security & Compliance

### Q: Is my data secure when using this tool?

**A**: 
- No data is sent to external servers
- All analysis happens locally
- Git repository access only reads files
- Configure sensitive data handling as needed

### Q: Can I run this in air-gapped environments?

**A**: Yes, the tool has no external dependencies. It works entirely offline.

### Q: How do I handle sensitive connection strings?

**A**: 
1. **Use environment variables**: Don't commit `appsettings.json`
2. **Use secrets manager**: Azure KeyVault, AWS Secrets Manager
3. **Use user secrets**: `dotnet user-secrets`

```bash
dotnet user-secrets set "ConnectionString" "Server=..."
```

## Troubleshooting Commands

### Q: What debugging tools are available?

**A**: 
```bash
# Enable debug logging
export LOG_LEVEL=Debug
ef-migration-diff compare --branch1 main --branch2 develop

# Show help
ef-migration-diff help
ef-migration-diff help compare

# Check version and install status
ef-migration-diff --version
dotnet tool list --global
```

## Reporting Issues

### Q: Where do I report bugs?

**A**: Please create an issue on [GitHub](https://github.com/Sarmkadan/ef-migration-diff/issues) with:
1. Clear description of the issue
2. Steps to reproduce
3. Expected vs actual behavior
4. Your environment (OS, .NET version)
5. Relevant error messages or logs

### Q: I have a feature request

**A**: Open a feature request on [GitHub Discussions](https://github.com/Sarmkadan/ef-migration-diff/discussions) describing:
1. What you want to do
2. Why it would be useful
3. Any related use cases

## Getting Help

### Q: Where can I find more documentation?

**A**: 
- [README](../README.md) - Overview and quick start
- [Getting Started](./getting-started.md) - Installation and basics
- [Architecture](./architecture.md) - How it works
- [API Reference](./api-reference.md) - Complete API docs
- [Deployment](./deployment.md) - Production deployment

### Q: Are there examples?

**A**: Yes, see the [examples/](../examples/) directory with:
- Basic comparison
- Conflict detection
- Schema preview
- Report generation
- Batch analysis
- CI/CD validation
- Custom analysis
- Library usage

### Q: How do I contact the author?

**A**: 
- **Portfolio**: [https://sarmkadan.com](https://sarmkadan.com)
- **GitHub**: [@Sarmkadan](https://github.com/Sarmkadan)
- **Telegram**: [@sarmkadan](https://t.me/sarmkadan)
