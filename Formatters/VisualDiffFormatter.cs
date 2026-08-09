#nullable enable
using System.Net;
using System.Text;
using System;
using EfMigrationDiff.Interfaces;
using EfMigrationDiff.Models;

namespace EfMigrationDiff.Formatters;

/// <summary>
/// Produces rich, self-contained HTML representations of schema diffs and merge editor views.
/// Supports side-by-side, unified, and three-way merge editor layouts with full CSS styling.
/// Implements <see cref="IVisualDiffRenderer"/> so it can be resolved via DI.
/// </summary>
public sealed class VisualDiffFormatter : IVisualDiffRenderer
{
    // =========================================================================
    // IVisualDiffRenderer
    // =========================================================================

    /// <inheritdoc />
    public string RenderSideBySide(SchemaDiffResult diff)
    {
        ArgumentNullException.ThrowIfNull(diff);
        var body = new StringBuilder();

        body.Append(RenderDiffHeader(diff.SourceLabel, diff.TargetLabel, diff));

        if (diff.IsIdentical)
        {
            body.Append(RenderAlert("No schema differences detected — the two branches are identical.", "info"));
        }
        else
        {
            body.Append("<div class=\"diff-container side-by-side\">");

            body.Append($"<div class=\"diff-pane\"><div class=\"pane-header\">{Enc(diff.SourceLabel)}</div>");
            foreach (var hunk in diff.Hunks)
                body.Append(RenderHunkPane(hunk, leftSide: true));
            body.Append("</div>");

            body.Append($"<div class=\"diff-pane\"><div class=\"pane-header\">{Enc(diff.TargetLabel)}</div>");
            foreach (var hunk in diff.Hunks)
                body.Append(RenderHunkPane(hunk, leftSide: false));
            body.Append("</div>");

            body.Append("</div>");
            body.Append(RenderChangeSummaryTable(diff));
        }

        return WrapDocument($"Schema Diff — {diff.SourceLabel} → {diff.TargetLabel}", body.ToString());
    }

    /// <inheritdoc />
    public string RenderUnified(SchemaDiffResult diff)
    {
        ArgumentNullException.ThrowIfNull(diff);
        var body = new StringBuilder();

        body.Append(RenderDiffHeader(diff.SourceLabel, diff.TargetLabel, diff));

        if (diff.IsIdentical)
        {
            body.Append(RenderAlert("No schema differences detected — the two branches are identical.", "info"));
        }
        else
        {
            body.Append("<div class=\"diff-container unified\">");
            body.Append("<table class=\"diff-table\"><thead><tr>");
            body.Append("<th class=\"ln\">Source</th><th class=\"ln\">Target</th>");
            body.Append("<th class=\"marker\"></th><th>Change</th>");
            body.Append("</tr></thead><tbody>");

            foreach (var hunk in diff.Hunks)
            {
                body.Append(
                    $"<tr class=\"hunk-header\"><td colspan=\"4\">" +
                    $"@@ -{hunk.SourceStart} +{hunk.TargetStart} @@" +
                    $"</td></tr>");

                foreach (var line in hunk.Lines)
                {
                    if (line.Kind == DiffLineKind.Placeholder) continue;

                    var cls   = LineClass(line.Kind);
                    var srcLn = line.Kind != DiffLineKind.Added   ? line.LineNumber.ToString() : string.Empty;
                    var tgtLn = line.Kind == DiffLineKind.Added
                        ? line.LineNumber.ToString()
                        : line.Kind == DiffLineKind.Unchanged
                            ? line.CorrespondingLineNumber.ToString()
                            : string.Empty;

                    body.Append($"<tr class=\"{cls}\">");
                    body.Append($"<td class=\"ln\">{srcLn}</td>");
                    body.Append($"<td class=\"ln\">{tgtLn}</td>");
                    body.Append($"<td class=\"marker\">{line.UnifiedMarker}</td>");
                    body.Append($"<td><code>{Enc(line.Content)}</code></td>");
                    body.Append("</tr>");
                }
            }

            body.Append("</tbody></table></div>");
            body.Append(RenderChangeSummaryTable(diff));
        }

        return WrapDocument($"Unified Diff — {diff.SourceLabel} → {diff.TargetLabel}", body.ToString());
    }

