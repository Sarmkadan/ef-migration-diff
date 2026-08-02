using BenchmarkDotNet.Attributes;
using EfMigrationDiff.Models;
using System;
using System.Collections.Generic;

namespace EfMigrationDiff.Benchmarks
{
    [MemoryDiagnoser]
    public class SchemaChangeBenchmarks
    {
        private SchemaChange _createTableChange = null!;
        private SchemaChange _addColumnChange = null!;
        private SchemaChange _modifyColumnChange = null!;
        private SchemaChange _dropColumnChange = null!;
        private SchemaChange _createTableChange2 = null!;
        private SchemaChange _addColumnChange2 = null!;
        private List<SchemaChange> _changesList = null!;
        private Dictionary<string, object?> _metadata = null!;

        [Params(10, 100, 1000)]
        public int MetadataCount { get; set; }

        [GlobalSetup]
        public void Setup()
        {
            // Initialize test SchemaChange objects
            _createTableChange = new SchemaChange("Migration_1", SqlChangeType.CreateTable,
                "CREATE TABLE [Users] (Id INT PRIMARY KEY, Name NVARCHAR(100))")
            {
                TableName = "Users",
                LineNumber = 1
            };

            _addColumnChange = new SchemaChange("Migration_2", SqlChangeType.AddColumn,
                "ALTER TABLE [Users] ADD [Email] NVARCHAR(100)")
            {
                TableName = "Users",
                ColumnName = "Email",
                LineNumber = 2
            };

            _modifyColumnChange = new SchemaChange("Migration_3", SqlChangeType.ModifyColumn,
                "ALTER TABLE [Users] ALTER COLUMN [Email] NVARCHAR(200)")
            {
                TableName = "Users",
                ColumnName = "Email",
                LineNumber = 3
            };

            _dropColumnChange = new SchemaChange("Migration_4", SqlChangeType.DropColumn,
                "ALTER TABLE [Users] DROP COLUMN [Email]")
            {
                TableName = "Users",
                ColumnName = "Email",
                LineNumber = 4
            };

            _createTableChange2 = new SchemaChange("Migration_5", SqlChangeType.CreateTable,
                "CREATE TABLE [Products] (Id INT PRIMARY KEY, Name NVARCHAR(100))")
            {
                TableName = "Products",
                LineNumber = 5
            };

            _addColumnChange2 = new SchemaChange("Migration_6", SqlChangeType.AddColumn,
                "ALTER TABLE [Products] ADD [Price] DECIMAL(18,2)")
            {
                TableName = "Products",
                ColumnName = "Price",
                LineNumber = 6
            };

            // Create a list of changes for batch operations
            _changesList = new List<SchemaChange>();
            var random = new Random(42);
            var changeTypes = new[] {
                SqlChangeType.CreateTable,
                SqlChangeType.AddColumn,
                SqlChangeType.ModifyColumn,
                SqlChangeType.DropColumn,
                SqlChangeType.CreateIndex
            };
            var tableNames = new[] { "Users", "Products", "Orders", "Customers" };
            var columnNames = new[] { "Id", "Name", "Email", "Price", "Quantity" };

            for (int i = 0; i < 100; i++)
            {
                var changeType = changeTypes[random.Next(changeTypes.Length)];
                var tableName = tableNames[random.Next(tableNames.Length)];
                var columnName = columnNames[random.Next(columnNames.Length)];

                var sql = changeType switch
                {
                    SqlChangeType.CreateTable => $"CREATE TABLE [{tableName}] (Id INT PRIMARY KEY, {columnName} NVARCHAR(100))",
                    SqlChangeType.AddColumn => $"ALTER TABLE [{tableName}] ADD [{columnName}] NVARCHAR(100)",
                    SqlChangeType.ModifyColumn => $"ALTER TABLE [{tableName}] ALTER COLUMN [{columnName}] NVARCHAR(200)",
                    SqlChangeType.DropColumn => $"ALTER TABLE [{tableName}] DROP COLUMN [{columnName}]",
                    _ => $"CREATE INDEX IX_{tableName}_{columnName} ON [{tableName}]([{columnName}])"
                };

                _changesList.Add(new SchemaChange($"Migration_{i}", changeType, sql)
                {
                    TableName = tableName,
                    ColumnName = columnName,
                    LineNumber = i + 1
                });
            }

            // Initialize metadata dictionary for testing
            _metadata = new Dictionary<string, object?>();
            for (int i = 0; i < MetadataCount; i++)
            {
                _metadata[$"Key{i}"] = $"Value{i}";
            }
        }

        [Benchmark]
        public bool IsValid_CreateTable()
        {
            return _createTableChange.IsValid();
        }

        [Benchmark]
        public bool IsValid_AddColumn()
        {
            return _addColumnChange.IsValid();
        }

        [Benchmark]
        public string GetDescription_CreateTable()
        {
            return _createTableChange.GetDescription();
        }

        [Benchmark]
        public string GetDescription_AddColumn()
        {
            return _addColumnChange.GetDescription();
        }

        [Benchmark]
        public bool AffectsSameTable_SameTable()
        {
            return _createTableChange.AffectsSameTable(_addColumnChange);
        }

        [Benchmark]
        public bool AffectsSameTable_DifferentTable()
        {
            return _createTableChange.AffectsSameTable(_createTableChange2);
        }

        [Benchmark]
        public bool ConflictsWith_AddDropColumn()
        {
            return _addColumnChange.ConflictsWith(_dropColumnChange);
        }

        [Benchmark]
        public bool ConflictsWith_NoConflict()
        {
            return _createTableChange.ConflictsWith(_addColumnChange2);
        }

        [Benchmark]
        public void AddMetadata_Batch()
        {
            var change = new SchemaChange("Migration_Test", SqlChangeType.CreateTable, "CREATE TABLE [Test] (Id INT)");
            foreach(var kvp in _metadata)
            {
                change.AddMetadata(kvp.Key, kvp.Value);
            }
        }

        [Benchmark]
        public object? GetMetadata_First()
        {
            var change = new SchemaChange("Migration_Test", SqlChangeType.CreateTable, "CREATE TABLE [Test] (Id INT)");
            if (_metadata.Count > 0)
            {
                var firstKvp = _metadata.GetEnumerator();
                firstKvp.MoveNext();
                change.AddMetadata(firstKvp.Current.Key, firstKvp.Current.Value);
            }
            return _metadata.Count > 0 ? change.GetMetadata(_metadata.GetEnumerator().Current.Key) : null;
        }

        [Benchmark]
        public bool IsDestructive_DropColumn()
        {
            return _dropColumnChange.IsDestructive();
        }

        [Benchmark]
        public bool IsDestructive_CreateTable()
        {
            return _createTableChange.IsDestructive();
        }

        [Benchmark]
        public bool Equals_SameObject()
        {
            return _createTableChange.Equals(_createTableChange);
        }

        [Benchmark]
        public bool Equals_DifferentObjects_SameValues()
        {
            var change1 = new SchemaChange("Migration_1", SqlChangeType.CreateTable,
                "CREATE TABLE [Users] (Id INT PRIMARY KEY, Name NVARCHAR(100))")
            {
                TableName = "Users",
                LineNumber = 1
            };

            var change2 = new SchemaChange("Migration_1", SqlChangeType.CreateTable,
                "CREATE TABLE [Users] (Id INT PRIMARY KEY, Name NVARCHAR(100))")
            {
                TableName = "Users",
                LineNumber = 1
            };

            return change1.Equals(change2);
        }

        [Benchmark]
        public int GetHashCode_Consistent()
        {
            return _createTableChange.GetHashCode();
        }
    }
}