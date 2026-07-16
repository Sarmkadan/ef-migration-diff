// ... existing content ...

## ValidationMiddleware

The `ValidationMiddleware` class validates command context before execution, checking for required options, valid argument counts, and application configuration state. It allows registering validators for specific commands and short-circuits command execution if validation fails.

Here's an example of how to use the `ValidationMiddleware` class:

```csharp
var middleware = new ValidationMiddleware();
middleware.RegisterValidator("myCommand", new CommandValidator()
  .RequireMinArguments(2)
  .RequireOption("--option")
  .ValidateOptionValue("--option", errorMessage: "Option value is required")
);

var context = new CommandContext("myCommand", new[] { "arg1", "arg2" }, new Dictionary<string, object> { { "--option", "optionValue" } });
var result = await middleware.InvokeAsync(context);

// Use AddRule for custom validation
middleware.RegisterValidator("myCustomCommand", new CommandValidator()
  .AddRule(ctx => ctx.ParsedArguments.Count > 0 ? null : "At least one argument is required")
);
```

The `ValidationMiddleware` uses validators to perform the actual validation. Validators can be created using the `CommandValidator` class which provides methods like `RequireMinArguments`, `RequireOption`, `ValidateOptionValue`, and `AddRule` to define validation rules.

## CommandParser

`CommandParser` converts raw command‑line arguments into a `CommandContext`. It lets you register known options (short and long names, descriptions, and whether they are flags) and later retrieve the defined options for help generation.

```csharp
using EfMigrationDiff.CLI;
using Microsoft.Extensions.DependencyInjection;

// Create and configure the parser
var parser = new CommandParser()
  .RegisterOption("f", "force", "Force the operation", isFlag: true)
  .RegisterOption("o", "output", "Path to the output file");

// Parse a sample argument list
var context = parser.Parse(
  commandName: "migrate",
  args: new[] { "--force", "-o", "result.txt", "src/db" },
  serviceProvider: new ServiceCollection().BuildServiceProvider());

// Inspect parsed arguments and options
Console.WriteLine($"Positional arguments: {context.ParsedArguments.Count}");
foreach (var opt in context.ParsedOptions)
{
  Console.WriteLine($"{opt.Key} = {opt.Value}");
}

// List the registered option definitions (useful for help text)
foreach (var optDef in parser.GetRegisteredOptions())
{
  Console.WriteLine($"{optDef.ShortName}/--{optDef.LongName}: {optDef.Description} (Flag: {optDef.IsFlag})");
}
```

The parser's option definitions expose `ShortName`, `LongName`, `Description`, and `IsFlag` properties, allowing callers to generate user‑friendly documentation or perform additional validation.




## CommandContext

`CommandContext` represents the execution context for a CLI command, providing access to parsed arguments, options, services, and I/O streams. It serves as the central data structure passed through command execution pipelines, middleware, and validators, enabling consistent argument handling and dependency injection across the application.

Here's a realistic example of creating and using a `CommandContext`:

```csharp
using EfMigrationDiff.CLI;
using Microsoft.Extensions.DependencyInjection;
using System;

// Create a service provider with required services
var services = new ServiceCollection()
    .AddLogging()
    .AddSingleton<IMyService, MyService>()
    .BuildServiceProvider();

// Create a command context with parsed arguments and options
var context = new CommandContext(
    commandName: "migrate",
    rawArguments: new[] { "--target", "v2.0.0", "--force", "src/Migrations" },
    parsedOptions: new Dictionary<string, string>
    {
        { "--target", "v2.0.0" },
        { "--force", "true" }
    },
    parsedArguments: new List<string> { "src/Migrations" },
    serviceProvider: services,
    output: Console.Out,
    errorOutput: Console.Error,
    cancellationToken: CancellationToken.None
);

// Access context properties
Console.WriteLine($"Command: {context.CommandName}");
Console.WriteLine($"Raw arguments: {string.Join(" ", context.RawArguments)}");
Console.WriteLine($"Target version: {context.GetOption("--target")}");
Console.WriteLine($"Force flag: {context.HasOption("--force")}");
Console.WriteLine($"Migration path: {context.ParsedArguments[0]}");

// Write to output streams
context.WriteOutput("Starting migration...");
context.WriteError("Warning: Database connection may be slow");

// Use metadata for command-specific data
context.SetMetadata("MigrationId", Guid.NewGuid());
if (context.TryGetMetadata<Guid>(out var migrationId))
{
    context.WriteOutput($"Migration ID: {migrationId}");
}
```

`CommandContext` is designed to be passed through middleware chains and command executors, providing a consistent interface for argument parsing, service resolution, and output handling throughout the CLI application.



## CommandExecutor

`CommandExecutor` provides a fluent API for registering and executing CLI commands with middleware support. It maintains a collection of registered commands and middleware components, executes commands asynchronously, and provides detailed execution results including success status, exit codes, and custom data payloads.

```csharp
using EfMigrationDiff.CLI;

// Create a command executor
var executor = new CommandExecutor()
  .RegisterCommand("migrate", "Migrate database to latest version")
  .RegisterCommand("rollback", "Rollback database to specified version")
  .RegisterMiddleware(async (context, next) => {
    Console.WriteLine($"Executing middleware for command: {context.CommandName}");
    return await next();
  });

// Execute a command asynchronously
var result = await executor.ExecuteAsync("migrate", new[] { "--target", "v2.0.0", "--force" });

// Check execution result
if (result.Success)
{
  Console.WriteLine($"Command succeeded with exit code: {result.ExitCode}");
  Console.WriteLine($"Message: {result.Message}");
  Console.WriteLine($"Data: {result.Data}");
}
else
{
  Console.WriteLine($"Command failed: {result.Message}");
}

// Get information about registered commands
Console.WriteLine($"Registered commands: {executor.GetRegisteredCommandCount()}");
foreach (var cmdName in executor.GetRegisteredCommandNames())
{
  Console.WriteLine($"- {cmdName}");
}

// Use static factory methods for quick results
var okResult = CommandExecutor.Ok("Operation completed successfully", 0);
var errorResult = CommandExecutor.Error("Invalid arguments provided", 1);

// Check if execution was short-circuited by middleware
if (executor.IsShortCircuited)
{
  Console.WriteLine($"Execution short-circuited with result: {executor.Result?.Message}");
}
```

The `CommandExecutor` supports middleware chaining, command registration, and provides detailed execution feedback through the `CommandResult` type which includes properties like `Success`, `Message`, `ExitCode`, `Data`, and `IsShortCircuited`.