    /// <inheritdoc />
    public string RenderMergeEditor(ThreeWayDiffResult diff)
    {
        ArgumentNullException.ThrowIfNull(diff);
        var body = new StringBuilder();

        body.Append($"<h2 class=\"diff-title\">Merge Editor</h2>");
        body.Append(
            $"<p class=\"diff-meta\">" +
            $"Merging <strong>{Enc(diff.SourceLabel)}</strong> and " +
            $"<strong>{Enc(diff.TargetLabel)}</strong> against base " +
            $"<strong>{Enc(diff.BaseLabel)}</strong>" +
            $"</p>");

        if (diff.ConflictCount == 0)
        {
            body.Append(RenderAlert(
                "No conflicts detected — this merge can be applied automatically.",
                "success"));
        }
        else
        {
            var alertType = diff.IsAutoMergeable ? "warning" : "danger";
            var autoNote  = diff.IsAutoMergeable
                ? " All conflicts are trivially resolvable (both sides are identical)."
                : " Manual review is required for one or more regions.";
            body.Append(RenderAlert(
                $"{diff.ConflictCount} conflict region(s) require resolution.{autoNote}",
                alertType));

            foreach (var (region, idx) in diff.ConflictRegions.Select((r, i) => (r, i + 1)))
                body.Append(RenderConflictRegion(region, idx, diff.SourceLabel, diff.TargetLabel));
        }

        body.Append("<h3>Base → Source changes</h3>");
        body.Append("<div class=\"sub-diff\">");
        body.Append(RenderUnifiedInline(diff.BaseToSource));
        body.Append("</div>");

        body.Append("<h3>Base → Target changes</h3>");
        body.Append("<div class=\"sub-diff\">");
        body.Append(RenderUnifiedInline(diff.BaseToTarget));
        body.Append("</div>");

        return WrapDocument($"Merge Editor — {diff.SourceLabel} ↔ {diff.TargetLabel}", body.ToString());
    }

    // =========================================================================
    // Private rendering helpers
    // =========================================================================

    private static string RenderDiffHeader(string source, string target, SchemaDiffResult diff)
    {
        var sb = new StringBuilder();
        sb.Append($"<h2 class=\"diff-title\">{Enc(source)} <span class=\"arrow\">→</span> {Enc(target)}</h2>");
        sb.Append("<div class=\"diff-stats\">");
        sb.Append($"<span class=\"stat added\">+{diff.TotalAdded} added</span> ");
        sb.Append($"<span class=\"stat removed\">-{diff.TotalRemoved} removed</span> ");
        sb.Append($"<span class=\"stat modified\">{diff.ModifiedChanges.Count} modified</span>");
        if (diff.HasDestructiveChanges)
            sb.Append(" <span class=\"stat destructive\">⚠ destructive changes</span>");
        sb.Append("</div>");
        return sb.ToString();
    }

    private static string RenderHunkPane(DiffHunk hunk, bool leftSide)
    {
        var sb = new StringBuilder();
        sb.Append("<table class=\"diff-table\"><tbody>");

        foreach (var line in hunk.Lines)
        {
            var showOnLeft  = line.Kind is DiffLineKind.Removed or DiffLineKind.Unchanged;
            var showOnRight = line.Kind is DiffLineKind.Added   or DiffLineKind.Unchanged;
            var show        = leftSide ? showOnLeft : showOnRight;

            if (!show || line.Kind == DiffLineKind.Placeholder)
            {
                sb.Append("<tr class=\"placeholder\"><td class=\"ln\"></td><td>&nbsp;</td></tr>");
                continue;
            }

            var cls = LineClass(line.Kind);
            sb.Append($"<tr class=\"{cls}\">");
            sb.Append($"<td class=\"ln\">{line.LineNumber}</td>");
            sb.Append($"<td><code>{Enc(line.Content)}</code></td>");
            sb.Append("</tr>");
        }

        sb.Append("</tbody></table>");
        return sb.ToString();
    }

    private static string RenderConflictRegion(
        MergeConflictRegion region,
        int index,
        string sourceLabel,
        string targetLabel)
    {
        var sb = new StringBuilder();
        sb.Append($"<div class=\"conflict-region\" id=\"conflict-{region.Id}\">");

        sb.Append($"<div class=\"conflict-header\">");
        sb.Append($"<span class=\"conflict-index\">#{index}</span> {Enc(region.Description)}");
        if (region.IsTriviallyResolvable)
            sb.Append(" <span class=\"badge badge-auto\">auto-resolvable</span>");
        sb.Append("</div>");

        sb.Append("<div class=\"conflict-panes\">");

        sb.Append($"<div class=\"conflict-pane source\"><div class=\"pane-label\">{Enc(sourceLabel)}</div><pre>");
        foreach (var l in region.SourceLines)
            sb.Append(Enc(l.Content) + "\n");
        sb.Append("</pre></div>");

        sb.Append($"<div class=\"conflict-pane base\"><div class=\"pane-label\">base</div><pre>");
        if (region.BaseLines.Count == 0)
            sb.Append("<em>(no base content)</em>");
        else
            foreach (var l in region.BaseLines)
                sb.Append(Enc(l.Content) + "\n");
        sb.Append("</pre></div>");

        sb.Append($"<div class=\"conflict-pane target\"><div class=\"pane-label\">{Enc(targetLabel)}</div><pre>");
        foreach (var l in region.TargetLines)
            sb.Append(Enc(l.Content) + "\n");
        sb.Append("</pre></div>");

        sb.Append("</div></div>");
        return sb.ToString();
    }

