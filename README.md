## MigrationParserServiceTests

The `MigrationParserServiceTests` class provides a set of unit tests for the `MigrationParserService` class, which parses Entity Framework Core migration files to extract metadata such as migration ID, name, and content. It tests various scenarios including valid migration files, designer files, invalid timestamps, empty content, and complex migration scenarios.

Here's an example of how to use the `MigrationParserService` class:

```csharp
// Create a parser instance
var parser = new MigrationParserService();

// Parse a valid migration file
var migrationFile = new MigrationFile
{
    FileName = "20240115093045_CreateUsersTable.cs",
    Content = "migrationBuilder.CreateTable(name: \"Users\", table => new { Id = table.Column<int>() })",
    DbContextName = "ApplicationDbContext"
};

var migration = parser.ParseMigrationFile(migrationFile);

// Extract migration metadata
Console.WriteLine($"Migration ID: {migration.Id}");           // "20240115093045"
Console.WriteLine($"Migration Name: {migration.Name}");       // "CreateUsersTable"
Console.WriteLine($"DbContext: {migration.DbContextName}");    // "ApplicationDbContext"
Console.WriteLine($"Content Length: {migration.Content.Length}");

// Parse a designer file (extracts the same migration ID)
var designerFile = new MigrationFile
{
    FileName = "20240115093045_CreateUsersTable.Designer.cs",
    Content = "// Designer file content",
    DbContextName = "ApplicationDbContext"
};

var designerMigration = parser.ParseMigrationFile(designerFile);
Console.WriteLine(designerMigration.Id); // "20240115093045"

// Handle invalid timestamp
var invalidFile = new MigrationFile
{
    FileName = "InvalidTimestamp_CreateUsersTable.cs",
    Content = "migrationBuilder.CreateTable(...)",
    DbContextName = "ApplicationDbContext"
};

var invalidResult = parser.ParseMigrationFile(invalidFile);
Console.WriteLine(invalidResult); // null

// Parse empty content migration
var emptyFile = new MigrationFile
{
    FileName = "20240115093045_EmptyMigration.cs",
    Content = string.Empty,
    DbContextName = "ApplicationDbContext"
};

var emptyMigration = parser.ParseMigrationFile(emptyFile);
Console.WriteLine(emptyMigration.Content); // ""
```

These tests ensure that the migration parser correctly extracts metadata from various migration file formats and handles edge cases appropriately.


## MigrationServicesTests

The `MigrationServicesTests` class provides unit tests for the `MigrationServices` class, which detects and validates changes between Entity Framework Core migrations. It tests various scenarios including table creation conflicts, column modification conflicts, and safe migration execution.

Here's an example of how to use the `MigrationServices` class:

```csharp
// Create a migration services instance
var migrationServices = new MigrationServices();

// Test detecting changes with a CreateTable operation
var createTableChange = migrationServices.DetectChanges_WithCreateTableContent_DetectsOneCreateTableChange();
Assert.Equal(1, createTableChange.Count);

// Test checking if a migration is safe when dropping a table
var isSafe = migrationServices.IsMigrationSafe_WithDropTableContent_ReturnsFalse();
Assert.False(isSafe);

// Test detecting naming conflicts when same table is created with different schemas
var namingConflict = migrationServices.DetectConflicts_WhenSameTableCreatedWithDifferentSchema_ReturnsNamingConflict();
Assert.NotNull(namingConflict);

// Test detecting column conflicts when same column is modified with different default values
var columnConflict = migrationServices.DetectConflicts_WhenSameColumnModifiedWithDifferentDefaultValue_ReturnsColumnConflict();
Assert.NotNull(columnConflict);

// Test that no conflicts are detected when same column is modified with same default value
var noConflicts = migrationServices.DetectConflicts_WhenSameColumnModifiedWithSameDefaultValue_ReturnsNoConflicts();
Assert.Null(noConflicts);

// Test async execution with a registered mocked command
await migrationServices.ExecuteAsync_WithRegisteredMockedCommand_InvokesCommandExactlyOnce();
Assert.True(commandWasInvoked);
```

These tests ensure that migration changes are properly detected, conflicts are identified, and migrations can be safely executed.



## ConflictDetectionServiceTests

The `ConflictDetectionServiceTests` class provides unit tests for the `ConflictDetectionService` class, which detects conflicts between Entity Framework Core migration schema changes. It tests various scenarios including table conflicts, column conflicts, index conflicts, and safe migration execution.

Here's an example of how to use the `ConflictDetectionService` class:

