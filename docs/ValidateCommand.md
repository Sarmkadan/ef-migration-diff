# ValidateCommand

`ValidateCommand` implements the `validate` command (aliased as `check`). It scans the migrations directory of the current repository, runs `MigrationParserService.ValidateMigrationFile` on every migration `.cs` file, prints a per-file result with a summary, and returns a pass/fail `CommandResult`.

The class lives in `CLI/Commands/ValidateCommand.cs` under the namespace `EfMigrationDiff.CLI.Commands` and implements `ICommand` (`EfMigrationDiff.CLI`).

## Usage

```bash
dotnet run -- validate
dotnet run -- check
```

The command takes no arguments or options. It validates the working-tree copy of the migrations, not a specific branch. The migrations directory is resolved as `AppSettings.GetMigrationsDirectory()`, which is `AppSettings.RepositoryPath` joined with the configured migrations path. `RepositoryPath` is overwritten with `Environment.CurrentDirectory` before the lookup, so run the command from the repository root.

Files are selected from the migrations directory with the pattern `*.cs`, excluding files whose names end in `.Designer.cs` (case-insensitive). Subdirectories are not searched.

## Behavior

`ExecuteAsync` runs these steps in order:

1. Resolves `AppSettings` and `MigrationParserService` from `context.ServiceProvider`. A missing service throws `InvalidOperationException`.
2. Sets `AppSettings.RepositoryPath` to `Environment.CurrentDirectory` and computes the migrations directory.
3. If the directory does not exist, returns `CommandResult.Error` with the message `Migrations directory not found: <path>` and the default error exit code.
4. Writes `Validating migrations in: <path>` in cyan.
5. Collects the migration files. If none are found, writes `No migration files found.` and returns `CommandResult.Ok("No migrations to validate")`.
6. Writes the file count, then validates each file. Each file is wrapped in a `MigrationFile` with the context name `DefaultContext`, and the parser's errors are collected:
   - No errors: prints `  ✓ <file name>` in green and increments the valid count.
   - Errors: prints `  ✗ <file name>` in red, then each error as `    - <error>`. Each error is also recorded as `<file name>: <error>` in an internal list, and the invalid count is incremented.
7. Prints a separator and `Valid: <n> | Invalid: <m>`. The summary is green when `n > 0` and red otherwise.
8. If any file was invalid, returns `CommandResult.Error("Validation failed: <m> invalid file(s)", 1)`. Otherwise returns `CommandResult.Ok("All <n> migration(s) validated successfully")`.

The internal list of `<file>: <error>` strings is built but not returned or printed separately. The per-file output above is the only report.

## API

### `public string GetDescription()`
Returns `"Validate migration files for structural correctness"`. Used in help output.

### `public Task<CommandResult> ExecuteAsync(CommandContext context)`
Runs the validation as described in [Behavior](#behavior).

- Throws `ArgumentNullException` when `context` or `context.ServiceProvider` is `null`.
- Throws `InvalidOperationException` when `AppSettings` or `MigrationParserService` is not registered.
- Returns a failed result (exit code `1`) when any file has validation errors, and a failed result with the default error exit code when the migrations directory is missing.
- Returns a successful result when all files are valid, or when no migration files exist.

## Exit codes

| Outcome | `CommandResult.Success` | `ExitCode` |
|---|---|---|
| All files valid | `true` | `Constants.ExitCodes.NoDiff` (`0`) |
| No migration files | `true` | `Constants.ExitCodes.NoDiff` (`0`) |
| One or more invalid files | `false` | `1` |
| Migrations directory not found | `false` | `Constants.ExitCodes.Error` (default) |

## Usage notes

- The validation is a structural check of the source text. `ValidateMigrationFile` looks for the `public partial class` declaration and other heuristics. It does not compile the migrations.
- `ValidateCommand` is an `ICommand` and can be registered with `CommandExecutor.RegisterCommand`. In the current `Program.cs`, the `validate` and `check` commands are dispatched to a private `ValidateCommand` method in `Program`. That method duplicates the logic above with console output instead of `CommandContext`, and it does not use this class.

## Example

Given a repository with these migration files in the migrations directory:

```
20240101000000_InitialCreate.cs            (valid)
20240115000000_AddOrdersTable.cs           (missing Down method)
20240115000000_AddOrdersTable.Designer.cs  (skipped)
```

Running `dotnet run -- validate` from the repository root prints output in this shape:

```
Validating migrations in: /path/to/repo/<migrations path>
Found 2 migration file(s)

  ✓ 20240101000000_InitialCreate.cs
  ✗ 20240115000000_AddOrdersTable.cs
    - <error reported by MigrationParserService>

────────────────────────────────────────
Valid: 1 | Invalid: 1
```

The process then exits with code `1` and the message `Validation failed: 1 invalid file(s)`. If every file is valid, the output ends with `Valid: 2 | Invalid: 0` and the message `All 2 migration(s) validated successfully`.

Calling the class directly from code requires a `CommandContext` whose `ServiceProvider` contains `AppSettings` and `MigrationParserService`:

```csharp
var command = new ValidateCommand();
var result = await command.ExecuteAsync(context);

if (!result.Success)
{
    Console.Error.WriteLine(result.Message);
}

return result.ExitCode;
```