    private static string RenderUnifiedInline(SchemaDiffResult diff)
    {
        if (diff.IsIdentical)
            return "<p class=\"no-changes\">No changes from base.</p>";

        var sb = new StringBuilder();
        sb.Append("<table class=\"diff-table compact\"><tbody>");

        foreach (var hunk in diff.Hunks)
        foreach (var line in hunk.Lines)
        {
            if (line.Kind == DiffLineKind.Placeholder) continue;
            sb.Append($"<tr class=\"{LineClass(line.Kind)}\">");
            sb.Append($"<td class=\"marker\">{line.UnifiedMarker}</td>");
            sb.Append($"<td><code>{Enc(line.Content)}</code></td>");
            sb.Append("</tr>");
        }

        sb.Append("</tbody></table>");
        return sb.ToString();
    }

    private static string RenderChangeSummaryTable(SchemaDiffResult diff)
    {
        if (diff.SourceOnlyChanges.Count == 0 &&
            diff.TargetOnlyChanges.Count == 0 &&
            diff.ModifiedChanges.Count    == 0)
            return string.Empty;

        var sb = new StringBuilder();
        sb.Append("<h3>Schema Change Summary</h3>");
        sb.Append("<table class=\"summary-table\"><thead><tr>");
        sb.Append("<th>Description</th><th>Table</th><th>Column</th><th>Side</th><th>Destructive</th>");
        sb.Append("</tr></thead><tbody>");

        void AppendRow(SchemaChange c, string side)
        {
            var destr = c.IsDestructive()
                ? "<span class=\"badge badge-danger\">⚠ Yes</span>"
                : "No";
            var rowCls = c.IsDestructive() ? " class=\"row-danger\"" : string.Empty;
            sb.Append($"<tr{rowCls}>");
            sb.Append($"<td>{Enc(c.GetDescription())}</td>");
            sb.Append($"<td>{Enc(c.TableName)}</td>");
            sb.Append($"<td>{Enc(c.ColumnName)}</td>");
            sb.Append($"<td>{Enc(side)}</td>");
            sb.Append($"<td>{destr}</td>");
            sb.Append("</tr>");
        }

        foreach (var c in diff.SourceOnlyChanges)  AppendRow(c, "Source only");
        foreach (var c in diff.TargetOnlyChanges)  AppendRow(c, "Target only");
        foreach (var c in diff.ModifiedChanges)    AppendRow(c, "Modified");

        sb.Append("</tbody></table>");
        return sb.ToString();
    }

    private static string RenderAlert(string message, string type) =>
        $"<div class=\"alert alert-{type}\"><p>{Enc(message)}</p></div>";

    private static string LineClass(DiffLineKind kind) => kind switch
    {
        DiffLineKind.Added     => "line-added",
        DiffLineKind.Removed   => "line-removed",
        DiffLineKind.Modified  => "line-modified",
        DiffLineKind.Unchanged => "line-unchanged",
        _                      => "line-placeholder"
    };

    private static string Enc(string? text) =>
        WebUtility.HtmlEncode(text ?? string.Empty);

