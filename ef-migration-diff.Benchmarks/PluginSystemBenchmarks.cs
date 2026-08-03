using System.Reflection;
using BenchmarkDotNet.Attributes;
using EfMigrationDiff.Plugins;

namespace EfMigrationDiff.Benchmarks;

[MemoryDiagnoser]
public class PluginSystemBenchmarks
{
    private PluginSystem _pluginSystem = null!;
    private List<DummyPlugin> _dummyPlugins = null!;

    [Params(10, 100, 1000)]
    public int PluginCount;

    [GlobalSetup]
    public void Setup()
    {
        _pluginSystem = new PluginSystem();
        _dummyPlugins = new List<DummyPlugin>();

        var loadedPluginsField = typeof(PluginSystem).GetField("_loadedPlugins", BindingFlags.NonPublic | BindingFlags.Instance);
        var loadedPlugins = (Dictionary<string, IPlugin>)loadedPluginsField!.GetValue(_pluginSystem)!;

        for (int i = 0; i < PluginCount; i++)
        {
            var plugin = new DummyPlugin($"Plugin{i}");
            _dummyPlugins.Add(plugin);
            loadedPlugins.Add(plugin.Name, plugin);
        }
    }

    [Benchmark]
    public IPlugin? GetPlugin()
    {
        return _pluginSystem.GetPlugin($"Plugin{PluginCount / 2}");
    }

    [Benchmark]
    public List<IPlugin> GetAllPlugins()
    {
        return _pluginSystem.GetAllPlugins().ToList();
    }

    [Benchmark]
    public async Task ExecuteHookAsync()
    {
        await _pluginSystem.ExecuteHookAsync("OnHook", 42);
    }

    [Benchmark]
    public PluginSystemStats GetStats()
    {
        return _pluginSystem.GetStats();
    }

    private class DummyPlugin : PluginBase
    {
        public override string Name { get; }
        public override string Version => "1.0.0";
        public override string Author => "Benchmark";
        public DummyPlugin(string name) => Name = name;
        public Task OnHook(int arg) => Task.CompletedTask;
    }
}
