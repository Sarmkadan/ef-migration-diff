# VisualDiffFormatter

`VisualDiffFormatter` (namespace `EfMigrationDiff.Formatters`, file `Formatters/VisualDiffFormatter.cs`) renders schema diffs and three-way merge results as **self-contained HTML documents**. Each public method returns a complete page with inline CSS, so the output can be written to a file and opened in a browser with no external assets.

It is the default implementation of the `IVisualDiffRenderer` interface (`Interfaces/ISchemaDiffEngine.cs`). The class is `sealed` and is registered as a singleton in the DI composition root (`Extensions/SchemaDiffServiceExtensions.cs`, `AddSingleton<IVisualDiffRenderer, VisualDiffFormatter>()`), so callers should depend on the interface rather than the concrete type.

The formatter does not compute diffs. It consumes results produced by `SchemaDiffEngine` (see `docs/SchemaDiffEngine.md`) and only handles presentation.

## Purpose

- Turn a `SchemaDiffResult` (two-way diff) into an HTML page in side-by-side or unified layout.
- Turn a `ThreeWayDiffResult` (base / source / target) into an HTML merge-editor page that lists conflict regions and shows both two-way sub-diffs.
- Escape every user-supplied string (branch labels, line content, migration-derived descriptions) so that migration SQL cannot inject markup.

## Public API

The class exposes three public methods, all from `IVisualDiffRenderer`. There are no other public members.

| Method | Input | Output |
|---|---|---|
| `string RenderSideBySide(SchemaDiffResult diff)` | Two-way diff | HTML page, source pane on the left, target pane on the right |
| `string RenderUnified(SchemaDiffResult diff)` | Two-way diff | HTML page, one table with `Source` / `Target` line numbers and `+` / `-` markers |
| `string RenderMergeEditor(ThreeWayDiffResult diff)` | Three-way diff | HTML page with merge summary, conflict regions, and base→source / base→target sub-diffs |

### `RenderSideBySide`

```csharp
public string RenderSideBySide(SchemaDiffResult diff)
```

**Purpose**: Renders the hunks as two columns. Each pane has a header with the branch label. Lines that belong to the other side are replaced with empty placeholder rows so that both panes stay vertically aligned. A "Schema Change Summary" table follows the panes when there are any schema-level changes.

**Parameters**:
- `diff` – The two-way diff. Must not be `null`.

**Returns**: A complete HTML document. The `<title>` is `Schema Diff — {source} → {target}`.

**Behavior**:
- If `diff.IsIdentical` is `true`, the panes are omitted and an info alert reads *"No schema differences detected — the two branches are identical."*
- Otherwise the source pane shows `Removed` and `Unchanged` lines, and the target pane shows `Added` and `Unchanged` lines.

**Throws**: `ArgumentNullException` if `diff` is `null`.

### `RenderUnified`

```csharp
public string RenderUnified(SchemaDiffResult diff)
```

**Purpose**: Renders all hunks in one table, in the style of a unified diff. Each hunk starts with a header row shaped like `@@ -{SourceStart} +{TargetStart} @@`. Each line row shows the source line number, the target line number, a marker, and the content.

**Parameters**:
- `diff` – The two-way diff. Must not be `null`.

**Returns**: A complete HTML document. The `<title>` is `Unified Diff — {source} → {target}`.

**Column rules**:

| Line kind | Source column | Target column | Marker |
|---|---|---|---|
| `Unchanged` | `LineNumber` | `CorrespondingLineNumber` | space |
| `Removed` | `LineNumber` | empty | `-` |
| `Added` | empty | `LineNumber` | `+` |
| `Modified` | `LineNumber` | empty | space |
| `Placeholder` | — | — | row is skipped |

**Throws**: `ArgumentNullException` if `diff` is `null`.

### `RenderMergeEditor`

```csharp
public string RenderMergeEditor(ThreeWayDiffResult diff)
```

**Purpose**: Shows the state of a three-way merge. The page has a header naming the base, source and target branches, then a status alert, then one block per conflict region, then two sub-diffs.

**Parameters**:
- `diff` – The three-way diff. Must not be `null`.

**Returns**: A complete HTML document. The `<title>` is `Merge Editor — {source} ↔ {target}`.

**Behavior**:
- `ConflictCount == 0` shows a green alert: *"No conflicts detected — this merge can be applied automatically."*
- Otherwise a warning alert (`IsAutoMergeable == true`, every region has identical source and target content) or a danger alert (manual review needed) reports the count of conflict regions.
- Each conflict region is a three-column block labeled with the source label, `base`, and the target label. The block carries `id="conflict-{region.Id}"` and an *auto-resolvable* badge when `IsTriviallyResolvable` is `true`. A region with no base lines shows `(no base content)`.
- `Base → Source changes` and `Base → Target changes` each show a compact unified table of the corresponding `SchemaDiffResult`, or *"No changes from base."* when that result is identical.

**Throws**: `ArgumentNullException` if `diff` is `null`.

## Output format

Every method returns the same page skeleton:

```html
<!DOCTYPE html>
<html lang="en">
<head>
  <meta charset="UTF-8" />
  <meta name="viewport" content="width=device-width, initial-scale=1.0" />
  <title>{escaped title}</title>
  <style>...inline stylesheet...</style>
</head>
<body>
  {body}
  <footer>ef-migration-diff &middot; visual diff engine v2</footer>
</body>
</html>
```

