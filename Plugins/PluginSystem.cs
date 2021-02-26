#nullable enable
// =============================================================================
// Author: Vladyslav Zaiets | https://sarmkadan.com
// CTO & Software Architect
// =============================================================================

using System.Reflection;

namespace EfMigrationDiff.Plugins;

/// <summary>
/// Plugin system for extending application functionality.
/// Loads plugins from assemblies and manages lifecycle.
/// </summary>
public class PluginSystem
{
    private readonly Dictionary<string, IPlugin> _loadedPlugins = new();
    private readonly List<string> _pluginDirectories = new();

    public PluginSystem(params string[] pluginDirectories)
    {
        _pluginDirectories.AddRange(pluginDirectories);
    }

    /// <summary>
    /// Loads all plugins from registered directories.
    /// </summary>
    public async Task LoadPluginsAsync()
    {
        foreach (var directory in _pluginDirectories)
        {
            if (!Directory.Exists(directory))
                continue;

            var dllFiles = Directory.GetFiles(directory, "*Plugin.dll");
            foreach (var dllFile in dllFiles)
            {
                try
                {
                    await LoadPluginFromAssemblyAsync(dllFile).ConfigureAwait(false);
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Failed to load plugin from {dllFile}: {ex.Message}");
                }
            }
        }
    }

    /// <summary>
    /// Loads a plugin from a specific assembly file.
    /// </summary>
    private async Task LoadPluginFromAssemblyAsync(string assemblyPath)
    {
        try
        {
            var assembly = Assembly.LoadFrom(assemblyPath);
            var pluginTypes = assembly.GetTypes()
                .Where(t => typeof(IPlugin).IsAssignableFrom(t) && !t.IsInterface && !t.IsAbstract);

            foreach (var pluginType in pluginTypes)
            {
                var pluginInstance = Activator.CreateInstance(pluginType) as IPlugin;
                if (pluginInstance is not null)
                {
                    await pluginInstance.InitializeAsync().ConfigureAwait(false);
                    _loadedPlugins[pluginInstance.Name] = pluginInstance;
                }
            }
        }
        catch (Exception ex)
        {
            throw new PluginLoadException($"Failed to load plugin from {assemblyPath}", ex);
        }
    }

    /// <summary>
    /// Gets a loaded plugin by its registered name.
    /// </summary>
    /// <param name="name">The <see cref="IPlugin.Name"/> value of the plugin to retrieve.</param>
    /// <returns>The plugin instance if found; otherwise <c>null</c>.</returns>
    public IPlugin? GetPlugin(string name)
    {
        return _loadedPlugins.TryGetValue(name, out var plugin) ? plugin : null;
    }

    /// <summary>
    /// Gets all loaded plugins.
    /// </summary>
    public IEnumerable<IPlugin> GetAllPlugins()
    {
        return _loadedPlugins.Values;
    }

    /// <summary>
    /// Executes a named hook method on all loaded plugins that implement it.
    /// Uses reflection to locate the method by name. If the hook method returns a <see cref="Task"/>,
    /// it is awaited. Errors in individual plugin hooks are logged but do not stop execution
    /// of subsequent plugins.
    /// </summary>
    /// <param name="hookName">The exact method name to invoke on each plugin (case-sensitive).</param>
    /// <param name="args">Arguments to pass to the hook method.</param>
    public async Task ExecuteHookAsync(string hookName, params object[] args)
    {
        foreach (var plugin in _loadedPlugins.Values)
        {
            var method = plugin.GetType().GetMethod(hookName);
            if (method is not null)
            {
                try
                {
                    var result = method.Invoke(plugin, args);
                    if (result is Task task)
                        await task;
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Plugin hook error in {plugin.Name}.{hookName}: {ex.Message}");
                }
            }
        }
    }

    /// <summary>
    /// Unloads and disposes all plugins.
    /// </summary>
    public async Task UnloadAllAsync()
    {
        foreach (var plugin in _loadedPlugins.Values)
        {
            try
            {
                await plugin.ShutdownAsync().ConfigureAwait(false);
                (plugin as IDisposable)?.Dispose();
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error unloading plugin {plugin.Name}: {ex.Message}");
            }
        }
        _loadedPlugins.Clear();
    }

    /// <summary>
    /// Gets plugin statistics.
    /// </summary>
    public PluginSystemStats GetStats()
    {
        return new PluginSystemStats
        {
            TotalPlugins = _loadedPlugins.Count,
            PluginNames = _loadedPlugins.Keys.ToList()
        };
    }
}

/// <summary>
/// Interface for plugin implementations.
/// </summary>
public interface IPlugin
{
    string Name { get; }
    string Version { get; }
    string Author { get; }

    Task InitializeAsync();
    Task ShutdownAsync();
}

/// <summary>
/// Base class for plugin implementations.
/// </summary>
public abstract class PluginBase : IPlugin
{
    public abstract string Name { get; }
    public abstract string Version { get; }
    public abstract string Author { get; }

    public virtual Task InitializeAsync()
    {
        return Task.CompletedTask;
    }

    public virtual Task ShutdownAsync()
    {
        return Task.CompletedTask;
    }
}

/// <summary>
/// Statistics about loaded plugins.
/// </summary>
public class PluginSystemStats
{
    public int TotalPlugins { get; set; }
    public List<string> PluginNames { get; set; } = new();
}

/// <summary>
/// Exception for plugin system errors.
/// </summary>
public class PluginLoadException : Exception
{
    public PluginLoadException(string message) : base(message) { }
    public PluginLoadException(string message, Exception innerException) : base(message, innerException) { }
}
