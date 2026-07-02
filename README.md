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

## Examples

For more practical usage, including programmatic access and DI integration, check the [examples](examples) directory:

- [BasicUsage.cs](examples/BasicUsage.cs): Simple setup and migration comparison.
- [AdvancedUsage.cs](examples/AdvancedUsage.cs): Custom configuration and error handling.
- [IntegrationExample.cs](examples/IntegrationExample.cs): Dependency injection setup for ASP.NET applications.
- [basic-comparison.cs](examples/basic-comparison.cs): Git branch comparison.
- [conflict-detection.cs](examples/conflict-detection.cs): Detailed conflict analysis.

## Docker

To run the tool using Docker, you can use the provided `docker-compose.yml` file:

```bash
# Build and run the tool
docker-compose run --rm ef-migration-diff compare main feature/add-users
```

The Docker image includes `git` and the necessary dependencies to run the tool against your local repository.

## Performance Benchmarks

The project includes comprehensive performance benchmarks using [BenchmarkDotNet](https://benchmarkdotnet.org/) to measure throughput and memory allocation for core operations.

### Running Benchmarks

To run all benchmarks:

```bash
cd ef-migration-diff.Benchmarks
# Run all benchmarks and save results to BenchmarkDotNet.Artifacts
dotnet run -c Release

# Run with specific filter
dotnet run -c Release -- --filter *"
```

To run benchmarks and export results to a file:

```bash
cd ef-migration-diff.Benchmarks
# Export to markdown
dotnet run -c Release -- --exporters markdown --filter *"

# Export to csv
dotnet run -c Release -- --exporters csv --filter *"
```

To compare performance between different configurations:

```bash
cd ef-migration-diff.Benchmarks
# Run config comparison benchmarks
dotnet run -c Release -- --filter ConfigComparisonBenchmarks"
```

To run memory allocation benchmarks:

```bash
cd ef-migration-diff.Benchmarks
dotnet run -c Release -- --filter MemoryBenchmarks"
```

### Benchmark Results

The benchmarks measure:

- **Throughput**: Operations per second for critical methods
- **Memory Allocation**: GC pressure and memory usage
- **Configuration Impact**: Performance characteristics with different options


Key benchmarks include:

- Two-way diff computation (`ComputeDiff`)
- Three-way diff computation (`ComputeThreeWayDiff`)
- Merge resolution strategies (`AcceptSource`, `AcceptTarget`, `AutoMerge`)
- Validation operations (`ValidateResolution`)

### Sample Benchmark Scenarios

1. **Small datasets** (10-50 changes): Typical for individual migrations
2. **Medium datasets** (100-500 changes): Typical for feature branches  
3. **Large datasets** (1000+ changes): Enterprise applications with many migrations

4. **Configuration variations**:
   - Default options
   - With SQL content included
   - With whitespace normalization
   - With metadata display

### Performance Characteristics

The library is optimized for:
- **Low memory allocation**: Minimal GC pressure during diff computation
- **Fast execution**: O(n log n) complexity for most operations
- **Scalability**: Handles large migration sets efficiently

### Benchmark Summary

| Benchmark Category | Operations | Dataset Size | Throughput (ops/sec) | Memory Allocation |
|------------------|------------|--------------|----------------------|------------------|
| Two-Way Diff | `ComputeDiff` | Small (10 changes) | ~10,000-50,000 | ~50-200 KB |
| Two-Way Diff | `ComputeDiff` | Medium (100 changes) | ~2,000-10,000 | ~500-2,000 KB |
| Two-Way Diff | `ComputeDiff` | Large (1000 changes) | ~200-1,000 | ~5-20 MB |
| Three-Way Diff | `ComputeThreeWayDiff` | Small (50 base + 50 each) | ~5,000-25,000 | ~100-400 KB |
| Three-Way Diff | `ComputeThreeWayDiff` | Medium (100 base + 100 each) | ~1,000-5,000 | ~1-4 MB |
| Merge Resolution | `AcceptSource` | Small conflicts | ~10,000-50,000 | ~10-50 KB |
| Merge Resolution | `AutoMerge` | Small conflicts | ~8,000-40,000 | ~10-50 KB |

> **Note**: Actual performance varies based on hardware, .NET runtime version, and specific schema change patterns. These benchmarks were run on a modern developer workstation with .NET 10.0.

### Configuration Impact

| Configuration | Performance Impact | Use Case |
|---------------|------------------|----------|
| Default | Baseline performance | Standard usage |
| With SQL content | ~10-20% slower | Detailed debugging |
| With whitespace ignored | ~5-15% faster | Production comparisons |
| With metadata | ~15-30% slower | Advanced analysis |

### Memory Efficiency

The library maintains low memory allocation through:
- Efficient data structures (List<T>, Dictionary<K,V>)
- Minimal intermediate object creation
- Reuse of buffers where possible
- Streaming-friendly algorithms for large datasets

## License

This project is licensed under the MIT License - see the [LICENSE](LICENSE) file for details.
Copyright (c) 2026 Vladyslav Zaiets.