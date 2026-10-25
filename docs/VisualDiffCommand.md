# VisualDiffCommand

`VisualDiffCommand` implements the v2 `visual-diff` command. It runs a schema diff between two branches (or a three-way merge when a base branch is given), writes the result to a self-contained HTML file, prints a short summary, and returns an exit code.

The class is `public sealed` and lives in `CLI/Commands/VisualDiffCommand.cs` under the namespace `EfMigrationDiff.CLI.Commands`. It does not implement an interface.

## Purpose

- Compare the schema changes of a source branch and a target branch and render them as HTML (side-by-side or unified).
- When `--base` is given, render a three-way merge editor page for base, source, and target, and report conflicts.
- Report the outcome as a process exit code (`0` or `1`) so that a script or CI step can react to it.

The class does no diffing itself. It delegates to `SchemaDiffPipelineService` (`Services/SchemaDiffPipelineService.cs`), which bridges the v1 migration data with the v2 engine.

## Dependencies

| Parameter | Type | Role |
|---|---|---|
| `pipeline` | `SchemaDiffPipelineService` | Runs `RunTwoWayDiff` and `RunThreeWayDiff`, and supplies the HTML. |
| `mergeEditor` | `IMergeEditor` | Stored in a field. No method in this class calls it. |
| `settings` | `AppSettings` | Supplies default source and target branches, and the output directory. |

## Public API

The class has one public constructor and one public method. There are no other public members.

### Constructor

```csharp
public VisualDiffCommand(
    SchemaDiffPipelineService pipeline,
    IMergeEditor              mergeEditor,
    AppSettings               settings)
```

Each argument is checked with `ArgumentNullException.ThrowIfNull`. A `null` argument throws `ArgumentNullException`.

In the DI composition root, `SchemaDiffServiceExtensions.AddSchemaDiffPipeline` registers the class as `AddTransient<VisualDiffCommand>()`. `IMergeEditor` is registered as a singleton that resolves to `SchemaDiffEngine`.

### `Execute`

```csharp
public int Execute(CommandContext context)
```

Runs the command. `context` must not be `null` (`ArgumentNullException`).

