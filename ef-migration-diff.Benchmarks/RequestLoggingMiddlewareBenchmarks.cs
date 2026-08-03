using BenchmarkDotNet.Attributes;
using EfMigrationDiff.CLI;
using EfMigrationDiff.Middleware;
using System.Linq;
using System.Threading.Tasks;

namespace EfMigrationDiff.Benchmarks;

[MemoryDiagnoser]
public class RequestLoggingMiddlewareBenchmarks
{
    private CommandContext? _context;
    private RequestLoggingMiddleware? _middleware;
    private RequestLoggingMiddleware? _verboseMiddleware;

    [Params(10, 1000)]
    public int DataSize;

    [GlobalSetup]
    public void Setup()
    {
        var serviceProvider = new DummyServiceProvider();
        _context = new CommandContext("test-command", new string[] { "arg1", "arg2" }, serviceProvider);
        
        // Populate DataSize
        _context.RawArguments = Enumerable.Range(0, DataSize).Select(i => $"arg{i}").ToArray();
        for(int i = 0; i < DataSize; i++)
        {
            _context.ParsedOptions[$"option{i}"] = $"value{i}";
            _context.ParsedArguments.Add($"arg{i}");
        }

        _middleware = new RequestLoggingMiddleware(new NullLogger(), isVerbose: false);
        _verboseMiddleware = new RequestLoggingMiddleware(new NullLogger(), isVerbose: true);
    }

    [Benchmark]
    public async Task<MiddlewareResult> InvokeAsync_NonVerbose()
    {
        return await _middleware!.InvokeAsync(_context!);
    }

    [Benchmark]
    public async Task<MiddlewareResult> InvokeAsync_Verbose()
    {
        return await _verboseMiddleware!.InvokeAsync(_context!);
    }
}

public class NullLogger : ILogger
{
    public void LogInformation(string message) { }
    public void LogDebug(string message) { }
    public void LogWarning(string message) { }
    public void LogError(string message, Exception? exception = null) { }
}

public class DummyServiceProvider : IServiceProvider
{
    public object? GetService(System.Type serviceType) => null;
}
