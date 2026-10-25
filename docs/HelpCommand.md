# HelpCommand

`HelpCommand` implements `ICommand` and prints the static usage text for the tool: banner, description, command list, options, examples and environment variables. It takes no arguments, reads no configuration and touches no git state. It writes the text through `CommandContext.WriteOutput` and returns a successful `CommandResult`.

The class lives in `CLI/Commands/HelpCommand.cs` under the namespace `EfMigrationDiff.CLI.Commands`. It is declared as `public class HelpCommand : ICommand`, where `ICommand` is in `EfMigrationDiff.CLI` (`CLI/CommandExecutor.cs`).

## Usage

The source does not register `HelpCommand` anywhere. `Program.cs` dispatches `help`, `--help` and `-h` to its own private `ShowHelp()` method. Commands can be registered by name with `CommandExecutor.RegisterCommand` or `ConfigurationBuilder.AddCommand`. If `HelpCommand` is registered with a name such as `help`, it is run the same way as the other commands:

```bash
dotnet run -- help
```

Registration example, based on the `CommandExecutor.RegisterCommand(string, ICommand)` and `ConfigurationBuilder.AddCommand(string, ICommand)` signatures:

```csharp
var executor = new CommandExecutor()
    .RegisterCommand("help", new HelpCommand());
```

The command has no options. `ExecuteAsync` does not read `context.ParsedOptions`, `context.ParsedArguments` or `context.RawArguments`, so any extra tokens are ignored by this class.

## API

### `public string GetDescription()`

Returns the constant `"Display help information"`. It is the `ICommand` description used for help listings.

### `public Task<CommandResult> ExecuteAsync(CommandContext context)`

Builds the help text in a `StringBuilder`, writes it with `context.WriteOutput(...)` and returns `CommandResult.Ok("Help displayed")`.

- Throws `ArgumentNullException` when `context` is `null` (`ArgumentNullException.ThrowIfNull`).
- Does not catch exceptions from `WriteOutput`. Failures from the underlying `TextWriter` propagate to the caller.
- Does not touch `context.ServiceProvider`, so the command runs without DI services being registered.
- Returns `Success = true`, `ExitCode = Constants.ExitCodes.NoDiff` (`0`) and `Data = null`, as set by `CommandResult.Ok`.

The method is `async` but has no `await`. The work is synchronous and the returned task is already completed.

## Output

The text is produced by `ExecuteAsync` and written as a single string. Sections appear in this order:

| Section | Content in source |
|---|---|
| Banner | Box-drawing title `EF Migration Diff - Entity Framework Migration Tool` |
| `DESCRIPTION:` | Two lines: analyzes EF migrations across git branches to detect conflicts, schema changes and incompatibilities |
| `COMMANDS:` | `compare <src> <tgt>`, `validate`, `analyze`, `report`, `help` |
| `OPTIONS:` | `--format <fmt>` (text/json/html), `--output <path>`, `--verbose, -v`, `--help, -h`, `--version, --ver` |
| `EXAMPLES:` | `compare develop main`, `validate`, `compare develop main --format json`, `analyze --verbose` |
| `ENVIRONMENT VARIABLES:` | `EF_MIGRATION_DIFF_REPO`, `EF_MIGRATION_DIFF_OUTPUT` |

All text is hard-coded in `private const string` fields. The output does not depend on the working directory, the environment or the arguments.

## Relationship to `Program.ShowHelp`

`HelpCommand` and `Program.ShowHelp()` (`Program.cs`) are two separate copies of the help text, and they do not match:

- `HelpCommand` lists `analyze` and `report` (without `<fmt>`). `Program.ShowHelp` lists `compare`, `validate`, `report <fmt>`, `visual-diff`, `graph` and `auto-merge`.
- `Program.cs` has no `analyze` dispatch entry, so the `analyze` line and the `analyze --verbose` example in `HelpCommand` do not match a command the dispatcher knows.
- `HelpCommand` has no entries for `visual-diff`, `graph` or `auto-merge`.
- The description wording differs: `HelpCommand` says "Analyzes ..." while `Program.ShowHelp` says "Compares ...".

Changing either copy is a behavior change and is out of scope for this document.

## Usage notes

- Because the text is constant, updating help output means editing the `private const string` fields in `HelpCommand.cs`.
- The class does not depend on `Utilities` at runtime. The file has a `using EfMigrationDiff.Utilities;` directive, but no type from that namespace is referenced in the code.
- `CommandResult.Ok` sets the exit code to `0`, so a successful help run exits with `0`.

## Example

Calling the command through an executor that has it registered as `help`:

```text
$ dotnet run -- help

╔════════════════════════════════════════════════════════════╗
║     EF Migration Diff - Entity Framework Migration Tool     ║
╚════════════════════════════════════════════════════════════╝

DESCRIPTION:
  Analyzes Entity Framework migrations across git branches
  to detect conflicts, schema changes, and incompatibilities.

COMMANDS:
  compare <src> <tgt>    Compare migrations between branches
  validate               Validate migration file structure
  analyze                Analyze migration impact
  report                 Generate detailed reports
  help                   Show this help message

...
```

The output above is abridged. The `OPTIONS`, `EXAMPLES` and `ENVIRONMENT VARIABLES` sections follow in the order listed in [Output](#output).

Calling it directly from C#, for example in a test with a `StringWriter` as output:

```csharp
var output = new StringWriter();
var context = new CommandContext("help", Array.Empty<string>(), serviceProvider, output);

var result = await new HelpCommand().ExecuteAsync(context);

// result.Success == true, result.ExitCode == 0, result.Message == "Help displayed"
// output.ToString() contains the help text
```
