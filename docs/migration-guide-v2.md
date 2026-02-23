# Migration Guide to v2.0

This guide provides instructions for upgrading from v1.x to v2.0 of EF Migration Diff.

## New in v2.0

Version 2.0 introduces a powerful visual schema comparison tool with a merge editor interface that enables developers to preview and resolve migration conflicts before they reach production.

## Key Features Added in v2.0

### Visual Schema Diff
- Side-by-side and unified diff views for comparing schema changes between branches
- Three-way merge editor for resolving conflicts between feature branches and integration targets
- Interactive HTML reports with visual conflict highlighting

### Breaking Changes

1. **Command Line Interface**: The `visual-diff` command has been added to generate visual schema comparisons
2. **Exit Codes**: New exit code 1 now indicates destructive schema changes detected
3. **Configuration**: New `SchemaDiffOptions` class for controlling diff rendering and comparison behavior

## Migration Steps

### 1. Update your CLI usage

The new visual diff command provides enhanced capabilities:

```bash
# Generate a visual diff between two branches
ef-migration-diff visual-diff --source develop --target main

# Generate a three-way merge editor with a common base
ef-migration-diff visual-diff --base main --source feature/user-profile --target develop --output merge.html
```

### 2. API Changes

The v2.0 API introduces new services for visual schema comparison:

```csharp
// New service dependencies
var diffEngine = new SchemaDiffEngine(conflictDetectionService);
var mergeEditor = new SchemaDiffEngine(); // implements IMergeEditor
var pipeline = new SchemaDiffPipelineService(migrationService, diffEngine, renderer);

// New models for schema comparison
var baseOptions = new SchemaDiffOptions
{
    IncludeSqlContent = true,
    IgnoreWhitespace = true
};
```

### 3. Configuration Changes

Add the new configuration options to your appsettings.json:

```json
{
  "EfMigrationDiff": {
    "SchemaDiff": {
      "IncludeSqlContent": true,
      "IgnoreWhitespace": false,
      "ContextLines": 3
    }
  }
}
```

## Code Examples

### Old vs New API

**v1.x approach:**
```csharp
// v1.x - Basic comparison only
var result = migrationDiffService.CompareBranches("main", "feature/users");
```

**v2.0 approach:**
```csharp
// v2.0 - Visual diff with enhanced capabilities
var options = SchemaDiffOptions.ForBranches("main", "feature/users");
var pipelineResult = schemaDiffPipelineService.RunTwoWayDiff(sourceBranch, targetBranch, options);

// v2.0 - Three-way merge support
var threeWayResult = schemaDiffPipelineService.RunThreeWayDiff(
    baseBranch, sourceBranch, targetBranch, 
    SchemaDiffOptions.ForMerge("main", "feature/users", "develop")
);
```

### Web UI Integration

v2.0 introduces a web-based merge editor that can be embedded in web applications:

```csharp
// In your web application
var html = schemaDiffPipelineService.RunTwoWayDiff(sourceBranch, targetBranch).SideBySideHtml;
Response.Write(html); // Direct HTML output for browser rendering
```

## New Models and Services

v2.0 adds several new model classes:

- `SchemaDiffResult` - Two-way diff output with hunks and change categorization
- `ThreeWayDiffResult` - Three-way merge analysis with conflict regions
- `SchemaDiffOptions` - Configuration object for diff rendering
- `MergeConflictRegion` - Structured conflict representation for UI presentation
- `MergeResolutionPlan` - Strategy pattern for merge resolution

## Docker Configuration

v2.0 enhances Docker support with new environment variables:

```yaml
- SCHEMA_DIFF_INCLUDE_SQL=true
- SCHEMA_DIFF_INCLUDE_METADATA=false
- SCHEMA_DIFF_CONTEXT_LINES=3
```

## Exit Code Changes

v2.0 introduces new exit codes:
- `0` - Success, no conflicts or trivially resolvable conflicts only
- `1` - Destructive changes or unresolvable conflicts detected

## Testing

v2.0 includes enhanced testing capabilities:

```csharp
// New testable components in v2.0
[Fact]
public void TestThreeWayDiff()
{
    var baseBranch = new BranchInfo("main", "");
    var sourceBranch = new BranchInfo("feature/users", "");
    var targetBranch = new BranchInfo("develop", "");
    
    var options = SchemaDiffOptions.ForMerge("main", "feature/users", "develop");
    var result = schemaDiffPipelineService.RunThreeWayDiff(
        baseBranch, sourceBranch, targetBranch, options);
    
    Assert.NotNull(result.ThreeWayDiff);
    Assert.Equal(0, result.ThreeWayDiff.ConflictCount);
}
```

## Backward Compatibility

v2.0 maintains full backward compatibility with v1.x. All existing v1.x functionality continues to work without changes, while new v2.0 features are strictly additive.

## Upgrade Checklist

- [ ] Update your project to use ef-migration-diff v2.0
- [ ] Review the new visual-diff command options
- [ ] Test the new three-way merge functionality
- [ ] Update your CI/CD pipelines to handle new exit codes
- [ ] Review the enhanced HTML reporting capabilities
- [ ] Check the new configuration options in your deployment