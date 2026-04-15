# Plugin Development Guide

This guide covers everything you need to build, register, and test custom plugins for ef-migration-diff.

## Overview

The plugin system lets you extend ef-migration-diff without modifying its core. Plugins can:

- React to migration events via hooks
- Add custom analysis or validation logic
- Post results to external systems (dashboards, Slack, etc.)
- Transform or filter report output

## Plugin Interface

All plugins must implement `IPlugin`:

```csharp
public interface IPlugin
{
    /// <summary>Plugin identifier — must be unique across all loaded plugins.</summary>
    string Name { get; }

    /// <summary>Semantic version string, e.g. "1.0.0".</summary>
    string Version { get; }

    /// <summary>Plugin author name or contact.</summary>
    string Author { get; }

    /// <summary>Called once after the plugin is loaded. Use for one-time setup.</summary>
    Task InitializeAsync();

    /// <summary>Called when the application shuts down. Use for cleanup.</summary>
    Task ShutdownAsync();
}
```

## Implementing a Plugin

### Option 1 — Extend `PluginBase` (recommended)

`PluginBase` provides default no-op implementations of `InitializeAsync` and `ShutdownAsync`.

```csharp
using EfMigrationDiff.Plugins;

public class AuditLogPlugin : PluginBase
{
    private StreamWriter? _logWriter;

    public override string Name    => "AuditLog";
    public override string Version => "1.0.0";
    public override string Author  => "Your Name";

    public override async Task InitializeAsync()
    {
        _logWriter = new StreamWriter("audit.log", append: true);
        await _logWriter.WriteLineAsync($"[{DateTime.UtcNow:O}] AuditLogPlugin initialized");
    }

    /// <summary>Hook called after every migration comparison.</summary>
    public async Task OnComparisonCompleted(string sourceBranch, string targetBranch, int conflictCount)
    {
        if (_logWriter is null) return;
        await _logWriter.WriteLineAsync(
            $"[{DateTime.UtcNow:O}] Compared {sourceBranch}..{targetBranch} — conflicts: {conflictCount}");
    }

    public override async Task ShutdownAsync()
    {
        if (_logWriter is not null)
        {
            await _logWriter.FlushAsync();
            await _logWriter.DisposeAsync();
        }
    }
}
```

### Option 2 — Implement `IPlugin` directly

Implement the interface directly when you cannot inherit from `PluginBase`.

```csharp
using EfMigrationDiff.Plugins;

public class MetricsPlugin : IPlugin
{
    public string Name    => "Metrics";
    public string Version => "2.1.0";
    public string Author  => "Your Name";

    public Task InitializeAsync() => Task.CompletedTask;
    public Task ShutdownAsync()   => Task.CompletedTask;

    public Task OnConflictDetected(string conflictId, string severity)
    {
        Console.WriteLine($"[Metrics] Conflict detected: {conflictId} ({severity})");
        return Task.CompletedTask;
    }
}
```

## Plugin Lifecycle

```
Assembly loaded
     │
     ▼
InitializeAsync()   ← one-time setup (open connections, read config)
     │
     ▼
Hook methods        ← called via ExecuteHookAsync during analysis
     │
     ▼
ShutdownAsync()     ← cleanup (close connections, flush buffers)
```

## Registering Hooks

Hooks are identified by **method name**. When `PluginSystem.ExecuteHookAsync("OnComparisonCompleted", ...)` is called, every loaded plugin that has a method named `OnComparisonCompleted` will be invoked with the supplied arguments.

```csharp
// Executing a hook from application code
await pluginSystem.ExecuteHookAsync(
    "OnComparisonCompleted",
    sourceBranch,
    targetBranch,
    diff.Conflicts.Count);
```

### Built-in Hooks

| Hook name               | When triggered                         | Arguments                                            |
|-------------------------|----------------------------------------|------------------------------------------------------|
| `OnComparisonCompleted` | After `CompareBranches` finishes       | `string sourceBranch, string targetBranch, int conflicts` |
| `OnConflictDetected`    | For each conflict found                | `string conflictId, string severity`                 |
| `OnReportGenerated`     | After a report file is written         | `string reportPath, string format`                   |
| `OnValidationFailed`    | When `validate` command returns errors | `string[] errors`                                    |

You can also define your own hook names — the system will simply skip plugins that don't implement that method.

## Packaging a Plugin

1. Create a class library project (not an executable):
   ```bash
   dotnet new classlib -n MyPlugin
   ```

2. Reference the ef-migration-diff package or copy the plugin contracts.

3. Build and place the output DLL in a plugins directory that follows the `*Plugin.dll` naming convention:
   ```
   plugins/
     AuditLogPlugin.dll
     MetricsPlugin.dll
   ```

4. Register the directory when constructing `PluginSystem`:
   ```csharp
   var pluginSystem = new PluginSystem("./plugins");
   await pluginSystem.LoadPluginsAsync();
   ```

## Testing a Plugin

```csharp
[Fact]
public async Task AuditLogPlugin_WritesEntryOnInitialization()
{
    var plugin = new AuditLogPlugin();
    await plugin.InitializeAsync();

    // Verify the log file was created
    Assert.True(File.Exists("audit.log"));

    await plugin.ShutdownAsync();
}

[Fact]
public async Task PluginSystem_ExecutesHook_OnAllLoadedPlugins()
{
    var system = new PluginSystem(); // no directories — we'll add manually
    // Simulate already-loaded plugin via reflection or a test double
    // ...
    await system.ExecuteHookAsync("OnComparisonCompleted", "main", "feature/x", 0);
    // Assert side effects in plugin under test
}
```

## Error Handling

Hook errors are caught and logged per-plugin — a failing plugin does **not** stop other plugins from running. If a plugin fails to load (e.g., missing dependencies), a `PluginLoadException` is thrown and the problematic DLL is skipped.

## See Also

- [`examples/custom-plugin.cs`](../examples/custom-plugin.cs) — complete working example
- [`Plugins/PluginSystem.cs`](../Plugins/PluginSystem.cs) — implementation source
- [API Reference](./api-reference.md)
