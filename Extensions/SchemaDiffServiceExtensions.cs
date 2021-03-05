#nullable enable
// =============================================================================
// Author: Vladyslav Zaiets | https://sarmkadan.com
// CTO & Software Architect
// =============================================================================

using System.Text;
using EfMigrationDiff.CLI.Commands;
using EfMigrationDiff.Configuration;
using EfMigrationDiff.Formatters;
using EfMigrationDiff.Interfaces;
using EfMigrationDiff.Models;
using EfMigrationDiff.Services;
using Microsoft.Extensions.DependencyInjection;

namespace EfMigrationDiff.Extensions;

/// <summary>
/// Extension methods for registering schema diff v2 services and for working fluently
/// with <see cref="SchemaDiffResult"/> and <see cref="ThreeWayDiffResult"/> instances.
/// </summary>
public static class SchemaDiffServiceExtensions
{
    // =========================================================================
    // IServiceCollection — DI registration
    // =========================================================================

    /// <summary>
    /// Registers all schema diff v2 services with default <see cref="SchemaDiffOptions"/>.
    /// Adds <see cref="SchemaDiffEngine"/> (as both <see cref="ISchemaDiffEngine"/> and
    /// <see cref="IMergeEditor"/>) and <see cref="VisualDiffFormatter"/> as
    /// <see cref="IVisualDiffRenderer"/>.
    /// </summary>
    /// <param name="services">The <see cref="IServiceCollection"/> to configure.</param>
    /// <returns>The same <see cref="IServiceCollection"/> for chaining.</returns>
    public static IServiceCollection AddSchemaDiffServices(this IServiceCollection services)
    {
        services.AddSingleton<SchemaDiffEngine>();
        services.AddSingleton<ISchemaDiffEngine>(sp => sp.GetRequiredService<SchemaDiffEngine>());
        services.AddSingleton<IMergeEditor>(sp      => sp.GetRequiredService<SchemaDiffEngine>());
        services.AddSingleton<IVisualDiffRenderer, VisualDiffFormatter>();
        services.AddSingleton(SchemaDiffOptions.Default);

        return services;
    }

    /// <summary>
    /// Registers all schema diff v2 services with custom <see cref="SchemaDiffOptions"/>
    /// produced by <paramref name="configure"/>.
    /// </summary>
    /// <param name="services">The <see cref="IServiceCollection"/> to configure.</param>
    /// <param name="configure">
    /// Factory that returns the <see cref="SchemaDiffOptions"/> to register as a singleton.
    /// </param>
    /// <returns>The same <see cref="IServiceCollection"/> for chaining.</returns>
    public static IServiceCollection AddSchemaDiffServices(
        this IServiceCollection services,
        Func<SchemaDiffOptions> configure)
    {
        services.AddSchemaDiffServices();
        // Override the default options registered by the overload above.
        services.AddSingleton(configure());
        return services;
    }

    // =========================================================================
    // SchemaDiffResult extensions
    // =========================================================================

    /// <summary>
    /// Renders the diff as a side-by-side HTML document using the provided renderer.
    /// </summary>
    /// <param name="diff">The diff to render.</param>
    /// <param name="renderer">The <see cref="IVisualDiffRenderer"/> to use.</param>
    /// <returns>A self-contained HTML document string.</returns>
    public static string ToSideBySideHtml(this SchemaDiffResult diff, IVisualDiffRenderer renderer) =>
        renderer.RenderSideBySide(diff);

    /// <summary>
    /// Renders the diff as a unified HTML document using the provided renderer.
    /// </summary>
    /// <param name="diff">The diff to render.</param>
    /// <param name="renderer">The <see cref="IVisualDiffRenderer"/> to use.</param>
    /// <returns>A self-contained HTML document string.</returns>
    public static string ToUnifiedHtml(this SchemaDiffResult diff, IVisualDiffRenderer renderer) =>
        renderer.RenderUnified(diff);

    /// <summary>
    /// Returns all destructive <see cref="SchemaChange"/> entries from both sides of the diff.
    /// Destructive operations include dropping tables, columns, indexes, and foreign keys.
    /// </summary>
    /// <param name="diff">The diff to inspect.</param>
    public static IEnumerable<SchemaChange> GetDestructiveChanges(this SchemaDiffResult diff) =>
        diff.SourceOnlyChanges.Concat(diff.TargetOnlyChanges).Where(c => c.IsDestructive());

