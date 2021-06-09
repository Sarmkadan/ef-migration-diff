# ValidationMiddleware

A middleware component for validating commands in the `ef-migration-diff` project. It provides a fluent interface for defining validation rules and executing them against command arguments and options, collecting errors for reporting or further processing.

## API

### `public ValidationMiddleware`

Initializes a new instance of the `ValidationMiddleware` class.

### `public ValidationMiddleware RegisterValidator(CommandValidator validator)`

Registers a command validator with the middleware.

- **Parameters**
  - `validator` (CommandValidator): The validator to register.
- **Return Value**
  - Returns the current `ValidationMiddleware` instance for method chaining.
- **Throws**
  - `ArgumentNullException`: If `validator` is `null`.

### `public async Task<MiddlewareResult> InvokeAsync(...)`

Executes the validation pipeline for the current command context.

- **Parameters**
  - Implicitly includes command context (arguments, options, etc.) from the middleware pipeline.
- **Return Value**
  - Returns a `MiddlewareResult` indicating success or failure, including validation errors if any.
- **Throws**
  - `InvalidOperationException`: If no validators have been registered.

### `public CommandValidator AddRule(string ruleName, Func<bool> predicate)`

Adds a custom validation rule to the current command validator.

- **Parameters**
  - `ruleName` (string): A descriptive name for the rule.
  - `predicate` (Func<bool>): A function that returns `true` if the rule passes.
- **Return Value**
  - Returns the current `CommandValidator` instance for method chaining.
- **Throws**
  - `ArgumentNullException`: If `ruleName` or `predicate` is `null`.
  - `ArgumentException`: If `ruleName` is empty or whitespace.

### `public CommandValidator RequireMinArguments(int minCount)`

Adds a rule requiring a minimum number of command arguments.

- **Parameters**
  - `minCount` (int): The minimum number of arguments required.
- **Return Value**
  - Returns the current `CommandValidator` instance for method chaining.
- **Throws**
  - `ArgumentOutOfRangeException`: If `minCount` is negative.

### `public CommandValidator RequireOption(string optionName)`

Adds a rule requiring a specific option to be present.

- **Parameters**
  - `optionName` (string): The name of the required option.
- **Return Value**
  - Returns the current `CommandValidator` instance for method chaining.
- **Throws**
  - `ArgumentNullException`: If `optionName` is `null`.
  - `ArgumentException`: If `optionName` is empty or whitespace.

### `public CommandValidator ValidateOptionValue(string optionName, Func<string, bool> predicate)`

Adds a rule validating the value of a specific option.

- **Parameters**
  - `optionName` (string): The name of the option to validate.
  - `predicate` (Func<string, bool>): A function that validates the option value.
- **Return Value**
  - Returns the current `CommandValidator` instance for method chaining.
- **Throws**
  - `ArgumentNullException`: If `optionName` or `predicate` is `null`.
  - `ArgumentException`: If `optionName` is empty or whitespace.

### `public ValidationResult Validate(...)`

Executes all registered validation rules and returns the result.

- **Parameters**
  - Implicitly includes command context (arguments, options, etc.).
- **Return Value**
  - Returns a `ValidationResult` containing validation outcome and errors.
- **Throws**
  - `InvalidOperationException`: If no validators have been registered.

### `public bool IsValid`

Gets a value indicating whether the last validation operation succeeded.

- **Return Value**
  - `true` if the last validation succeeded; otherwise, `false`.

### `public List<string> Errors`

Gets the list of validation error messages collected during the last validation.

- **Return Value**
  - A `List<string>` containing error messages. Empty if no errors occurred.

## Usage

### Example 1: Basic Validation