The stylesheet is embedded in the `<style>` element. It defines the `diff-*`, `line-*`, `alert-*`, `conflict-*`, `badge-*` and `summary-table` classes used by the body.

### Body structure (two-way diff)

Each two-way page body has these parts, in order:

1. `<h2 class="diff-title">` with the source and target labels and an arrow.
2. `<div class="diff-stats">` with `+N added`, `-N removed`, `N modified` and, when applicable, `⚠ destructive changes`. Counts come from the hunks (`TotalAdded`, `TotalRemoved`) and from `ModifiedChanges.Count`.
3. The diff container: `diff-container side-by-side` or `diff-container unified`, or an alert when identical.
4. `<h3>Schema Change Summary</h3>` and a `summary-table`, only when there is at least one `SourceOnlyChanges`, `TargetOnlyChanges` or `ModifiedChanges` entry.

### Example: unified output

Input (abbreviated): source label `main`, target label `feature/add-email`. One hunk starts at line 3 on both sides. It keeps `[Id]`, adds `[Email]`, and removes `[Phone]`. The source-only change list contains `DropColumn` for `Users.Phone`.

Body fragment, with the page wrapper and most of the stylesheet omitted:

```html
<h2 class="diff-title">main <span class="arrow">→</span> feature/add-email</h2>
<div class="diff-stats"><span class="stat added">+1 added</span> <span class="stat removed">-1 removed</span> <span class="stat modified">0 modified</span> <span class="stat destructive">⚠ destructive changes</span></div>
<div class="diff-container unified"><table class="diff-table"><thead><tr><th class="ln">Source</th><th class="ln">Target</th><th class="marker"></th><th>Change</th></tr></thead><tbody>
<tr class="hunk-header"><td colspan="4">@@ -3 +3 @@</td></tr>
<tr class="line-unchanged"><td class="ln">3</td><td class="ln">3</td><td class="marker"> </td><td><code>    [Id] int NOT NULL,</code></td></tr>
<tr class="line-removed"><td class="ln">4</td><td class="ln"></td><td class="marker">-</td><td><code>    [Phone] nvarchar(32) NULL,</code></td></tr>
<tr class="line-added"><td class="ln"></td><td class="ln">4</td><td class="marker">+</td><td><code>    [Email] nvarchar(256) NULL,</code></td></tr>
</tbody></table></div>
<h3>Schema Change Summary</h3>
<table class="summary-table"><thead><tr><th>Description</th><th>Table</th><th>Column</th><th>Side</th><th>Destructive</th></tr></thead><tbody>
<tr class="row-danger"><td>Drop column [Phone] from [Users]</td><td>Users</td><td>Phone</td><td>Source only</td><td><span class="badge badge-danger">⚠ Yes</span></td></tr>
</tbody></table>
```

Notes on this example:
- `class="row-danger"` is applied to the summary row because `IsDestructive()` is `true` for `DropColumn`.
- The row and cell markup matches what the code emits. Square brackets and spaces are not HTML-special, so `Enc` leaves `[Id]`, `[Email]` and `[Phone]` unchanged. `<`, `>`, `&`, `"` and `'` would be encoded.

## Escaping and safety

- All labels, descriptions, column names, table names, line content and conflict text go through a private `Enc` helper, which calls `WebUtility.HtmlEncode` and treats `null` as an empty string.
- The formatter never writes raw user strings into attributes or tags. The only raw values are integers (line numbers, hunk starts, counts) and the fixed CSS class names.
- The `Guid` in a conflict region `id` is emitted directly, since `Guid.ToString()` only contains hex digits and hyphens.

## Errors and edge cases

- Every public method throws `ArgumentNullException` for a `null` argument.
- Empty hunks produce no line rows. An empty `Hunks` list with `IsIdentical == true` gives the identical-branches alert, not an empty table.
- A `Placeholder` line is skipped in unified and merge sub-diffs. In side-by-side it becomes an empty `placeholder` row.
- Line numbers are written as-is. Pass a `SchemaDiffResult` produced by `SchemaDiffEngine` so they are one-based and consistent.

## Known behavior to be aware of

These are current behaviors of the code, documented here as-is. This document does not change them.

- **`Modified` lines are hidden in side-by-side view.** `RenderHunkPane` shows only `Removed`/`Unchanged` on the left and `Added`/`Unchanged` on the right. A `Modified` line therefore appears as an empty placeholder row in both panes. It is still visible in the unified and merge sub-diff views, which render every non-placeholder line.
- **Summary-table side labels are fixed.** The side column is set by which list a change came from (`Source only`, `Target only`, `Modified`). It is not derived from the line data.
- **Merge sub-diffs have no line numbers.** `RenderUnifiedInline` writes only the marker and content.

## Related

- `Interfaces/ISchemaDiffEngine.cs` – `IVisualDiffRenderer` contract.
- `Models/SchemaDiffModels.cs` – `DiffLine`, `DiffHunk`, `SchemaDiffResult`, `ThreeWayDiffResult`, `MergeConflictRegion`.
- `Models/SchemaChange.cs` – `GetDescription()` and `IsDestructive()`, which drive the summary table.
- `Configuration/SchemaDiffOptions.cs` – default labels (`source`, `target`, `base`) used by the engine.
- `docs/SchemaDiffEngine.md`, `docs/VisualDiffOutputTests.md` – producer of the diff models and the existing tests for this output.
