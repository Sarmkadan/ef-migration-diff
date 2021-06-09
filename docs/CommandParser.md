# CommandParser

Represents a command-line command definition within the `ef-migration-diff` tool. It encapsulates the command's identity (short name, long name, description, and whether it is a flag) and provides a fluent interface for registering sub‑options (`CommandOptionDefinition` instances). The `Parse` method processes raw command-line arguments against the registered options and returns a `CommandContext` containing the parsed values. Equality is based on the command's identity properties.

## API

### `public CommandParser()`
Initializes a new instance of `CommandParser` with default values. The command's `ShortName`, `LongName`, `Description`, and `IsFlag` are set to their default (empty or `false`).

### `public CommandParser RegisterOption(CommandOptionDefinition option)`
Registers a sub‑option definition with this command.  
**Parameters:**  
- `option` – The `CommandOptionDefinition` to register.  

**Returns:** The same `CommandParser` instance to enable fluent chaining.  

**Throws:**  
- `ArgumentNullException` if `option` is `null`.  
- `InvalidOperationException` if an option with the same `ShortName` or `LongName` is already registered.

### `public CommandContext Parse(string[] args)`
Parses the provided command-line arguments against the registered options and the command's own definition.  
**Parameters:**  
- `args` – The array of argument strings to parse.  

**Returns:** A `CommandContext` containing the parsed values, including any flags, values, and the matched command identity.  

**Throws:**  
- `ArgumentNullException` if `args` is `null`.  
- `CommandParseException` (or a derived exception) if the arguments are malformed, contain unrecognized options, or violate required‑option constraints.

### `public IEnumerable<CommandOptionDefinition> GetRegisteredOptions()`
Returns a read‑only collection of all sub‑option definitions that have been registered via `RegisterOption`.  
**Returns:** An `IEnumerable<CommandOptionDefinition>` representing the registered options. The collection is a snapshot at the time of the call.

### `public bool Equals(object obj)`
Determines whether the specified object is equal to the current `CommandParser`. Two instances are considered equal if they have the same `ShortName` and `LongName` (case‑insensitive).  
**Returns:** `true` if the objects are equal; otherwise `false`.

### `public int GetHashCode()`
Returns a hash code for this instance, computed from the `ShortName` and `LongName` properties.  
**Returns:** A 32‑bit signed integer hash code.

### `public string ShortName { get; set; }`
Gets or sets the short (single‑character) name of the command, e.g. `"d"`. Default is `String.Empty`.

### `public string LongName { get; set; }`
Gets or sets the long (multi‑character) name of the command, e.g. `"diff"`. Default is `String.Empty`.

### `public string Description { get; set; }`
Gets or sets a human‑readable description of the command, used in help text. Default is `String.Empty`.

### `public bool IsFlag { get; set; }`
Gets or sets whether the command itself is a flag (i.e., it does not accept a value). When `true`, the command's presence in the argument list is treated as a boolean switch. Default is `false`.

## Usage

### Example 1: Defining a command with a sub‑option and parsing arguments

```csharp
var diffCommand = new CommandParser
{
    ShortName = "d",
    LongName = "diff",
    Description = "Show differences between two migrations",
    IsFlag = false
};

diffCommand.RegisterOption(new CommandOptionDefinition
{
    ShortName = "s",
    LongName = "source",
    Description = "Source migration name",
    IsFlag = false
});

string[] args = { "--diff", "--source", "InitialCreate" };
CommandContext context = diffCommand.Parse(args);

Console.WriteLine(context.Command);          // "diff"
Console.WriteLine(context.GetValue("source")); // "InitialCreate"
```

### Example 2: Using a flag command and checking equality

```csharp
var helpCommand = new CommandParser
{
    ShortName = "h",
    LongName = "help",
    Description = "Display help information",
    IsFlag = true
};

var anotherHelp = new CommandParser
{
    ShortName = "h",
    LongName = "help",
    Description = "Show help",
    IsFlag = true
};

Console.WriteLine(helpCommand.Equals(anotherHelp)); // True (same ShortName and LongName)

string[] args = { "--help" };
CommandContext context = helpCommand.Parse(args);
Console.WriteLine(context.IsFlagPresent); // True
```

## Notes

- **Edge cases:**  
  - If `ShortName` and `LongName` are both empty, the command is considered anonymous and equality will compare all instances with empty names as equal.  
  - Setting `IsFlag` to `true` does not prevent the command from having sub‑options; the flag status applies only to the command itself.  
  - `Parse` will throw if `args` contains the command name but the command is defined as a flag and a value is provided after it.

- **Thread safety:**  
  Instances of `CommandParser` are not thread‑safe for mutation. Concurrent calls to `RegisterOption` or property setters from multiple threads may produce inconsistent state. After configuration is complete, `Parse` and `GetRegisteredOptions` can be called from multiple threads safely as long as no further mutations occur. The `Equals` and `GetHashCode` methods are safe for concurrent read‑only access.
