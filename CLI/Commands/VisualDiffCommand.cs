#nullable enable
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
    // Option names
    private const string SourceOption           = "source";
    private const string TargetOption           = "target";
    private const string BaseOption             = "base";
    private const string FormatOption           = "format";
    private const string OutputOption           = "output";
    private const string MetadataOption         = "metadata";
    private const string IgnoreWhitespaceOption = "ignore-whitespace";

    // Option values and defaults
    private const string DefaultFormat    = "sidebyside";
    private const string UnifiedFormat    = "unified";
    private const string DefaultBaseLabel = "base";

    // Output file naming
    private const string MergeEditorKind = "merge-editor";
    private const string VisualDiffKind  = "visual-diff";
    private const string TimestampFormat = "yyyyMMdd-HHmmss";

    // Console messages
    private const string VisualDiffWrittenPrefix   = "Visual diff written to: ";
    private const string MergeEditorWrittenPrefix  = "Merge editor written to: ";
    private const string SourceOnlyLabel           = "  Source-only: ";
    private const string TargetOnlyLabel           = "Target-only: ";
    private const string ModifiedLabel             = "Modified: ";
    private const string DestructiveWarning        = "  WARNING: destructive schema changes detected — review before merging.";
    private const string SchemaIdenticalMessage    = "  Branches are schema-identical.";
    private const string NoConflictsMessage        = "  No conflicts — merge can be applied cleanly.";
    private const string AutoResolvableSuffix      = " conflict(s) detected, all trivially auto-resolvable.";
    private const string ManualResolutionSuffix    = " conflict(s) require manual resolution — see: ";

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
        ArgumentNullException.ThrowIfNull(pipeline);
        ArgumentNullException.ThrowIfNull(mergeEditor);
        ArgumentNullException.ThrowIfNull(settings);

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
        ArgumentNullException.ThrowIfNull(context);

        var sourceName = context.GetOption(SourceOption) ?? _settings.SourceBranch;
        var targetName = context.GetOption(TargetOption) ?? _settings.TargetBranch;
        var baseName   = context.GetOption(BaseOption);
        var format     = context.GetOption(FormatOption) ?? DefaultFormat;

        var outputFile = context.GetOption(OutputOption)
                         ?? Path.Combine(
                             _settings.GetOutputDirectory(),
                             BuildOutputFilename(sourceName, targetName, baseName));

        var diffOptions = new SchemaDiffOptions
        {
            SourceLabel       = sourceName,
            TargetLabel       = targetName,
            BaseLabel         = baseName ?? DefaultBaseLabel,
            IncludeSqlContent = true,
            IncludeMetadata   = context.HasOption(MetadataOption),
            IgnoreWhitespace  = context.HasOption(IgnoreWhitespaceOption)
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

        var html = format.Equals(UnifiedFormat, StringComparison.OrdinalIgnoreCase)
            ? result.UnifiedHtml
            : result.SideBySideHtml;

        WriteHtmlFile(outputFile, html);
        context.WriteOutput($"{VisualDiffWrittenPrefix}{outputFile}");

        if (result.Diff is { } diff)
        {
            context.WriteOutput(
                $"{SourceOnlyLabel}{diff.SourceOnlyChanges.Count}  " +
                $"{TargetOnlyLabel}{diff.TargetOnlyChanges.Count}  " +
                $"{ModifiedLabel}{diff.ModifiedChanges.Count}");

            if (result.HasDestructiveChanges)
            {
                context.WriteColoredOutput(DestructiveWarning, ConsoleColor.Yellow);
                return 1;
            }

            if (diff.IsIdentical)
                context.WriteColoredOutput(SchemaIdenticalMessage, ConsoleColor.Green);
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
        context.WriteOutput($"{MergeEditorWrittenPrefix}{outputFile}");

        if (result.ThreeWayDiff is { } diff)
        {
            if (diff.ConflictCount == 0)
            {
                context.WriteColoredOutput(NoConflictsMessage, ConsoleColor.Green);
                return 0;
            }

            if (diff.IsAutoMergeable)
            {
                context.WriteColoredOutput(
                    $"  {diff.ConflictCount}{AutoResolvableSuffix}",
                    ConsoleColor.Cyan);
                return 0;
            }

            context.WriteColoredOutput(
                $"  {diff.ConflictCount}{ManualResolutionSuffix}{outputFile}",
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
        var kind  = baseBranch is not null ? MergeEditorKind : VisualDiffKind;
        var stamp = DateTime.UtcNow.ToString(TimestampFormat);
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