**Returns**: `0` or `1`, as described under [Exit codes](#exit-codes).

**Options**: read from `context.GetOption` and `context.HasOption`:

| Option | Type | Default | Behavior |
|---|---|---|---|
| `--source <branch>` | value | `AppSettings.SourceBranch` (`develop`) | Source branch name. Also used as the source label. |
| `--target <branch>` | value | `AppSettings.TargetBranch` (`main`) | Target branch name. Also used as the target label. |
| `--base <branch>` | value | none | When present, runs the three-way merge editor. Its value is the base branch and the base label. |
| `--format <name>` | value | `sidebyside` | Two-way mode only. `unified` (case-insensitive) selects the unified view. Any other value selects side-by-side. Ignored in three-way mode. |
| `--output <path>` | value | generated file name in `AppSettings.GetOutputDirectory()` | Explicit output file path. Used as given. |
| `--metadata` | flag | off | Sets `IncludeMetadata` when the option is present. |
| `--ignore-whitespace` | flag | off | Sets `IgnoreWhitespace` when the option is present. |

Flags are recognised by name. The parser stores them as `"true"`. `HasOption` checks only whether the key is present.

`IncludeSqlContent` is always set to `true`.

**Output file name**: when `--output` is not given, the name is built as follows:

```
{kind}-{source}-vs-{target}-{yyyyMMdd-HHmmss}.html
```

- `kind` is `merge-editor` when `--base` is given, and `visual-diff` otherwise.
- `source` and `target` have `/` and `\` replaced by `-`, and leading `-` characters removed.
- The timestamp is UTC.

### Two-way flow (no `--base`)

1. Runs `RunTwoWayDiff` with `BranchInfo` objects built from the source and target names. The second field of each `BranchInfo` is an empty string.
2. Picks `UnifiedHtml` or `SideBySideHtml` based on `--format`.
3. Writes the HTML to the output file as UTF-8. The directory is created if it does not exist.
4. Prints `Visual diff written to: <path>`.
5. If the result has a `Diff`, prints one summary line:
   `  Source-only: <n>  Target-only: <n>  Modified: <n>`
6. If `HasDestructiveChanges` is `true`, prints a yellow warning and returns `1`.
7. Otherwise, if `Diff.IsIdentical` is `true`, prints a green `Branches are schema-identical.` and returns `0`.
8. Returns `0` in all other cases, including when there is no `Diff`.

### Three-way flow (`--base` given)

1. Runs `RunThreeWayDiff` with the base, source, and target `BranchInfo` objects.
2. Writes `MergeEditorHtml` to the output file and prints `Merge editor written to: <path>`.
3. If the result has a `ThreeWayDiff`:
   - `ConflictCount == 0`: prints a green `No conflicts — merge can be applied cleanly.` and returns `0`.
   - `IsAutoMergeable` is `true`: prints a cyan `  <n> conflict(s) detected, all trivially auto-resolvable.` and returns `0`.
   - Otherwise: prints a yellow `  <n> conflict(s) require manual resolution — see: <path>` and returns `1`.
4. Returns `0` when there is no `ThreeWayDiff`.

Destructive changes are not checked in three-way mode. Only conflicts affect the exit code there.

## Exit codes

| Code | Meaning |
|---|---|
| `0` | Two-way: no destructive changes. Three-way: no conflicts, or all conflicts are trivially auto-resolvable. |
| `1` | Two-way: destructive changes were detected. Three-way: at least one conflict needs manual resolution. |

## Usage

The class is resolved from the service provider and executed with a `CommandContext` whose `ParsedOptions` hold the option values. The context name and the raw arguments are not used by `Execute`.

```csharp
using EfMigrationDiff.CLI;
using EfMigrationDiff.CLI.Commands;
using Microsoft.Extensions.DependencyInjection;

// serviceProvider must have AddSchemaDiffPipeline() and the v1 services registered.
var context = new CommandContext("visual-diff", Array.Empty<string>(), serviceProvider)
{
    ParsedOptions = new Dictionary<string, string>
    {
        ["source"] = "feature/orders",
        ["target"] = "main",
        ["format"] = "unified",
        ["output"] = "reports/orders-vs-main.html"
    }
};

var command  = serviceProvider.GetRequiredService<VisualDiffCommand>();
int exitCode = command.Execute(context);   // 0 or 1
```

Three-way merge editor, with the output file generated under the configured output directory:

```csharp
var context = new CommandContext("visual-diff", Array.Empty<string>(), serviceProvider)
{
    ParsedOptions = new Dictionary<string, string>
    {
        ["source"] = "feature/orders",
        ["target"] = "main",
        ["base"]   = "develop"
    }
};

int exitCode = serviceProvider.GetRequiredService<VisualDiffCommand>().Execute(context);
```

## Notes

- `Program.cs` does not call this class for the `visual-diff` command. `MigrationDiffApplication` maps `visual-diff` and `visual` to its own private `VisualDiffCommand(string[])` method, which takes positional arguments (`<source> <target> [format]`), always writes `visual-diff-<stamp>.html`, and calls `Environment.Exit(1)` on destructive changes. The class described here is not reached from that path.
- `--base` is handled only by this class. `Program.cs` does not support it.
- The `--format` option is read by name in this class. The `Program.cs` path reads it as a positional argument.
- `mergeEditor` is stored but unused. Three-way rendering and conflict counts come from `SchemaDiffPipelineService.RunThreeWayDiff`.

## Related

- `docs/SchemaDiffEngine.md`: the v2 engine behind `IMergeEditor` and the diffs.
- `docs/VisualDiffFormatter.md`: produces the HTML written by this command.
- `docs/Program.md`: the `visual-diff` path that does not use this class.
- `docs/CompareCommand.md`: the v1 `compare` command, for comparison.
