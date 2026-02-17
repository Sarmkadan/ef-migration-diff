// =============================================================================
// Author: Vladyslav Zaiets | https://sarmkadan.com
// CTO & Software Architect
// =============================================================================

using EfMigrationDiff.Configuration;
using EfMigrationDiff.Interfaces;
using EfMigrationDiff.Models;
using EfMigrationDiff.Services;

namespace EfMigrationDiff.CLI.Commands;

/// <summary>
/// CLI command that drives the v2 visual schema diff feature.
/// </summary>
/// <remarks>
/// Accepts <c>--source</c>, <c>--target</c>, and optionally <c>--base</c> branch name
/// options. When <c>--base</c> is present a full three-way merge editor HTML document is
/// produced; otherwise a two-way side-by-side or unified view is generated depending on
/// the <c>--format</c> option.
///
/// Exit codes:
/// <list type="bullet">
///   <item><description><c>0</c> — diff is clean, or all conflicts are trivially auto-resolvable.</description></item>
///   <item><description><c>1</c> — destructive changes detected, or unresolvable conflicts present.</description></item>
/// </list>
/// </remarks>
public sealed class VisualDiffCommand
{
    private readonly SchemaDiffPipelineService _pipeline;
    private readonly IMergeEditor              _mergeEditor;
    private readonly AppSettings               _settings;

    /// <summary>
    /// Initialises the command with required v2 service dependencies.
    /// </summary>
    /// <param name="pipeline">Pipeline that bridges v1 migration data with the v2 diff engine.</param>
    /// <param name="mergeEditor">Editor used when an auto-merge is requested.</param>
    /// <param name="settings">Application settings for default branches and output directory.</param>
    public VisualDiffCommand(
        SchemaDiffPipelineService pipeline,
        IMergeEditor              mergeEditor,
        AppSettings               settings)
    {
        _pipeline    = pipeline;
        _mergeEditor = mergeEditor;
        _settings    = settings;
    }

    // =========================================================================
    // Command entry point
    // =========================================================================

    /// <summary>
    /// Executes the visual-diff command, writing the HTML report to disk and printing
    /// a summary to the command context's output stream.
    /// </summary>
    /// <param name="context">Runtime context carrying parsed arguments and options.</param>
    /// <returns>
    /// <c>0</c> when the diff is clean or auto-resolvable; <c>1</c> when manual review
    /// is required.
    /// </returns>
    public int Execute(CommandContext context)
    {
        var sourceName = context.GetOption("source") ?? _settings.SourceBranch;
        var targetName = context.GetOption("target") ?? _settings.TargetBranch;
        var baseName   = context.GetOption("base");
        var format     = context.GetOption("format") ?? "sidebyside";

        var outputFile = context.GetOption("output")
                         ?? Path.Combine(
                             _settings.GetOutputDirectory(),
                             BuildOutputFilename(sourceName, targetName, baseName));

        var diffOptions = new SchemaDiffOptions
        {
            SourceLabel       = sourceName,
            TargetLabel       = targetName,
            BaseLabel         = baseName ?? "base",
            IncludeSqlContent = true,
            IncludeMetadata   = context.HasOption("metadata"),
            IgnoreWhitespace  = context.HasOption("ignore-whitespace")
        };

        var source = MakeBranchInfo(sourceName);
        var target = MakeBranchInfo(targetName);

        return baseName is not null
            ? ExecuteThreeWayDiff(context, MakeBranchInfo(baseName), source, target, diffOptions, outputFile)
            : ExecuteTwoWayDiff(context, source, target, diffOptions, format, outputFile);
    }

    // =========================================================================
    // Two-way diff
    // =========================================================================

    private int ExecuteTwoWayDiff(
        CommandContext    context,
        BranchInfo        source,
        BranchInfo        target,
        SchemaDiffOptions options,
        string            format,
        string            outputFile)
    {
        var result = _pipeline.RunTwoWayDiff(source, target, options);

        var html = format.Equals("unified", StringComparison.OrdinalIgnoreCase)
            ? result.UnifiedHtml
            : result.SideBySideHtml;

        WriteHtmlFile(outputFile, html);
        context.WriteOutput($"Visual diff written to: {outputFile}");

        if (result.Diff is { } diff)
        {
            context.WriteOutput(
                $"  Source-only: {diff.SourceOnlyChanges.Count}  " +
                $"Target-only: {diff.TargetOnlyChanges.Count}  " +
                $"Modified: {diff.ModifiedChanges.Count}");

            if (result.HasDestructiveChanges)
            {
                context.WriteColoredOutput(
                    "  WARNING: destructive schema changes detected — review before merging.",
                    ConsoleColor.Yellow);
                return 1;
            }

            if (diff.IsIdentical)
                context.WriteColoredOutput("  Branches are schema-identical.", ConsoleColor.Green);
        }

        return 0;
    }

    // =========================================================================
    // Three-way merge editor
    // =========================================================================

    private int ExecuteThreeWayDiff(
        CommandContext    context,
        BranchInfo        baseBranch,
        BranchInfo        source,
        BranchInfo        target,
        SchemaDiffOptions options,
        string            outputFile)
    {
        var result = _pipeline.RunThreeWayDiff(baseBranch, source, target, options);

        WriteHtmlFile(outputFile, result.MergeEditorHtml);
        context.WriteOutput($"Merge editor written to: {outputFile}");

        if (result.ThreeWayDiff is { } diff)
        {
            if (diff.ConflictCount == 0)
            {
                context.WriteColoredOutput(
                    "  No conflicts — merge can be applied cleanly.",
                    ConsoleColor.Green);
                return 0;
            }

            if (diff.IsAutoMergeable)
            {
                context.WriteColoredOutput(
                    $"  {diff.ConflictCount} conflict(s) detected, all trivially auto-resolvable.",
                    ConsoleColor.Cyan);
                return 0;
            }

            context.WriteColoredOutput(
                $"  {diff.ConflictCount} conflict(s) require manual resolution — see: {outputFile}",
                ConsoleColor.Yellow);
            return 1;
        }

        return 0;
    }

    // =========================================================================
    // Private helpers
    // =========================================================================

    private static BranchInfo MakeBranchInfo(string branchName) =>
        new(branchName, string.Empty);

    private static string BuildOutputFilename(string source, string target, string? baseBranch)
    {
        var kind  = baseBranch is not null ? "merge-editor" : "visual-diff";
        var stamp = DateTime.UtcNow.ToString("yyyyMMdd-HHmmss");
        return $"{kind}-{SanitiseName(source)}-vs-{SanitiseName(target)}-{stamp}.html";
    }

    private static string SanitiseName(string name) =>
        name.Replace('/', '-').Replace('\\', '-').TrimStart('-');

    private static void WriteHtmlFile(string path, string content)
    {
        var dir = Path.GetDirectoryName(path);
        if (!string.IsNullOrEmpty(dir) && !Directory.Exists(dir))
            Directory.CreateDirectory(dir);

        File.WriteAllText(path, content, System.Text.Encoding.UTF8);
    }
}
