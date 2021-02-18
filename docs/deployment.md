# Deployment Guide

Instructions for deploying ef-migration-diff in various environments.

## Docker Deployment

### Building Docker Image

Create a `Dockerfile` in the project root (see root Dockerfile for details):

```bash
docker build -t ef-migration-diff:1.2.0 .
docker tag ef-migration-diff:1.2.0 ef-migration-diff:latest
```

### Running with Docker

```bash
docker run --rm \
  -v /path/to/project:/workspace \
  ef-migration-diff:latest \
  compare --branch1 main --branch2 feature/users
```

### Docker Compose

Use the provided `docker-compose.yml` for local development:

```bash
docker-compose up
```

## Local Installation

### Installation Steps

1. **Clone repository**:
```bash
git clone https://github.com/Sarmkadan/ef-migration-diff.git
cd ef-migration-diff
```

2. **Build release**:
```bash
dotnet publish -c Release -o ./publish
```

3. **Install as global tool**:
```bash
dotnet tool install --global --add-source ./publish ef-migration-diff
```

4. **Verify**:
```bash
ef-migration-diff --version
```

### Uninstall

```bash
dotnet tool uninstall --global ef-migration-diff
```

## CI/CD Integration

### GitHub Actions

Use the provided workflow in `.github/workflows/build.yml`:

```yaml
name: Build and Test

on: [push, pull_request]

jobs:
  build:
    runs-on: ubuntu-latest
    steps:
      - uses: actions/checkout@v3
      - uses: actions/setup-dotnet@v3
        with:
          dotnet-version: '10.0.x'
      - run: dotnet restore
      - run: dotnet build -c Release
      - run: dotnet test
```

### Azure Pipelines

```yaml
trigger:
  - main

pool:
  vmImage: 'ubuntu-latest'

variables:
  buildConfiguration: 'Release'

steps:
  - task: UseDotNet@2
    inputs:
      version: '10.0.x'
  
  - task: DotNetCoreCLI@2
    inputs:
      command: 'restore'
  
  - task: DotNetCoreCLI@2
    inputs:
      command: 'build'
      arguments: '-c $(buildConfiguration)'
  
  - task: DotNetCoreCLI@2
    inputs:
      command: 'test'
      arguments: '-c $(buildConfiguration)'
```

### GitLab CI

```yaml
image: mcr.microsoft.com/dotnet/sdk:10.0

stages:
  - build
  - test

build:
  stage: build
  script:
    - dotnet restore
    - dotnet build -c Release

test:
  stage: test
  script:
    - dotnet test
```

## Integration in Pull Request Workflows

### GitHub Actions Example

Create `.github/workflows/migration-check.yml`:

```yaml
name: Migration Validation

on:
  pull_request:
    types: [opened, synchronize, reopened]

jobs:
  validate-migrations:
    runs-on: ubuntu-latest
    steps:
      - uses: actions/checkout@v3
        with:
          fetch-depth: 0
      
      - uses: actions/setup-dotnet@v3
        with:
          dotnet-version: '10.0.x'
      
      - name: Restore
        run: dotnet restore
      
      - name: Build
        run: dotnet build -c Release
      
      - name: Validate migrations
        run: dotnet run -- validate --strict-mode
      
      - name: Compare with main
        run: dotnet run -- compare \
          --branch1 origin/main \
          --branch2 HEAD \
          --strict-mode \
          --detect-breaking-changes
```

## Performance Optimization

### Production Settings

For production deployments, optimize configuration:

```json
{
  "EfMigrationDiff": {
    "CacheEnabled": true,
    "CacheTtlSeconds": 7200,
    "Performance": {
      "MaxConcurrentAnalysis": 8,
      "EnableCaching": true,
      "CacheDirectory": "/var/cache/ef-migration-diff"
    },
    "Logging": {
      "LogLevel": "Warning"
    }
  }
}
```

### Caching Strategy

1. **Enable caching** for repeated comparisons
2. **Set appropriate TTL** (2-3 hours for production)
3. **Use persistent storage** for cache directory
4. **Monitor cache size** to prevent disk issues

