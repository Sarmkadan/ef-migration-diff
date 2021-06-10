# CommandContext

`CommandContext` provides a container for command input and output state used during command execution. It encapsulates the parsed command name, raw and parsed arguments, options, metadata, and output streams, along with a service provider for dependency resolution and a cancellation token for cooperative cancellation.

## API

### `public string CommandName`
The name of the command being executed. This is the first token from the raw input and identifies which command handler should process the request.

### `public string[] RawArguments`
The complete set of unprocessed command-line arguments, including the command name and all options and positional arguments in their original string form. This array is never modified after construction.

### `public Dictionary<string, string> ParsedOptions`
A read-only dictionary mapping option names (without leading dashes) to their provided values. Options are parsed from the raw arguments and normalized to lowercase keys. If an option appears multiple times, the last value is retained.

### `public List<string> ParsedArguments`
A list of positional arguments in the order they appeared in the input, excluding options and their values. This list is populated after option parsing and reflects the remaining tokens.

### `public IServiceProvider ServiceProvider`
A dependency injection container used to resolve services required by command handlers. The provider is shared across the command execution context and supports scoped or transient services as configured.

### `public TextWriter Output`
A `TextWriter` stream where command output should be written. By default, this is `Console.Out`, but can be redirected for testing or logging purposes.

### `public TextWriter ErrorOutput`
A `TextWriter` stream where error output and diagnostics should be written. By default, this is `Console.Error`, but can be redirected to capture or suppress error messages.

### `public CancellationToken CancellationToken`
A token that signals if the command execution should be canceled. This is typically tied to a user interrupt (e.g., Ctrl+C) or a higher-level timeout mechanism.

### `public CommandContext`
Constructs a new `CommandContext` with the given raw command-line arguments. The constructor parses the first token as the command name and initializes the output, error output, service provider, and cancellation token to default values.

### `public void SetMetadata(object metadata)`
Stores arbitrary metadata associated with the command context. Metadata can be retrieved later using `GetMetadata`, `TryGetMetadata<T>`, or similar methods. Overwrites any existing metadata with the same type.

### `public object? GetMetadata(Type type)`
Retrieves metadata of the specified type from the context. Returns `null` if no metadata of that type exists. This method is non-generic and intended for internal or reflection-based use.

### `public bool TryGetMetadata<T>(out T? metadata)`
Attempts to retrieve metadata of type `T` from the context. Returns `true` if metadata exists and was successfully cast to `T`; otherwise, returns `false` and sets `metadata` to `default`.

### `public string? GetOption(string name)`
Gets the value of a parsed option by name. The name is case-insensitive. Returns `null` if the option was not provided.

### `public bool HasOption(string name)`
Determines whether an option with the given name was provided in the input. The name is case-insensitive.

### `public string GetOptionOrDefault(string name, string defaultValue = "")`
Gets the value of a parsed option by name, or returns the specified default value if the option was not provided. The name is case-insensitive.

### `public void WriteOutput(string? message)`
Writes a message to the standard output stream (`Output`). The message may be `null`, in which case nothing is written.

### `public void WriteError(string? message)`
Writes a message to the error output stream (`ErrorOutput`). The message may be `null`, in which case nothing is written.

### `public void WriteColoredOutput(string? message, ConsoleColor color)`
Writes a message to the standard output stream with the specified foreground color. The message may be `null`, in which case nothing is written. The color applies only to the written message and does not affect subsequent output.

## Usage