```csharp
// Create a conflict detection service instance
var conflictDetectionService = new ConflictDetectionService(NullLogger<ConflictDetectionService>.Instance);

// Test detecting no conflicts when there are no changes
var noChangesConflicts = conflictDetectionService.DetectConflicts(new List<SchemaChange>(), new List<SchemaChange>());
Assert.Empty(noChangesConflicts);

// Test detecting table conflicts when same table is created and dropped
var tableConflictChanges = new List<SchemaChange>
{
    new SchemaChange("m1", SqlChangeType.CreateTable, "CREATE TABLE Users") { TableName = "Users" }
};
var targetTableConflictChanges = new List<SchemaChange>
{
    new SchemaChange("m2", SqlChangeType.DropTable, "DROP TABLE Users") { TableName = "Users" }
};
var tableConflicts = conflictDetectionService.DetectConflicts(tableConflictChanges, targetTableConflictChanges);
Assert.Single(tableConflicts);
Assert.Equal(ConflictType.TableConflict, tableConflicts.First().ConflictType);
Assert.Equal(ConflictSeverity.Error, tableConflicts.First().Severity);

// Test detecting column conflicts when same column is added and dropped
var columnConflictChanges = new List<SchemaChange>
{
    new SchemaChange("m1", SqlChangeType.AddColumn, "ALTER TABLE Users ADD Name") { TableName = "Users", ColumnName = "Name" }
};
var targetColumnConflictChanges = new List<SchemaChange>
{
    new SchemaChange("m2", SqlChangeType.DropColumn, "ALTER TABLE Users DROP COLUMN Name") { TableName = "Users", ColumnName = "Name" }
};
var columnConflicts = conflictDetectionService.DetectConflicts(columnConflictChanges, targetColumnConflictChanges);
Assert.Single(columnConflicts);
Assert.Equal(ConflictType.ColumnConflict, columnConflicts.First().ConflictType);

// Test detecting index conflicts when same index is created and dropped
var indexConflictChanges = new List<SchemaChange>
{
    new SchemaChange("m1", SqlChangeType.CreateIndex, "CREATE INDEX Idx_Users_Name ON Users(Name)") { TableName = "Users" }
};
indexConflictChanges.First().AddMetadata("IndexName", "Idx_Users_Name");
var targetIndexConflictChanges = new List<SchemaChange>
{
    new SchemaChange("m2", SqlChangeType.DropIndex, "DROP INDEX Idx_Users_Name ON Users") { TableName = "Users" }
};
targetIndexConflictChanges.First().AddMetadata("IndexName", "Idx_Users_Name");
var indexConflicts = conflictDetectionService.DetectConflicts(indexConflictChanges, targetIndexConflictChanges);
Assert.Single(indexConflicts);
Assert.Equal(ConflictType.IndexConflict, indexConflicts.First().ConflictType);
Assert.Equal(ConflictSeverity.Warning, indexConflicts.First().Severity);

// Test that non-conflicting changes return empty list
var nonConflictingChanges = new List<SchemaChange>
{
    new SchemaChange("m1", SqlChangeType.CreateTable, "CREATE TABLE Users") { TableName = "Users" }
};
var targetNonConflictingChanges = new List<SchemaChange>
{
    new SchemaChange("m2", SqlChangeType.CreateTable, "CREATE TABLE Products") { TableName = "Products" }
};
var noConflicts = conflictDetectionService.DetectConflicts(nonConflictingChanges, targetNonConflictingChanges);
Assert.Empty(noConflicts);
```

These tests ensure that the conflict detection service correctly identifies conflicts between migration schema changes and returns appropriate severity levels.


## ReportGenerationServiceTests

The `ReportGenerationServiceTests` class provides unit tests for the `ReportGenerationService` class, which generates comprehensive reports comparing Entity Framework Core migrations. It tests various output formats (text, JSON, HTML) and includes detailed summaries of migration differences, conflicts, schema changes, and destructive operations.

Here's an example of how to use the `ReportGenerationService` class:

```csharp
// Create a report generation service instance
var reportService = new ReportGenerationService();

// Generate a text report with conflicts
var textReport = reportService.GenerateTextReport_WithDiffContainingConflicts_IncludesConflictSummary(
    new List<MigrationDiff> { /* your migration diffs with conflicts */ },
    new ReportOptions { Format = ReportFormat.Text, IncludeTimestamp = true }
);
Console.WriteLine(textReport);

// Generate a JSON report with multiple migrations
var jsonReport = reportService.GenerateJsonReport_WithMultipleMigrations_IncludesMigrationSummary(
    new List<MigrationDiff> { /* your migration diffs */ },
    new ReportOptions { Format = ReportFormat.Json, IncludeSchemaChanges = true }
);
Console.WriteLine(jsonReport);

// Generate an HTML report with schema changes
var htmlReport = reportService.GenerateHtmlReport_WithSchemaChanges_IncludesSchemaChangeSummary(
    new List<MigrationDiff> { /* your migration diffs */ },
    new ReportOptions { Format = ReportFormat.Html, IncludeDestructiveChanges = true }
);
Console.WriteLine(htmlReport);

// Generate a clean comparison report with no issues
var cleanReport = reportService.GenerateTextReport_WithNoIssues_ReportsCleanComparison(
    new List<MigrationDiff> { /* your clean migration diffs */ },
    new ReportOptions { Format = ReportFormat.Text }
);
Assert.Contains("No issues found", cleanReport);

// Generate a conflict summary
var conflictSummary = reportService.GenerateConflictSummary_WithConflicts_IncludesAllConflictDetails(
    new List<MigrationDiff> { /* your migration diffs with conflicts */ },
    new ConflictSummaryOptions { IncludeSeverity = true, GroupByType = true }
);
Console.WriteLine(conflictSummary);
```

