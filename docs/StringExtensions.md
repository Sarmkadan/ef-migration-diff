# StringExtensions

A utility class providing common string manipulation and formatting extensions for use in Entity Framework migrations and other .NET string handling scenarios.

## API

### `IsNullOrEmpty`
Determines whether the specified string is `null` or an empty string.
- **Parameters**: `string? value` – the string to check.
- **Returns**: `bool` – `true` if the value is `null` or `""`; otherwise, `false`.
- **Exceptions**: None.

### `IsNullOrWhiteSpace`
Determines whether the specified string is `null`, empty, or consists only of white-space characters.
- **Parameters**: `string? value` – the string to check.
- **Returns**: `bool` – `true` if the value is `null`, `""`, or whitespace-only; otherwise, `false`.
- **Exceptions**: None.

### `OrEmpty`
Returns the input string if it is not `null`; otherwise, returns an empty string.
- **Parameters**: `string? value` – the string to evaluate.
- **Returns**: `string` – the original string or `""` if `null`.
- **Exceptions**: None.

### `Or`
Returns the input string if it is not `null` or whitespace; otherwise, returns the provided fallback string.
- **Parameters**:
  - `string? value` – the string to evaluate.
  - `string fallback` – the string to return if `value` is `null` or whitespace.
- **Returns**: `string` – the original string if valid; otherwise, `fallback`.
- **Exceptions**: Throws `ArgumentNullException` if `fallback` is `null`.

### `EnsureEndsWith`
Ensures the string ends with the specified suffix, appending it if missing.
- **Parameters**:
  - `string? value` – the string to process.
  - `string suffix` – the suffix to ensure.
- **Returns**: `string` – the original string if it already ends with `suffix`; otherwise, the concatenation of `value` and `suffix`.
- **Exceptions**: Throws `ArgumentNullException` if `suffix` is `null`.

### `EnsureStartsWith`
Ensures the string starts with the specified prefix, prepending it if missing.
- **Parameters**:
  - `string? value` – the string to process.
  - `string prefix` – the prefix to ensure.
- **Returns**: `string` – the original string if it already starts with `prefix`; otherwise, the concatenation of `prefix` and `value`.
- **Exceptions**: Throws `ArgumentNullException` if `prefix` is `null`.

### `RemovePrefix`
Removes the specified prefix from the string if it exists at the start.
- **Parameters**:
  - `string? value` – the string to process.
  - `string prefix` – the prefix to remove.
- **Returns**: `string` – the original string if it does not start with `prefix`; otherwise, the string without the prefix.
- **Exceptions**: Throws `ArgumentNullException` if `prefix` is `null`.

### `RemoveSuffix`
Removes the specified suffix from the string if it exists at the end.
- **Parameters**:
  - `string? value` – the string to process.
  - `string suffix` – the suffix to remove.
- **Returns**: `string` – the original string if it does not end with `suffix`; otherwise, the string without the suffix.
- **Exceptions**: Throws `ArgumentNullException` if `suffix` is `null`.

### `ToPascalCase`
Converts the string to PascalCase, capitalizing the first character and any character following a non-letter or digit.
- **Parameters**: `string? value` – the string to convert.
- **Returns**: `string` – the PascalCase version of the input, or `""` if `null` or empty.
- **Exceptions**: None.

### `ToCamelCase`
Converts the string to camelCase, capitalizing the first character if it is a letter, and any character following a non-letter or digit.
- **Parameters**: `string? value` – the string to convert.
- **Returns**: `string` – the camelCase version of the input, or `""` if `null` or empty.
- **Exceptions**: None.

### `ToSnakeCase`
Converts the string to snake_case, inserting underscores between words and converting to lowercase.
- **Parameters**: `string? value` – the string to convert.
- **Returns**: `string` – the snake_case version of the input, or `""` if `null` or empty.
- **Exceptions**: None.

### `ToKebabCase`
Converts the string to kebab-case, inserting hyphens between words and converting to lowercase.
- **Parameters**: `string? value` – the string to convert.
- **Returns**: `string` – the kebab-case version of the input, or `""` if `null` or empty.
- **Exceptions**: None.

### `Truncate`
Truncates the string to the specified maximum length, optionally appending a suffix if truncated.
- **Parameters**:
  - `string? value` – the string to truncate.
  - `int maxLength` – the maximum allowed length.
  - `string? suffix` – optional suffix to append if truncation occurs (default: `null`).
- **Returns**: `string` – the truncated string, or the original if within `maxLength`; if truncated and `suffix` is provided, the truncated string plus `suffix`.
- **Exceptions**: Throws `ArgumentOutOfRangeException` if `maxLength` is negative.

### `Repeat`
Repeats the string the specified number of times.
- **Parameters**:
  - `string? value` – the string to repeat.
  - `int count` – the number of repetitions.
- **Returns**: `string` – the repeated string, or `""` if `value` is `null` or `count` is zero.
- **Exceptions**: Throws `ArgumentOutOfRangeException` if `count` is negative.

### `CountOccurrences`
Counts the number of occurrences of a substring within the string.
- **Parameters**:
  - `string? value` – the string to search.
  - `string substring` – the substring to count.
- **Returns**: `int` – the number of times `substring` appears in `value`.
- **Exceptions**: Throws `ArgumentNullException` if `substring` is `null`.

## Usage
