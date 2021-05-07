using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Columns;
using BenchmarkDotNet.Configs;
using BenchmarkDotNet.Diagnosers;
using BenchmarkDotNet.Exporters;
using BenchmarkDotNet.Running;
using EfMigrationDiff.Configuration;
using EfMigrationDiff.Models;
using EfMigrationDiff.Services;
using System;
using System.Collections.Generic;
using System.Linq;

namespace EfMigrationDiff.Benchmarks;

/// <summary>
/// Performance benchmarks for the ef-migration-diff library core algorithms.
/// Measures throughput and memory allocation for critical operations.
/// </summary>
[MemoryDiagnoser]
[Orderer(BenchmarkDotNet.Order.SummaryOrderPolicy.FastestToSlowest)]
[RankColumn]
[JsonExporterAttribute.Full]
[MarkdownExporterAttribute.Default]
[CsvExporterAttribute]
[HtmlExporterAttribute]
public class SchemaDiffBenchmarks
{
    private SchemaDiffEngine _engine = null!;
    private List<SchemaChange> _smallSourceChanges = new();
    private List<SchemaChange> _smallTargetChanges = new();
    private List<SchemaChange> _mediumSourceChanges = new();
    private List<SchemaChange> _mediumTargetChanges = new();
    private List<SchemaChange> _largeSourceChanges = new();
    private List<SchemaChange> _largeTargetChanges = new();
    private List<SchemaChange> _baseChanges = new();
    private List<SchemaChange> _conflictSourceChanges = new();
    private List<SchemaChange> _conflictTargetChanges = new();

    [GlobalSetup]
    public void Setup()
    {
        var conflictDetection = new ConflictDetectionService();
        _engine = new SchemaDiffEngine(conflictDetection);

        // Generate test data - simulating realistic schema changes
        _baseChanges = GenerateSchemaChanges(50);
        _smallSourceChanges = GenerateSchemaChanges(10);
        _smallTargetChanges = GenerateSchemaChanges(10);

        _mediumSourceChanges = GenerateSchemaChanges(100);
        _mediumTargetChanges = GenerateSchemaChanges(100);

        _largeSourceChanges = GenerateSchemaChanges(1000);
        _largeTargetChanges = GenerateSchemaChanges(1000);

        // Generate conflicting changes for three-way diff benchmarks
        _conflictSourceChanges = GenerateConflictingChanges(50, "feature-branch-a");
        _conflictTargetChanges = GenerateConflictingChanges(50, "feature-branch-b");
    }

    #region Two-Way Diff Benchmarks

    [Benchmark]
    public SchemaDiffResult ComputeDiff_Small()
    {
        return _engine.ComputeDiff(_smallSourceChanges, _smallTargetChanges);
    }

    [Benchmark]
    public SchemaDiffResult ComputeDiff_Medium()
    {
        return _engine.ComputeDiff(_mediumSourceChanges, _mediumTargetChanges);
    }

    [Benchmark]
    public SchemaDiffResult ComputeDiff_Large()
    {
        return _engine.ComputeDiff(_largeSourceChanges, _largeTargetChanges);
    }

    [Benchmark]
    public SchemaDiffResult ComputeDiff_WithSqlContent()
    {
        var options = SchemaDiffOptions.Default with { IncludeSqlContent = true };
        return _engine.ComputeDiff(_mediumSourceChanges, _mediumTargetChanges, options);
    }

    [Benchmark]
    public SchemaDiffResult ComputeDiff_WithWhitespaceIgnored()
    {
        var options = SchemaDiffOptions.Default with { IgnoreWhitespace = true };
        return _engine.ComputeDiff(_mediumSourceChanges, _mediumTargetChanges, options);
    }

    [Benchmark]
    public SchemaDiffResult ComputeDiff_WithMetadata()
    {
        var options = SchemaDiffOptions.Default with { IncludeMetadata = true };
        return _engine.ComputeDiff(_mediumSourceChanges, _mediumTargetChanges, options);
    }

    #endregion

    #region Three-Way Diff Benchmarks

    [Benchmark]
    public ThreeWayDiffResult ComputeThreeWayDiff_Small()
    {
        return _engine.ComputeThreeWayDiff(_baseChanges, _smallSourceChanges, _smallTargetChanges);
    }