These tests ensure that migration comparison reports are generated correctly across all supported formats and include all relevant information about differences, conflicts, and schema changes.

## MigrationDependencyGraphTests

The `MigrationDependencyGraphTests` class provides unit tests for the `MigrationDependencyGraph` class, which builds and analyzes dependency graphs between Entity Framework Core migrations. It tests various scenarios including graph construction, topological ordering, cycle detection, and impact analysis for rollback operations.

Here's an example of how to use the `MigrationDependencyGraph` class:

```csharp
// Create a graph instance
var graph = new MigrationDependencyGraph();

// Build a graph with empty list of migrations
var emptyGraph = graph.Build(new List<MigrationInfo>());
Assert.Empty(emptyGraph.Nodes);
```

These tests ensure that migration dependency graphs are correctly constructed and analyzed, enabling safe migration execution and rollback operations.

## AutoMergeSuggestionsTests

The `AutoMergeSuggestionsTests` class provides unit tests for the `MigrationAutoResolverService` class, which automatically resolves merge conflicts between Entity Framework Core migrations. It tests various conflict resolution strategies including automatic resolution for index and constraint conflicts, and manual resolution requirements for table and column conflicts.

Here's an example of how to use the `MigrationAutoResolverService` class:

```csharp
// Create an auto-merge resolver service instance
var autoResolver = new MigrationAutoResolverService(NullLogger<MigrationAutoResolverService>.Instance);

// Configure custom strategy for specific conflict type
autoResolver.ConfigureStrategy(ConflictType.NameConflict, MergeStrategy.LastWins);

// Get the default strategy for a conflict type
var defaultStrategy = autoResolver.GetStrategy(ConflictType.IndexConflict);
// Returns: MergeStrategy.Skip

var unregisteredStrategy = autoResolver.GetStrategy(ConflictType.TableConflict);
// Returns: null (no default strategy)

// Resolve conflicts with no conflicts (returns empty result)
var noConflictsResult = await autoResolver.ResolveAsync(Enumerable.Empty<ConflictInfo>());
Console.WriteLine(noConflictsResult.TotalConflicts); // 0
Console.WriteLine(noConflictsResult.IsFullyResolved); // true

// Resolve an index conflict (auto-resolves via Skip strategy)
var indexConflict = new ConflictInfo("mig_src", "mig_tgt", ConflictType.IndexConflict)
{
    Severity = ConflictSeverity.Warning,
    Description = "Duplicate index on Users table"
};
var indexResult = await autoResolver.ResolveAsync(new[] { indexConflict });
Console.WriteLine(indexResult.ResolvedCount); // 1
Console.WriteLine(indexResult.UnresolvedCount); // 0

// Resolve a constraint conflict (auto-resolves via Combine strategy)
var constraintConflict = new ConflictInfo("mig_src", "mig_tgt", ConflictType.ConstraintConflict)
{
    Severity = ConflictSeverity.Warning,
    Description = "Constraint added on both branches"
};
constraintConflict.AddDetail("SourceSql", "ADD CONSTRAINT FK_Orders_Users ...");
constraintConflict.AddDetail("TargetSql", "ADD CONSTRAINT FK_Products_Users ...");
var constraintResult = await autoResolver.ResolveAsync(new[] { constraintConflict });
Console.WriteLine(constraintResult.ResolvedCount); // 1

// Resolve a column conflict (leaves unresolved - requires manual resolution)
var columnConflict = new ConflictInfo("mig_src", "mig_tgt", ConflictType.ColumnConflict)
{
    Severity = ConflictSeverity.Error,
    Description = "Column definition conflict"
};
var columnResult = await autoResolver.ResolveAsync(new[] { columnConflict });
Console.WriteLine(columnResult.ResolvedCount); // 0
Console.WriteLine(columnResult.UnresolvedConflicts.Count); // 1

// Resolve mixed conflicts (partially resolves)
var mixedConflicts = new[]
{
    new ConflictInfo("mig1", "mig2", ConflictType.IndexConflict)
    {
        Severity = ConflictSeverity.Warning,
        Description = "Duplicate index"
    },
    new ConflictInfo("mig3", "mig4", ConflictType.TableConflict)
    {
        Severity = ConflictSeverity.Critical,
        Description = "Table conflict"
    }
};
var mixedResult = await autoResolver.ResolveAsync(mixedConflicts);
Console.WriteLine(mixedResult.ResolvedCount); // 1
Console.WriteLine(mixedResult.UnresolvedConflicts.Count); // 1
Console.WriteLine(mixedResult.IsFullyResolved); // false
```

