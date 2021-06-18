# CsvFormatter

A utility class for converting collections of objects into CSV-formatted strings and writing them to files. It supports generic type parameters to serialize any object with public properties as CSV rows, handling basic escaping and delimiter placement automatically.

## API

### `public CsvFormatter`

Initializes a new instance of the `CsvFormatter` class. No configuration is required; the formatter uses default settings for delimiter (`,`) and quote character (`"`).

### `public string Format<T>(IEnumerable<T> items)`

Serializes a sequence of objects into a single CSV-formatted string.

- **Parameters**
  - `items`: An `IEnumerable<T>` containing the objects to serialize. Must not be `null`.

- **Return Value**
  Returns a `string` representing the CSV content. Each object in `items` becomes a row in the output.

- **Exceptions**
  - Throws `ArgumentNullException` if `items` is `null`.

### `public string FormatRow<T>(T item)`

Serializes a single object into a CSV-formatted string representing one row.

- **Parameters**
  - `item`: The object to serialize. Must not be `null`.

- **Return Value**
  Returns a `string` representing the CSV row for the given object.

- **Exceptions**
  - Throws `ArgumentNullException` if `item` is `null`.

### `public void WriteToFile<T>(IEnumerable<T> items, string filePath)`

Serializes a sequence of objects and writes the result to a file at the specified path.

- **Parameters**
  - `items`: An `IEnumerable<T>` containing the objects to serialize. Must not be `null`.
  - `filePath`: The full path to the output file. Must not be `null` or empty.

- **Exceptions**
  - Throws `ArgumentNullException` if `items` or `filePath` is `null`.
  - Throws `ArgumentException` if `filePath` is empty or whitespace.
  - Throws `IOException` or derived exceptions if file operations fail (e.g., invalid path, permissions).

## Usage