### Resource Limits

For Docker/Kubernetes:

```yaml
resources:
  requests:
    memory: "256Mi"
    cpu: "100m"
  limits:
    memory: "512Mi"
    cpu: "500m"
```

## Troubleshooting Deployment

### Issue: "dotnet: command not found"

**Solution**: Install .NET SDK
```bash
# Ubuntu/Debian
wget https://dot.net/v1/dotnet-install.sh
sudo chmod +x dotnet-install.sh
./dotnet-install.sh

# macOS
brew install dotnet-sdk
```

### Issue: Docker build fails

**Solution**: Ensure Docker file is up to date
```bash
docker build --no-cache -t ef-migration-diff:latest .
```

### Issue: Out of memory during analysis

**Solution**: Increase available memory or enable caching
```bash
# Docker: increase memory limit
docker run --memory 2g ef-migration-diff:latest

# Or enable caching in config
"CacheEnabled": true
```

### Issue: Git operations timeout

**Solution**: Increase timeout and check Git configuration
```bash
# Set Git timeout
git config http.postBuffer 524288000

# Verify repository access
git remote -v
```

## Monitoring and Logging

### Enable Debug Logging

```json
{
  "EfMigrationDiff": {
    "Logging": {
      "LogLevel": "Debug"
    }
  }
}
```

Or via environment variable:

```bash
export EFMIGDIFF_LOG_LEVEL=Debug
ef-migration-diff compare --branch1 main --branch2 feature/users
```

### Log Locations

- **Console Output**: Direct stdout/stderr
- **File Logging** (if configured): See `appsettings.json`
- **Docker Logs**: `docker logs <container-id>`

### Health Checks

```bash
# Check if tool is working
ef-migration-diff --version

# Validate configuration
ef-migration-diff validate --branch main

# Test Git access
git status
```

## Rollback Procedure

If deployment issues occur:

```bash
# Uninstall current version
dotnet tool uninstall --global ef-migration-diff

# Reinstall previous version
dotnet tool install --global --add-source ./publish-v1.1.0 ef-migration-diff
```

## Security Considerations

### Access Control

1. **Restrict Git repository access** to authorized users
2. **Limit report output** to appropriate stakeholders
3. **Secure API tokens** (use environment variables, not hardcoded)

### GitHub API Token

```bash
# Use environment variable
export GITHUB_TOKEN=${{ secrets.GITHUB_TOKEN }}

# Reference in configuration
{
  "GitHub": {
    "Token": "${GITHUB_TOKEN}"
  }
}
```

### CORS Configuration

For web-based integration:

```csharp
services.AddCors(options =>
{
    options.AddPolicy("AllowSpecificOrigin",
        builder => builder
            .WithOrigins("https://yourdomain.com")
            .AllowAnyMethod()
            .AllowAnyHeader());
});
```

## Scaling Considerations

### Horizontal Scaling

For multiple instances:

1. **Use shared cache** (Redis)
2. **Load balance** requests
3. **Coordinate Git operations** to avoid conflicts

### Vertical Scaling

For single-instance optimization:

1. **Increase memory** allocation
2. **Enable caching** with long TTL
3. **Parallel processing** for batch operations

## Backup and Recovery

### Backup Strategy

```bash
# Backup configuration
cp appsettings.json appsettings.backup.json

# Backup cache
cp -r .cache .cache.backup

# Backup reports
cp -r ./reports ./reports.backup
```

### Recovery Steps

```bash
# Restore from backup
cp appsettings.backup.json appsettings.json

# Clear cache to force refresh
rm -rf .cache

# Verify operation
ef-migration-diff validate
```

## Version Management

### Update to Latest Version

```bash
# Check current version
ef-migration-diff --version

# Update global tool
dotnet tool update --global ef-migration-diff

# Verify update
ef-migration-diff --version
```

### Pin to Specific Version

```bash
dotnet tool install --global --version 1.2.0 ef-migration-diff
```