These tests ensure that the auto-merge resolver correctly identifies and applies appropriate strategies for different conflict types, providing automatic resolution where safe and flagging conflicts that require manual intervention.

```csharp
// Create a graph instance
var graph = new MigrationDependencyGraph();

// Build a graph with empty list of migrations
var emptyGraph = graph.Build(new List<MigrationInfo>());
Assert.Empty(emptyGraph.Nodes);

// Build a graph with a single migration
var singleMigration = new MigrationInfo("20240115093045", "CreateUsersTable", "ApplicationDbContext");
var singleNodeGraph = graph.Build(new List<MigrationInfo> { singleMigration });
Assert.Single(singleNodeGraph.Nodes);
Assert.Equal("20240115093045", singleNodeGraph.Nodes.First().MigrationId);

// Build a graph with two migrations that have a sequential dependency
var migration1 = new MigrationInfo("20240115093045", "CreateUsersTable", "ApplicationDbContext");
var migration2 = new MigrationInfo("20240115093046", "AddEmailToUsers", "ApplicationDbContext");
migration2.AddDependency("20240115093045"); // migration2 depends on migration1

var sequentialGraph = graph.Build(new List<MigrationInfo> { migration1, migration2 });
Assert.Equal(2, sequentialGraph.Nodes.Count);
Assert.Single(sequentialGraph.Edges);
Assert.Equal("20240115093045", sequentialGraph.Edges.First().Source);
Assert.Equal("20240115093046", sequentialGraph.Edges.First().Target);

// Build a graph where migrations touch the same table
var tableA = new MigrationInfo("20240115093045", "CreateUsersTable", "ApplicationDbContext");
tableA.AddTableDependency("Users");

var tableB = new MigrationInfo("20240115093046", "AddEmailToUsers", "ApplicationDbContext");
tableB.AddTableDependency("Users");

var sharedTableGraph = graph.Build(new List<MigrationInfo> { tableA, tableB });
Assert.Equal(2, sharedTableGraph.Nodes.Count);
Assert.Single(sharedTableGraph.TableEdges);
Assert.Equal("Users", sharedTableGraph.TableEdges.First().TableName);

// Get topological order of migrations in a linear chain
var linearChainGraph = graph.Build(new List<MigrationInfo> { migration1, migration2 });
var topologicalOrder = linearChainGraph.GetTopologicalOrder();
Assert.Equal(2, topologicalOrder.Count);
Assert.Equal("20240115093045", topologicalOrder[0]);
Assert.Equal("20240115093046", topologicalOrder[1]);

// Detect cycles in an acyclic graph
var acyclicGraph = graph.Build(new List<MigrationInfo> { migration1, migration2 });
Assert.False(acyclicGraph.HasCycles());

// Get ancestors of a migration
var ancestors = linearChainGraph.GetAncestors("20240115093046");
Assert.Single(ancestors);
Assert.Equal("20240115093045", ancestors.First());

// Get descendants of a migration
var descendants = linearChainGraph.GetDescendants("20240115093045");
Assert.Single(descendants);
Assert.Equal("20240115093046", descendants.First());

// Get rollback impact (includes target and all descendants)
var rollbackImpact = linearChainGraph.GetRollbackImpact("20240115093045");
Assert.Equal(2, rollbackImpact.Count);

// Render graph as text
var textOutput = linearChainGraph.RenderText();
Assert.NotEmpty(textOutput);

// Add an edge with unknown node (should throw)
var unknownGraph = new MigrationDependencyGraph();
Assert.Throws<ArgumentException>(() => unknownGraph.AddEdge("unknown-id", "20240115093045"));

// Get topological order with cyclic graph
var cyclicMigration1 = new MigrationInfo("m1", "Migration1", "DbContext");
var cyclicMigration2 = new MigrationInfo("m2", "Migration2", "DbContext");
cyclicMigration1.AddDependency("m2");
cyclicMigration2.AddDependency("m1");

var cyclicGraph = new MigrationDependencyGraph();
cyclicGraph.AddNode(cyclicMigration1);
cyclicGraph.AddNode(cyclicMigration2);
cyclicGraph.AddEdge("m1", "m2");
cyclicGraph.AddEdge("m2", "m1");

var cyclicOrder = cyclicGraph.GetTopologicalOrder();
Assert.Empty(cyclicOrder);
```

These tests ensure that migration dependency graphs are correctly constructed and analyzed, enabling safe migration execution and rollback operations.