    [Benchmark]
    public ThreeWayDiffResult ComputeThreeWayDiff_Medium()
    {
        return _engine.ComputeThreeWayDiff(_baseChanges, _mediumSourceChanges, _mediumTargetChanges);
    }

    [Benchmark]
    public ThreeWayDiffResult ComputeThreeWayDiff_Large()
    {
        return _engine.ComputeThreeWayDiff(_baseChanges, _largeSourceChanges, _largeTargetChanges);
    }

    [Benchmark]
    public ThreeWayDiffResult ComputeThreeWayDiff_WithConflicts()
    {
        return _engine.ComputeThreeWayDiff(_baseChanges, _conflictSourceChanges, _conflictTargetChanges);
    }

    #endregion

    #region Merge Resolution Benchmarks

    [Benchmark]
    public MergeResolutionPlan AcceptSourceStrategy()
    {
        var diff = _engine.ComputeThreeWayDiff(_baseChanges, _conflictSourceChanges, _conflictTargetChanges);
        return _engine.AcceptSource(diff);
    }

    [Benchmark]
    public MergeResolutionPlan AcceptTargetStrategy()
    {
        var diff = _engine.ComputeThreeWayDiff(_baseChanges, _conflictSourceChanges, _conflictTargetChanges);
        return _engine.AcceptTarget(diff);
    }

    [Benchmark]
    public MergeResolutionPlan AutoMergeStrategy()
    {
        var diff = _engine.ComputeThreeWayDiff(_baseChanges, _conflictSourceChanges, _conflictTargetChanges);
        return _engine.AutoMerge(diff);
    }

    [Benchmark]
    public SchemaMergeResult ApplyMergeResolution()
    {
        var diff = _engine.ComputeThreeWayDiff(_baseChanges, _conflictSourceChanges, _conflictTargetChanges);
        var plan = _engine.AcceptSource(diff);
        return _engine.ApplyMergeResolution(diff, plan);
    }

    [Benchmark]
    public IReadOnlyList<string> ValidateResolution()
    {
        var diff = _engine.ComputeThreeWayDiff(_baseChanges, _conflictSourceChanges, _conflictTargetChanges);
        var plan = _engine.AcceptSource(diff);
        return _engine.ValidateResolution(plan, diff);
    }

    #endregion

    #region Helper Methods

    private static List<SchemaChange> GenerateSchemaChanges(int count)
    {
        var changes = new List<SchemaChange>(count);
        var random = new Random(42);

        var changeTypes = new[] {
            SqlChangeType.CreateTable,
            SqlChangeType.AddColumn,
            SqlChangeType.ModifyColumn,
            SqlChangeType.CreateIndex,
            SqlChangeType.DropColumn
        };

        var tableNames = new[] { "Users", "Products", "Orders", "Customers", "Inventory", "Categories" };
        var columnNames = new[] { "Id", "Name", "Email", "Price", "Quantity", "Description", "Status" };

        for (int i = 0; i < count; i++)
        {
            var changeType = changeTypes[random.Next(changeTypes.Length)];
            var tableName = tableNames[random.Next(tableNames.Length)];
            var columnName = columnNames[random.Next(columnNames.Length)];

            var sql = changeType switch
            {
                SqlChangeType.CreateTable => $"CREATE TABLE [{tableName}] (Id INT PRIMARY KEY, {columnName} NVARCHAR(100))",
                SqlChangeType.AddColumn => $"ALTER TABLE [{tableName}] ADD [{columnName}] NVARCHAR(100)",
                SqlChangeType.ModifyColumn => $"ALTER TABLE [{tableName}] ALTER COLUMN [{columnName}] NVARCHAR(200)",
                SqlChangeType.CreateIndex => $"CREATE INDEX IX_{tableName}_{columnName} ON [{tableName}]([{columnName}])",
                _ => $"ALTER TABLE [{tableName}] ADD COLUMN [{columnName}] INT"
            };

            changes.Add(new SchemaChange($"Migration_{i}", changeType, sql)
            {
                TableName = tableName,
                ColumnName = columnName,
                LineNumber = i + 1
            });
        }

        return changes;
    }

