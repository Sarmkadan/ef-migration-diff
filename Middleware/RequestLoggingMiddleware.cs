#nullable enable
using EfMigrationDiff.CLI;

namespace EfMigrationDiff.Middleware;

/// <summary>
/// Middleware that logs command execution details including arguments, execution time, and results.
/// Supports both file-based and console logging with configurable verbosity levels.
/// </summary>
public class RequestLoggingMiddleware : ICommandMiddleware
{
    private readonly ILogger _logger;
    private readonly bool _isVerbose;

    public RequestLoggingMiddleware(ILogger? logger = null, bool isVerbose = false)
    {
        _logger = logger ?? new ConsoleLogger();
        _isVerbose = isVerbose;
    }

    /// <summary>
    /// Logs command invocation and arguments, then continues to next middleware.
    /// Records execution metadata in context for later use by handlers.
    /// </summary>
    public async Task<MiddlewareResult> InvokeAsync(CommandContext context)
    {
        var startTime = DateTime.UtcNow;
        var executionId = Guid.NewGuid().ToString("N").Substring(0, 8);

        context.SetMetadata("executionId", executionId);
        context.SetMetadata("startTime", startTime);

        _logger.LogInformation($"[{executionId}] Command started: {context.CommandName}");

        if (_isVerbose)
        {
            _logger.LogDebug($"  Arguments: {string.Join(", ", context.RawArguments)}");
            _logger.LogDebug($"  Parsed options: {string.Join(", ", context.ParsedOptions.Select(kv => $"{kv.Key}={kv.Value}"))}");
            _logger.LogDebug($"  Positional args: {string.Join(", ", context.ParsedArguments)}");
        }

        return MiddlewareResult.Continue();
    }
}

/// <summary>
/// Simple logger interface for abstraction.
/// </summary>
public interface ILogger
{
    void LogInformation(string message);
    void LogDebug(string message);
    void LogWarning(string message);
    void LogError(string message, Exception? exception = null);
}

/// <summary>
/// Console-based logger implementation.
/// </summary>
public class ConsoleLogger : ILogger
{
    public void LogInformation(string message) => Console.WriteLine($"[INFO] {message}");
    public void LogDebug(string message) => Console.WriteLine($"[DEBUG] {message}");
    public void LogWarning(string message) => Console.WriteLine($"[WARN] {message}");
    public void LogError(string message, Exception? exception = null)
    {
        Console.ForegroundColor = ConsoleColor.Red;
        Console.WriteLine($"[ERROR] {message}");
        if (exception is not null)
            Console.WriteLine($"  Exception: {exception.Message}");
        Console.ResetColor();
    }
}

/// <summary>
/// File-based logger that writes to a log file with timestamp.
/// </summary>
public class FileLogger : ILogger
{
    private readonly string _logPath;

    public FileLogger(string logPath = "./logs/app.log")
    {
        _logPath = logPath;
        EnsureLogDirectory();
    }

    public void LogInformation(string message) => WriteLog("INFO", message);
    public void LogDebug(string message) => WriteLog("DEBUG", message);
    public void LogWarning(string message) => WriteLog("WARN", message);
    public void LogError(string message, Exception? exception = null)
    {
        WriteLog("ERROR", message);
        if (exception is not null)
            WriteLog("ERROR", $"  Exception: {exception.Message}\n{exception.StackTrace}");
    }

    /// <summary>
    /// Writes a log entry with timestamp to the log file.
    /// </summary>
    private void WriteLog(string level, string message)
    {
        var logEntry = $"{DateTime.UtcNow:yyyy-MM-dd HH:mm:ss.fff} [{level}] {message}";
        try
        {
            File.AppendAllText(_logPath, logEntry + Environment.NewLine);
        }
        catch
        {
            // Fallback to console if file write fails
            Console.WriteLine(logEntry);
        }
    }

    /// <summary>
    /// Ensures the log directory exists.
    /// </summary>
    private void EnsureLogDirectory()
    {
        var directory = Path.GetDirectoryName(_logPath);
        if (!string.IsNullOrEmpty(directory) && !Directory.Exists(directory))
        {
            Directory.CreateDirectory(directory);
        }
    }
}
