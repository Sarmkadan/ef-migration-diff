# PluginSystemValidation

`PluginSystemValidation` is a static extension-method class that validates the state of a
[`PluginSystem`](PluginSystem.md) instance and reports any problems as a list of
human-readable messages. It is the safety net for the plugin registry: it checks that the
registry's statistics are coherent and that every loaded plugin exposes valid metadata
(name, version, author).

It exposes three entry points:

- **`Validate`** – runs every check and returns an immutable list of problem strings
  (empty when the instance is valid).
- **`IsValid`** – convenience wrapper returning `true` when `Validate` yields no problems.
- **`EnsureValid`** – convenience wrapper that throws an `ArgumentException` describing all
  problems when validation fails.

## API

| Member | Signature | Description |
|--------|-----------|-------------|
| **Validate** | `public static IReadOnlyList<string> Validate(this PluginSystem value)` | Performs a comprehensive validation of the plugin system instance and returns an immutable list of human-readable problems. Empty when the instance is valid. |
| **IsValid** | `public static bool IsValid(this PluginSystem value)` | Returns `true` if `Validate` yields no problems; otherwise `false`. |
| **EnsureValid** | `public static void EnsureValid(this PluginSystem value)` | Calls `Validate` and throws an `ArgumentException` listing every problem when validation fails. |

**Exceptions** – All three methods throw `ArgumentNullException` if `value` is `null`.
`EnsureValid` additionally throws `ArgumentException` when the instance is invalid.

## Validation rules

`Validate` inspects the plugin system in two passes: the registry statistics and each
individual loaded plugin.

### Registry statistics

The checks below operate on the snapshot returned by `PluginSystem.GetStats()`:

| Rule | Problem reported |
|------|------------------|
| `TotalPlugins` must not be negative. | `TotalPlugins cannot be negative.` |
| `PluginNames` collection must not be `null`. | `PluginNames collection cannot be null.` |
| `PluginNames.Count` must equal `TotalPlugins`. | `PluginNames count does not match TotalPlugins.` |
| Each plugin name must not be null, empty, or whitespace. | `PluginNames[i] cannot be null, empty, or whitespace.` |
| Each plugin name must not exceed 255 characters. | `PluginNames[i] exceeds maximum length of 255 characters.` |
| Each plugin name must not contain control or surrogate characters. | `PluginNames[i] contains invalid control or surrogate characters.` |

### Individual plugins

`Validate` iterates over every plugin returned by `GetAllPlugins()` and checks its
metadata:

| Rule | Problem reported |
|------|------------------|
| A plugin instance must not be `null`. | `GetAllPlugins() returned a null plugin instance.` |
| `Name` must not be null, empty, or whitespace. | `Plugin '<TypeName>' has null or empty Name.` |
| `Name` must not exceed 255 characters. | `Plugin '<Name>' Name exceeds maximum length of 255 characters.` |
| `Version` must not be null, empty, or whitespace. | `Plugin '<Name>' has null or empty Version.` |
| `Version` must be a valid semantic version (`MAJOR.MINOR.PATCH`). | `Plugin '<Name>' has invalid semantic version format: '<Version>'. Expected format: MAJOR.MINOR.PATCH.` |
| `Author` must not be null, empty, or whitespace. | `Plugin '<Name>' has null or empty Author.` |
| `Author` must not exceed 255 characters. | `Plugin '<Name>' Author exceeds maximum length of 255 characters.` |

### Semantic version validation

The private `IsValidSemanticVersion` helper enforces the version format used by the
`Version` check:

- The string must not be null or empty.
- The string must not exceed 50 characters.
- The string must not be whitespace.
- The string must match the semantic version pattern
  `MAJOR.MINOR.PATCH`, optionally followed by a `-prerelease` tag and/or a
  `+build` metadata suffix (e.g. `1.2.3`, `1.2.3-beta.1`, `1.2.3+build.5`).

## Usage

```csharp
using EfMigrationDiff.Plugins;

var system = new PluginSystem("plugins");

// Load plugins from the configured directory.
await system.LoadPluginsAsync();

// Validate the registry and every loaded plugin.
var problems = system.Validate();
if (problems.Count > 0)
{
    foreach (var problem in problems)
    {
        Console.WriteLine($"- {problem}");
    }
}

// Or use the convenience wrappers.
if (system.IsValid())
{
    Console.WriteLine("Plugin system is valid.");
}

// Throws ArgumentException listing all problems when invalid.
system.EnsureValid();
```

## Notes

- Validation is non-destructive: it never mutates the plugin system, only reads its
  statistics and plugin metadata.
- Because `Validate` reads the live registry via `GetStats()` and `GetAllPlugins()`, the
  results reflect the current state at the time of the call.
- The `Name` used in individual-plugin problem messages falls back to the plugin's type
  name when `Name` is null or empty, so problems remain identifiable even for malformed
  plugins.