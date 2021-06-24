#nullable enable

using System.Globalization;
using System.Text.RegularExpressions;

namespace EfMigrationDiff.Plugins;

/// <summary>
/// Provides validation helpers for <see cref="PluginSystem"/> instances.
/// </summary>
public static class PluginSystemValidation
{
    /// <summary>
    /// Validates a <see cref="PluginSystem"/> instance and returns a list of human-readable problems.
    /// </summary>
    /// <param name="value">The plugin system instance to validate.</param>
    /// <returns>An immutable list of validation problems; empty if the instance is valid.</returns>
    /// <exception cref="ArgumentNullException">Thrown if <paramref name="value"/> is null.</exception>
    public static IReadOnlyList<string> Validate(this PluginSystem value)
    {
        ArgumentNullException.ThrowIfNull(value);

        var problems = new List<string>();

        // Validate plugin directories (if accessible)
        var pluginStats = value.GetStats();

        if (pluginStats.TotalPlugins < 0)
        {
            problems.Add("TotalPlugins cannot be negative.");
        }

        // Validate plugin names
        if (pluginStats.PluginNames is null)
        {
            problems.Add("PluginNames collection cannot be null.");
        }
        else
        {
            if (pluginStats.PluginNames.Count != pluginStats.TotalPlugins)
            {
                problems.Add("PluginNames count does not match TotalPlugins.");
            }

            for (var i = 0; i < pluginStats.PluginNames.Count; i++)
            {
                var pluginName = pluginStats.PluginNames[i];
                if (string.IsNullOrWhiteSpace(pluginName))
                {
                    problems.Add($"PluginNames[{i}] cannot be null, empty, or whitespace.");
                }
                else if (pluginName.Length > 255)
                {
                    problems.Add($"PluginNames[{i}] exceeds maximum length of 255 characters.");
                }

                // Check for invalid characters in plugin names
                if (pluginName.Any(c => char.IsControl(c) || char.IsSurrogate(c)))
                {
                    problems.Add($"PluginNames[{i}] contains invalid control or surrogate characters.");
                }
            }
        }

        // Validate individual plugins if any are loaded
        foreach (var plugin in value.GetAllPlugins())
        {
            if (plugin is null)
            {
                problems.Add("GetAllPlugins() returned a null plugin instance.");
                continue;
            }

            // Validate plugin metadata
            if (string.IsNullOrWhiteSpace(plugin.Name))
            {
                problems.Add($"Plugin '{plugin.GetType().Name}' has null or empty Name.");
            }
            else if (plugin.Name.Length > 255)
            {
                problems.Add($"Plugin '{plugin.Name}' Name exceeds maximum length of 255 characters.");
            }

            if (string.IsNullOrWhiteSpace(plugin.Version))
            {
                problems.Add($"Plugin '{plugin.Name ?? "unknown"}' has null or empty Version.");
            }
            else if (!IsValidSemanticVersion(plugin.Version))
            {
                problems.Add($"Plugin '{plugin.Name ?? "unknown"}' has invalid semantic version format: '{plugin.Version}'. Expected format: MAJOR.MINOR.PATCH.");
            }

            if (string.IsNullOrWhiteSpace(plugin.Author))
            {
                problems.Add($"Plugin '{plugin.Name ?? "unknown"}' has null or empty Author.");
            }
            else if (plugin.Author.Length > 255)
            {
                problems.Add($"Plugin '{plugin.Name ?? "unknown"}' Author exceeds maximum length of 255 characters.");
            }
        }

        return problems.AsReadOnly();
    }

    /// <summary>
    /// Determines whether a <see cref="PluginSystem"/> instance is valid.
    /// </summary>
    /// <param name="value">The plugin system instance to check.</param>
    /// <returns><c>true</c> if the instance is valid; otherwise, <c>false</c>.</returns>
    /// <exception cref="ArgumentNullException">Thrown if <paramref name="value"/> is null.</exception>
    public static bool IsValid(this PluginSystem value)
    {
        return value.Validate().Count == 0;
    }

    /// <summary>
    /// Ensures that a <see cref="PluginSystem"/> instance is valid, throwing an <see cref="ArgumentException"/>
    /// with a detailed message if it is not.
    /// </summary>
    /// <param name="value">The plugin system instance to validate.</param>
    /// <exception cref="ArgumentNullException">Thrown if <paramref name="value"/> is null.</exception>
    /// <exception cref="ArgumentException">Thrown if the instance is invalid, containing a list of problems.</exception>
    public static void EnsureValid(this PluginSystem value)
    {
        ArgumentNullException.ThrowIfNull(value);

        var problems = value.Validate();

        if (problems.Count > 0)
        {
            throw new ArgumentException(
                $"PluginSystem validation failed with {problems.Count} problem(s):{Environment.NewLine}" +
                string.Join(Environment.NewLine, problems.Select((p, i) => $"  {i + 1}. {p}")));
        }
    }

    /// <summary>
    /// Validates whether a version string follows semantic versioning format (MAJOR.MINOR.PATCH).
    /// </summary>
    /// <param name="version">The version string to validate.</param>
    /// <returns><c>true</c> if the version is valid semantic version; otherwise, <c>false</c>.</returns>
    private static bool IsValidSemanticVersion(string version)
    {
        if (string.IsNullOrWhiteSpace(version))
        {
            return false;
        }

        // Basic semantic version pattern: MAJOR.MINOR.PATCH
        // Allow for pre-release tags and build metadata
        var pattern = @"^(0|[1-9]\d*)\.(0|[1-9]\d*)\.(0|[1-9]\d*)(?:-((?:0|[1-9]\d*|\d*[a-zA-Z-][0-9a-zA-Z-]*)(?:\.(?:0|[1-9]\d*|\d*[a-zA-Z-][0-9a-zA-Z-]*))*))?(?:\+([0-9a-zA-Z-]+(?:\.[0-9a-zA-Z-]+)*))?$";
        return System.Text.RegularExpressions.Regex.IsMatch(version, pattern, RegexOptions.CultureInvariant);
    }
}