    private static List<SchemaChange> GenerateConflictingChanges(int count, string migrationIdPrefix)
    {
        var changes = new List<SchemaChange>(count);
        var random = new Random(123);

        var changeTypes = new[] {
            SqlChangeType.CreateTable,
            SqlChangeType.AddColumn,
            SqlChangeType.ModifyColumn
        };

        var tableNames = new[] { "Users", "Products", "Orders" };
        var columnNames = new[] { "Name", "Email", "Status" };

        for (int i = 0; i < count; i++)
        {
            var changeType = changeTypes[random.Next(changeTypes.Length)];
            var tableName = tableNames[random.Next(tableNames.Length)];
            var columnName = columnNames[random.Next(columnNames.Length)];

            var sql = changeType switch
            {
                SqlChangeType.CreateTable => $"CREATE TABLE [{tableName}] (Id INT PRIMARY KEY, {columnName} NVARCHAR(100))",
                SqlChangeType.AddColumn => $"ALTER TABLE [{tableName}] ADD [{columnName}] NVARCHAR(100)",
                _ => $"ALTER TABLE [{tableName}] ALTER COLUMN [{columnName}] NVARCHAR(200)"
            };

            changes.Add(new SchemaChange($"{migrationIdPrefix}_{i}", changeType, sql)
            {
                TableName = tableName,
                ColumnName = columnName,
                LineNumber = i + 1
            });
        }

        return changes;
    }

    #endregion
}

/// <summary>
/// Configuration comparison benchmarks for different SchemaDiffOptions.
/// </summary>
[Config(typeof(Config))]
public class ConfigComparisonBenchmarks
{
    private SchemaDiffEngine _engine = null!;
    private List<SchemaChange> _sourceChanges = new();
    private List<SchemaChange> _targetChanges = new();

    private class Config : ManualConfig
    {
        public Config()
        {
            AddDiagnoser(MemoryDiagnoser.Default);
        }
    }

    [GlobalSetup]
    public void Setup()
    {
        var conflictDetection = new ConflictDetectionService();
        _engine = new SchemaDiffEngine(conflictDetection);

        _sourceChanges = GenerateSchemaChanges(200);
        _targetChanges = GenerateSchemaChanges(200);
    }

    [Benchmark]
    public SchemaDiffResult DefaultOptions()
    {
        return _engine.ComputeDiff(_sourceChanges, _targetChanges, SchemaDiffOptions.Default);
    }

    [Benchmark]
    public SchemaDiffResult WithSqlContent()
    {
        var options = SchemaDiffOptions.Default with { IncludeSqlContent = true };
        return _engine.ComputeDiff(_sourceChanges, _targetChanges, options);
    }

    [Benchmark]
    public SchemaDiffResult WithWhitespaceIgnored()
    {
        var options = SchemaDiffOptions.Default with { IgnoreWhitespace = true };
        return _engine.ComputeDiff(_sourceChanges, _targetChanges, options);
    }

    [Benchmark]
    public SchemaDiffResult WithMetadata()
    {
        var options = SchemaDiffOptions.Default with { IncludeMetadata = true };
        return _engine.ComputeDiff(_sourceChanges, _targetChanges, options);
    }

    [Benchmark]
    public SchemaDiffResult AllOptions()
    {
        var options = SchemaDiffOptions.Default with {
            IncludeSqlContent = true,
            IgnoreWhitespace = true,
            IncludeMetadata = true
        };
        return _engine.ComputeDiff(_sourceChanges, _targetChanges, options);
    }

