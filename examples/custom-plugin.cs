#nullable enable
// =============================================================================
// Author: Vladyslav Zaiets | https://sarmkadan.com
// CTO & Software Architect
// =============================================================================

using System;
using System.IO;
using System.Threading.Tasks;
using EfMigrationDiff.Plugins;

/// <summary>
/// Custom plugin example: extends ef-migration-diff with audit logging and metrics.
/// Drop the compiled DLL into your plugins directory and the tool loads it automatically.
/// </summary>

// ---------------------------------------------------------------------------
// 1. A minimal plugin — override only what you need via PluginBase
// ---------------------------------------------------------------------------

/// <summary>
/// Writes a log entry whenever a migration comparison completes.
/// </summary>
public class AuditLogPlugin : PluginBase
{
    private StreamWriter? _logWriter;

    public override string Name    => "AuditLog";
    public override string Version => "1.0.0";
    public override string Author  => "Your Name";

    /// <summary>Opens the log file for appending.</summary>
    public override async Task InitializeAsync()
    {
        _logWriter = new StreamWriter("migration-audit.log", append: true);
        await _logWriter.WriteLineAsync(
            $"[{DateTime.UtcNow:O}] AuditLogPlugin v{Version} initialized");
    }

    // Hook — invoked by PluginSystem.ExecuteHookAsync("OnComparisonCompleted", ...)
    public async Task OnComparisonCompleted(
        string sourceBranch,
        string targetBranch,
        int conflictCount)
    {
        if (_logWriter is null) return;

        var status = conflictCount > 0 ? "⚠ CONFLICTS" : "✓ CLEAN";
        await _logWriter.WriteLineAsync(
            $"[{DateTime.UtcNow:O}] {status} | {sourceBranch} → {targetBranch} | conflicts={conflictCount}");
    }

    // Hook — invoked by PluginSystem.ExecuteHookAsync("OnConflictDetected", ...)
    public async Task OnConflictDetected(string conflictId, string severity)
    {
        if (_logWriter is null) return;
        await _logWriter.WriteLineAsync(
            $"[{DateTime.UtcNow:O}] CONFLICT [{severity}] id={conflictId}");
    }

    /// <summary>Flushes and disposes the log writer.</summary>
    public override async Task ShutdownAsync()
    {
        if (_logWriter is not null)
        {
            await _logWriter.WriteLineAsync(
                $"[{DateTime.UtcNow:O}] AuditLogPlugin shutting down");
            await _logWriter.FlushAsync();
            await _logWriter.DisposeAsync();
            _logWriter = null;
        }
    }
}

// ---------------------------------------------------------------------------
// 2. A plugin that implements IPlugin directly (no PluginBase)
// ---------------------------------------------------------------------------

/// <summary>
/// Prints hook events to the console — useful as a debugging/template plugin.
/// </summary>
public class DiagnosticsPlugin : IPlugin
{
    public string Name    => "Diagnostics";
    public string Version => "1.0.0";
    public string Author  => "Your Name";

    public Task InitializeAsync()
    {
        Console.WriteLine($"[{Name}] initialized");
        return Task.CompletedTask;
    }

    public Task ShutdownAsync()
    {
        Console.WriteLine($"[{Name}] shutting down");
        return Task.CompletedTask;
    }

    public Task OnReportGenerated(string reportPath, string format)
    {
        Console.WriteLine($"[{Name}] Report written: {reportPath} ({format})");
        return Task.CompletedTask;
    }
}

// ---------------------------------------------------------------------------
// 3. Usage: load plugins from a directory
// ---------------------------------------------------------------------------

class CustomPluginExample
{
    static async Task Main()
    {
        // Point PluginSystem at one or more directories containing *Plugin.dll files.
        var pluginSystem = new PluginSystem("./plugins");
        await pluginSystem.LoadPluginsAsync();

        Console.WriteLine($"Loaded {pluginSystem.GetStats().TotalPlugins} plugin(s):");
        foreach (var p in pluginSystem.GetAllPlugins())
            Console.WriteLine($"  • {p.Name} v{p.Version} by {p.Author}");

        // Simulate triggering hooks (normally called by the tool's internals)
        await pluginSystem.ExecuteHookAsync("OnComparisonCompleted", "main", "feature/users", 2);
        await pluginSystem.ExecuteHookAsync("OnConflictDetected", "conflict-001", "Critical");
        await pluginSystem.ExecuteHookAsync("OnReportGenerated", "./output/diff.json", "json");

        await pluginSystem.UnloadAllAsync();
        Console.WriteLine("All plugins unloaded.");
    }
}