    private static string WrapDocument(string title, string body) => $$"""
        <!DOCTYPE html>
        <html lang="en">
        <head>
          <meta charset="UTF-8" />
          <meta name="viewport" content="width=device-width, initial-scale=1.0" />
          <title>{{Enc(title)}}</title>
          <style>
            *, *::before, *::after { box-sizing: border-box; }
            body { font-family: 'Segoe UI', system-ui, sans-serif; margin: 0; padding: 20px 28px; background: #f8f9fa; color: #212529; }
            h2.diff-title { font-size: 1.45rem; border-bottom: 2px solid #3498db; padding-bottom: 10px; color: #2c3e50; margin-bottom: 6px; }
            h3 { font-size: 1.05rem; color: #34495e; margin: 28px 0 8px; }
            .diff-stats { margin: 6px 0 18px; font-size: 0.88rem; display: flex; gap: 8px; flex-wrap: wrap; }
            .stat { display: inline-block; padding: 2px 10px; border-radius: 12px; font-weight: 600; }
            .stat.added       { background: #d4edda; color: #155724; }
            .stat.removed     { background: #f8d7da; color: #721c24; }
            .stat.modified    { background: #fff3cd; color: #856404; }
            .stat.destructive { background: #f8d7da; color: #721c24; border: 1px solid #f5c6cb; }
            .diff-container { border: 1px solid #dee2e6; border-radius: 6px; overflow: hidden; background: #fff; }
            .diff-container.side-by-side { display: grid; grid-template-columns: 1fr 1fr; }
            .diff-pane { overflow-x: auto; }
            .pane-header { padding: 6px 12px; background: #e9ecef; border-bottom: 1px solid #dee2e6; font-weight: 600; font-size: 0.88rem; color: #495057; }
            .diff-table { width: 100%; border-collapse: collapse; font-size: 0.82rem; font-family: 'Cascadia Code','Fira Code','Consolas',monospace; }
            .diff-table td { padding: 2px 8px; white-space: pre; vertical-align: top; }
            .diff-table .ln { width: 44px; min-width: 44px; text-align: right; color: #adb5bd; border-right: 1px solid #dee2e6; user-select: none; }
            .diff-table .marker { width: 18px; min-width: 18px; text-align: center; font-weight: bold; border-right: 1px solid #dee2e6; }
            .diff-table thead th { background: #e9ecef; border-bottom: 2px solid #dee2e6; padding: 5px 8px; font-size: 0.8rem; font-weight: 600; color: #495057; }
            .line-added     { background: #d4edda; }
            .line-removed   { background: #f8d7da; }
            .line-modified  { background: #fff3cd; }
            .line-unchanged { background: #fff; }
            .line-placeholder, .placeholder { background: #f8f9fa; }
            .hunk-header td { background: #cfe2ff; color: #084298; font-family: monospace; padding: 3px 8px; }
            .alert { padding: 12px 16px; border-radius: 6px; margin: 14px 0; font-size: 0.9rem; }
            .alert p { margin: 0; }
            .alert-info    { background: #cff4fc; border-left: 4px solid #0dcaf0; color: #055160; }
            .alert-success { background: #d1e7dd; border-left: 4px solid #198754; color: #0a3622; }
            .alert-warning { background: #fff3cd; border-left: 4px solid #ffc107; color: #664d03; }
            .alert-danger  { background: #f8d7da; border-left: 4px solid #dc3545; color: #58151c; }
            .summary-table { width: 100%; border-collapse: collapse; margin-top: 6px; font-size: 0.85rem; }
            .summary-table th, .summary-table td { border: 1px solid #dee2e6; padding: 6px 10px; text-align: left; }
            .summary-table th { background: #e9ecef; font-weight: 600; color: #495057; }
            .row-danger { background: #fff5f5; }
            .badge { display: inline-block; padding: 2px 7px; border-radius: 10px; font-size: 0.74rem; font-weight: 600; }
            .badge-danger { background: #dc3545; color: #fff; }
            .badge-auto   { background: #198754; color: #fff; }
            .conflict-region { border: 2px solid #dc3545; border-radius: 6px; margin: 16px 0; overflow: hidden; }
            .conflict-header { background: #dc3545; color: #fff; padding: 8px 14px; font-weight: 600; font-size: 0.9rem; display: flex; align-items: center; gap: 8px; }
            .conflict-index { background: rgba(0,0,0,.2); border-radius: 4px; padding: 1px 6px; font-size: 0.8rem; }
            .conflict-panes { display: grid; grid-template-columns: 1fr 1fr 1fr; }
            .conflict-pane { padding: 10px 12px; border-right: 1px solid #dee2e6; }
            .conflict-pane:last-child { border-right: none; }
            .conflict-pane.source { background: #fffbea; }
            .conflict-pane.base   { background: #f8f9fa; }
            .conflict-pane.target { background: #eaf4fe; }
            .pane-label { font-size: 0.8rem; font-weight: 700; color: #6c757d; margin-bottom: 6px; text-transform: uppercase; letter-spacing: .04em; }
            .conflict-pane pre { margin: 0; font-size: 0.81rem; font-family: 'Cascadia Code','Fira Code',monospace; white-space: pre-wrap; word-break: break-all; }
            .sub-diff { margin: 6px 0 24px; border: 1px solid #dee2e6; border-radius: 6px; overflow: hidden; background: #fff; }
            .diff-table.compact td { padding: 1px 8px; }
            .no-changes { color: #6c757d; font-style: italic; padding: 10px 14px; margin: 0; }
            .arrow { color: #3498db; }
            .diff-meta { color: #6c757d; margin: 4px 0 18px; font-size: 0.9rem; }
            footer { margin-top: 36px; color: #adb5bd; font-size: 0.75rem; text-align: right; border-top: 1px solid #dee2e6; padding-top: 10px; }
          </style>
        </head>
        <body>
          {{body}}
          <footer>ef-migration-diff &middot; visual diff engine v2</footer>
        </body>
        </html>
        """;
}