    private static List<SchemaChange> GenerateSchemaChanges(int count)
    {
        var changes = new List<SchemaChange>(count);
        var random = new Random(456);

        var changeTypes = new[] {
            SqlChangeType.CreateTable,
            SqlChangeType.AddColumn,
            SqlChangeType.ModifyColumn,
            SqlChangeType.CreateIndex
        };

        var tableNames = new[] { "Users", "Products", "Orders" };
        var columnNames = new[] { "Id", "Name", "Email" };

        for (int i = 0; i < count; i++)
        {
            var changeType = changeTypes[random.Next(changeTypes.Length)];
            var tableName = tableNames[random.Next(tableNames.Length)];
            var columnName = columnNames[random.Next(columnNames.Length)];

            var sql = changeType switch
            {
                SqlChangeType.CreateTable => $"CREATE TABLE [{tableName}] (Id INT PRIMARY KEY, {columnName} NVARCHAR(100))",
                SqlChangeType.AddColumn => $"ALTER TABLE [{tableName}] ADD [{columnName}] NVARCHAR(100)",
                SqlChangeType.ModifyColumn => $"ALTER TABLE [{tableName}] ALTER COLUMN [{columnName}] NVARCHAR(200)",
                _ => $"CREATE INDEX IX_{tableName}_{columnName} ON [{tableName}]([{columnName}])"
            };

            changes.Add(new SchemaChange($"Migration_{i}", changeType, sql)
            {
                TableName = tableName,
                ColumnName = columnName,
                LineNumber = i + 1
            });
        }

        return changes;
    }
}

/// <summary>
/// Memory allocation benchmarks for critical operations.
/// </summary>
[MemoryDiagnoser]
public class MemoryBenchmarks
{
    private SchemaDiffEngine _engine = null!;
    private List<SchemaChange> _sourceChanges = new();
    private List<SchemaChange> _targetChanges = new();
    private List<SchemaChange> _baseChanges = new();

    [GlobalSetup]
    public void Setup()
    {
        var conflictDetection = new ConflictDetectionService();
        _engine = new SchemaDiffEngine(conflictDetection);

        _sourceChanges = GenerateSchemaChanges(500);
        _targetChanges = GenerateSchemaChanges(500);
        _baseChanges = GenerateSchemaChanges(250);
    }

    [Benchmark]
    public SchemaDiffResult ComputeDiff_Memory()
    {
        return _engine.ComputeDiff(_sourceChanges, _targetChanges);
    }

    [Benchmark]
    public ThreeWayDiffResult ComputeThreeWayDiff_Memory()
    {
        return _engine.ComputeThreeWayDiff(_baseChanges, _sourceChanges, _targetChanges);
    }

    [Benchmark]
    public MergeResolutionPlan AcceptSource_Memory()
    {
        var diff = _engine.ComputeThreeWayDiff(_baseChanges, _sourceChanges, _targetChanges);
        return _engine.AcceptSource(diff);
    }

    [Benchmark]
    public MergeResolutionPlan AutoMerge_Memory()
    {
        var diff = _engine.ComputeThreeWayDiff(_baseChanges, _sourceChanges, _targetChanges);
        return _engine.AutoMerge(diff);
    }

    private static List<SchemaChange> GenerateSchemaChanges(int count)
    {
        var changes = new List<SchemaChange>(count);
        var random = new Random(789);

        var changeTypes = new[] {
            SqlChangeType.CreateTable,
            SqlChangeType.AddColumn,
            SqlChangeType.ModifyColumn
        };

        var tableNames = new[] { "Users", "Products" };
        var columnNames = new[] { "Name", "Email" };

        for (int i = 0; i < count; i++)
        {
            var changeType = changeTypes[random.Next(changeTypes.Length)];
            var tableName = tableNames[random.Next(tableNames.Length)];
            var columnName = columnNames[random.Next(columnNames.Length)];

            var sql = changeType switch
            {
                SqlChangeType.CreateTable => $"CREATE TABLE [{tableName}] (Id INT PRIMARY KEY, {columnName} NVARCHAR(100))",
                SqlChangeType.AddColumn => $"ALTER TABLE [{tableName}] ADD [{columnName}] NVARCHAR(100)",
                _ => $"ALTER TABLE [{tableName}] ALTER COLUMN [{columnName}] NVARCHAR(200)"
            };

            changes.Add(new SchemaChange($"Migration_{i}", changeType, sql)
            {
                TableName = tableName,
                ColumnName = columnName,
                LineNumber = i + 1
            });
        }

        return changes;
    }
}

public class Program
{
    public static void Main(string[] args)
    {
        BenchmarkRunner.Run<SchemaDiffBenchmarks>();
        BenchmarkRunner.Run<ConfigComparisonBenchmarks>();
        BenchmarkRunner.Run<MemoryBenchmarks>();
    }
}