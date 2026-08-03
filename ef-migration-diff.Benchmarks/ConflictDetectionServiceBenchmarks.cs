using BenchmarkDotNet.Attributes;
using EfMigrationDiff.Models;
using EfMigrationDiff.Services;
using Microsoft.Extensions.Logging;

namespace EfMigrationDiff.Benchmarks
{
    [MemoryDiagnoser]
    public class ConflictDetectionServiceBenchmarks
    {
        private readonly ILogger<ConflictDetectionService> _logger;

        public ConflictDetectionServiceBenchmarks()
        {
            _logger = Microsoft.Extensions.Logging.Abstractions.NullLogger<ConflictDetectionService>.Instance;
        }

        [Params(10, 100, 1000)]
        public int Size { get; set; }

        [Benchmark]
        public void DetectConflicts_Benchmark()
        {
            var service = new ConflictDetectionService(_logger);
            var sourceChanges = new List<SchemaChange>();
            var targetChanges = new List<SchemaChange>();

            for (int i = 0; i < Size; i++)
            {
                sourceChanges.Add(new SchemaChange
                {
                    MigrationId = i.ToString(),
                    ChangeType = SqlChangeType.CreateTable,
                    TableName = "Table" + i
                });
                targetChanges.Add(new SchemaChange
                {
                    MigrationId = i.ToString(),
                    ChangeType = SqlChangeType.CreateTable,
                    TableName = "Table" + i
                });
            }

            service.DetectConflicts(sourceChanges, targetChanges);
        }

        [Benchmark]
        public void DetectConflicts_InvalidInput_Benchmark()
        {
            var service = new ConflictDetectionService(_logger);
            var sourceChanges = new List<SchemaChange>();
            var targetChanges = new List<SchemaChange>();

            for (int i = 0; i < Size; i++)
            {
                sourceChanges.Add(new SchemaChange
                {
                    MigrationId = i.ToString(),
                    ChangeType = SqlChangeType.CreateTable,
                    TableName = "Table" + i
                });
                targetChanges.Add(new SchemaChange
                {
                    MigrationId = i.ToString(),
                    ChangeType = SqlChangeType.CreateTable,
                    TableName = "Table" + i
                });
            }

            service.DetectConflicts(sourceChanges, targetChanges);
        }

        [Benchmark]
        public void DetectConflicts_MultipleCalls_Benchmark()
        {
            var service = new ConflictDetectionService(_logger);
            var sourceChanges = new List<SchemaChange>();
            var targetChanges = new List<SchemaChange>();

            for (int i = 0; i < Size; i++)
            {
                sourceChanges.Add(new SchemaChange
                {
                    MigrationId = i.ToString(),
                    ChangeType = SqlChangeType.CreateTable,
                    TableName = "Table" + i
                });
                targetChanges.Add(new SchemaChange
                {
                    MigrationId = i.ToString(),
                    ChangeType = SqlChangeType.CreateTable,
                    TableName = "Table" + i
                });
            }

            for (int i = 0; i < Size; i++)
            {
                service.DetectConflicts(sourceChanges, targetChanges);
            }
        }
    }
}