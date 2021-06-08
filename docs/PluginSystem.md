# PluginSystem

Central registry and lifecycle manager for discoverable, loadable plug-ins in the EF Migration Diff tool. It discovers plug-ins via reflection, tracks their load state, and provides hooks for executing plug-in-defined operations while exposing statistics and diagnostic information.

## API

### `public PluginSystem()`

Constructs an empty plug-in registry. No plug-ins are loaded until `LoadPluginsAsync` is called.

### `public async Task LoadPluginsAsync()`

Locates plug-ins via the configured plug-in path, instantiates each one, and registers it. Plug-ins are loaded in no guaranteed order. If a plug-in fails to load, a `PluginLoadException` is thrown and the registry remains in the pre-load state (no partially-loaded plug-ins are retained).

**Exceptions**
- `PluginLoadException` – thrown if any plug-in fails to load; the message contains the name of the offending plug-in and the inner exception.

### `public IPlugin? GetPlugin(string name)`

Returns the loaded plug-in with the given `name`, or `null` if no such plug-in exists.

**Parameters**
- `name` – the unique plug-in name as declared by the plug-in.

**Return Value**
- The requested `IPlugin` instance, or `null`.

### `public IEnumerable<IPlugin> GetAllPlugins()`

Returns an immutable enumeration of all currently loaded plug-ins in no particular order.

**Return Value**
- A sequence of `IPlugin` instances.

### `public async Task ExecuteHookAsync(string hookName, object? context = null)`

Invokes the named hook on every loaded plug-in that implements `IPlugin` and exposes the hook. Execution is sequential and synchronous per plug-in; exceptions are collected and surfaced as an `AggregateException` after all plug-ins have been attempted.

**Parameters**
- `hookName` – the hook identifier to execute.
- `context` – an optional context object passed to each plug-in’s hook method.

**Exceptions**
- `AggregateException` – thrown if any plug-in throws during hook execution; the inner exceptions contain the original plug-in exceptions.

### `public async Task UnloadAllAsync()`

Unloads every currently loaded plug-in by invoking its `ShutdownAsync` method. If any plug-in throws, the remaining plug-ins are still shut down and the exceptions are collected into an `AggregateException`.

**Exceptions**
- `AggregateException` – thrown if any plug-in fails during shutdown.

### `public PluginSystemStats GetStats()`

Returns a snapshot of current registry statistics.

**Return Value**
- A `PluginSystemStats` record containing `TotalPlugins`, `LoadedPlugins`, `FailedPlugins`, and `PluginNames`.

### `public abstract string Name`

Gets the human-readable name of the plug-in system itself (e.g., “EF Migration Diff Plug-in System”).

### `public abstract string Version`

Gets the semantic version of the plug-in system.

### `public abstract string Author`

Gets the author or organization responsible for the plug-in system.

### `public virtual Task InitializeAsync()`

Called once after the plug-in system is constructed and before any plug-ins are loaded. The default implementation is a no-op; derived types may override to perform one-time setup.

### `public virtual Task ShutdownAsync()`

Called once when the plug-in system is being torn down. The default implementation is a no-op; derived types may override to perform cleanup.

### `public int TotalPlugins`

Gets the total number of plug-ins discovered during the last successful load operation. Zero if no load has occurred or if the last load failed.

### `public List<string> PluginNames`

Gets an immutable list of the names of all plug-ins discovered during the last successful load operation. Empty if no load has occurred or if the last load failed.

### `public PluginLoadException(string message) : base(message)`

Constructs a plug-in load exception with the given message.

**Parameters**
- `message` – the error description.

### `public PluginLoadException(string message, Exception innerException) : base(message, innerException)`

Constructs a plug-in load exception with the given message and inner exception.

**Parameters**
- `message` – the error description.
- `innerException` – the underlying exception that caused the load failure.

## Usage
