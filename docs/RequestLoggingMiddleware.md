# RequestLoggingMiddleware

The `RequestLoggingMiddleware` class provides HTTP request logging for the `ef-migration-diff` tool. It captures incoming requests and logs their details (method, path, status, duration) to a file using an embedded `FileLogger` implementation. The middleware is designed to be inserted into an ASP.NET Core pipeline and offers convenience methods for logging at different severity levels, as well as direct access to the underlying file logger.

## API

### `public RequestLoggingMiddleware()`

Initializes a new instance of the middleware with default configuration. The file logger is set up to write to a default log file path.

### `public async Task<MiddlewareResult> InvokeAsync()`

Processes the current HTTP request. This method is called by the ASP.NET Core pipeline. It logs the request details, invokes the next middleware, and logs the response status and elapsed time.

- **Returns**: A `Task<MiddlewareResult>` representing the asynchronous operation. The `MiddlewareResult` indicates whether the pipeline should continue or short-circuit.
- **Throws**: `InvalidOperationException` if the middleware is not properly configured or if the file logger fails to initialize.

### `public void LogInformation(string message)`

Logs an informational message using the underlying file logger.

- **Parameters**: `message` – The message to log.
- **Throws**: `ObjectDisposedException` if the file logger has been disposed.

### `public void LogDebug(string message)`

Logs a debug message using the underlying file logger.

- **Parameters**: `message` – The message to log.
- **Throws**: `ObjectDisposedException` if the file logger has been disposed.

### `public void LogWarning(string message)`

Logs a warning message using the underlying file logger.

- **Parameters**: `message` – The message to log.
- **Throws**: `ObjectDisposedException` if the file logger has been disposed.

### `public void LogError(string message)`

Logs an error message using the underlying file logger.

- **Parameters**: `message` – The message to log.
- **Throws**: `ObjectDisposedException` if the file logger has been disposed.

### `public class FileLogger`

A nested class that provides file-based logging. It writes log entries to a specified file path, appending each entry on a new line with a timestamp and severity prefix.

#### `public void LogInformation(string message)`

Writes an informational log entry to the file.

- **Parameters**: `message` – The message to log.
- **Throws**: `IOException` if the file cannot be written to (e.g., disk full, permission denied).

#### `public void LogDebug(string message)`

Writes a debug log entry to the file.

- **Parameters**: `message` – The message to log.
- **Throws**: `IOException` if the file cannot be written to.

#### `public void LogWarning(string message)`

Writes a warning log entry to the file.

- **Parameters**: `message` – The message to log.
- **Throws**: `IOException` if the file cannot be written to.

#### `public void LogError(string message)`

Writes an error log entry to the file.

- **Parameters**: `message` – The message to log.
- **Throws**: `IOException` if the file cannot be written to.

## Usage

### Example 1: Registering the middleware in the ASP.NET Core pipeline

```csharp
public class Startup
{
    public void Configure(IApplicationBuilder app)
    {
        app.UseMiddleware<RequestLoggingMiddleware>();
        app.UseRouting();
        app.UseEndpoints(endpoints => { endpoints.MapControllers(); });
    }
}
```

### Example 2: Using the FileLogger directly for custom logging

```csharp
var middleware = new RequestLoggingMiddleware();
middleware.LogInformation("Application started.");

// Access the underlying file logger for more granular control
middleware.FileLogger.LogWarning("Disk space is low.");
```

## Notes

- The `FileLogger` writes to a single file; concurrent writes from multiple threads are serialized internally using a lock. However, the `Log*` methods on `RequestLoggingMiddleware` are not thread-safe by themselves—they delegate to the same `FileLogger` instance, so external synchronization is recommended if the middleware is used from multiple threads outside the ASP.NET Core pipeline.
- If the log file cannot be created or written to (e.g., due to insufficient permissions or a full disk), the `FileLogger` methods throw an `IOException`. The middleware’s `InvokeAsync` method catches such exceptions and logs them to the standard error stream to avoid crashing the application.
- The `InvokeAsync` method is asynchronous and should be awaited. Calling it without awaiting may lead to incomplete logging or resource leaks.
- The `FileLogger` class is not disposable; it holds the file stream open for the lifetime of the middleware. Ensure that the middleware instance is not reused across application restarts without proper cleanup.