    /// <summary>
    /// Builds a concise plain-text summary of the diff suitable for console output or log entries.
    /// </summary>
    /// <param name="diff">The diff to summarize.</param>
    /// <returns>A multi-line plain-text summary string.</returns>
    public static string ToTextSummary(this SchemaDiffResult diff)
    {
        var sb = new StringBuilder();
        sb.AppendLine($"Diff: {diff.SourceLabel} → {diff.TargetLabel}");
        sb.AppendLine($"  Hunks:         {diff.Hunks.Count}");
        sb.AppendLine($"  Added lines:   {diff.TotalAdded}");
        sb.AppendLine($"  Removed lines: {diff.TotalRemoved}");
        sb.AppendLine($"  Source-only:   {diff.SourceOnlyChanges.Count} change(s)");
        sb.AppendLine($"  Target-only:   {diff.TargetOnlyChanges.Count} change(s)");
        sb.AppendLine($"  Modified:      {diff.ModifiedChanges.Count} change(s)");
        sb.AppendLine($"  Destructive:   {(diff.HasDestructiveChanges ? "YES — review required" : "none")}");
        sb.AppendLine($"  Identical:     {diff.IsIdentical}");
        return sb.ToString();
    }

    // =========================================================================
    // ThreeWayDiffResult extensions
    // =========================================================================

    /// <summary>
    /// Renders the three-way diff as a merge editor HTML document.
    /// </summary>
    /// <param name="diff">The three-way diff to render.</param>
    /// <param name="renderer">The <see cref="IVisualDiffRenderer"/> to use.</param>
    /// <returns>A self-contained HTML document string representing the merge editor.</returns>
    public static string ToMergeEditorHtml(this ThreeWayDiffResult diff, IVisualDiffRenderer renderer) =>
        renderer.RenderMergeEditor(diff);

    /// <summary>
    /// Returns <c>true</c> when the three-way diff has zero conflict regions,
    /// indicating the merge requires no manual resolution.
    /// </summary>
    /// <param name="diff">The three-way diff to inspect.</param>
    public static bool IsCleanMerge(this ThreeWayDiffResult diff) =>
        diff.ConflictCount == 0;

    /// <summary>
    /// Attempts to auto-resolve all trivially resolvable conflicts via
    /// <paramref name="editor"/> and returns the resulting plan.
    /// Irreconcilable regions remain <see cref="MergeResolutionStrategy.Unresolved"/>.
    /// </summary>
    /// <param name="diff">The three-way diff to process.</param>
    /// <param name="editor">The <see cref="IMergeEditor"/> to use.</param>
    /// <returns>A <see cref="MergeResolutionPlan"/> with trivial conflicts resolved.</returns>
    public static MergeResolutionPlan TryAutoResolve(this ThreeWayDiffResult diff, IMergeEditor editor) =>
        editor.AutoMerge(diff);

    /// <summary>
    /// Returns a summary of conflict regions grouped by their resolution status.
    /// </summary>
    /// <param name="diff">The three-way diff to inspect.</param>
    /// <returns>
    /// A read-only dictionary mapping status labels to counts:
    /// <c>Total</c>, <c>Unresolved</c>, <c>AutoResolvable</c>, <c>Resolved</c>.
    /// </returns>
    public static IReadOnlyDictionary<string, int> GetConflictSummary(this ThreeWayDiffResult diff) =>
        new Dictionary<string, int>
        {
            ["Total"]          = diff.ConflictCount,
            ["Unresolved"]     = diff.ConflictRegions.Count(r => !r.IsResolved),
            ["AutoResolvable"] = diff.ConflictRegions.Count(r => r.IsTriviallyResolvable),
            ["Resolved"]       = diff.ConflictRegions.Count(r => r.IsResolved)
        };

    // =========================================================================
    // Pipeline + CLI registration
    // =========================================================================

    /// <summary>
    /// Registers the v2 schema diff pipeline service (<see cref="SchemaDiffPipelineService"/>)
    /// and the <see cref="VisualDiffCommand"/> alongside the core diff services.
    /// </summary>
    /// <remarks>
    /// <see cref="SchemaDiffServiceExtensions.AddSchemaDiffServices(Microsoft.Extensions.DependencyInjection.IServiceCollection)"/> must be called before or after this method —
    /// it registers <see cref="ISchemaDiffEngine"/> and <see cref="IVisualDiffRenderer"/>
    /// that <see cref="SchemaDiffPipelineService"/> depends on.
    /// The v1 <c>MigrationDiffService</c> must also be registered (handled by the
    /// existing <c>DependencyInjection</c> configuration class).
    /// </remarks>
    /// <param name="services">The <see cref="IServiceCollection"/> to configure.</param>
    /// <returns>The same <see cref="IServiceCollection"/> for chaining.</returns>
    public static IServiceCollection AddSchemaDiffPipeline(this IServiceCollection services)
    {
        services.AddSingleton<SchemaDiffPipelineService>();
        services.AddTransient<VisualDiffCommand>();
        return services;
    }
}
