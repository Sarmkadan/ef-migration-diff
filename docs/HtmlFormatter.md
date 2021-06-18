# HtmlFormatter

The `HtmlFormatter` class provides a set of utility methods for generating HTML fragments and documents, specifically designed for formatting migration diffs in the `ef-migration-diff` project. It handles HTML encoding, structural elements (headings, paragraphs, alerts), and table generation, allowing callers to produce consistent, safe HTML output without manually concatenating markup.

## API

### `CreateDocument`

```csharp
public string CreateDocument(string title, string bodyContent)
```

**Purpose**: Wraps the provided body content into a complete HTML document with a `<head>` section containing the given title and a `<body>` section.

**Parameters**:
- `title` – The text to place inside the `<title>` element. Must not be `null`.
- `bodyContent` – The HTML string to insert into the `<body>` element. Must not be `null`.

**Returns**: A string containing a full HTML document (including `<!DOCTYPE html>`, `<html>`, `<head>`, and `<body>` tags).

**Throws**:
- `ArgumentNullException` if `title` or `bodyContent` is `null`.

### `GenerateTable<T>`

```csharp
public string GenerateTable<T>(IEnumerable<T> items, params Func<T, string>[] columnSelectors)
```

**Purpose**: Generates an HTML `<table>` from a sequence of items. Each `Func<T, string>` in `columnSelectors` defines a column by extracting a string value from an item. The table includes a header row with default column names (e.g., "Column 1", "Column 2") and one data row per item.

**Parameters**:
- `items` – The collection of items to render as rows. Must not be `null`.
- `columnSelectors` – One or more functions that project an item to its string representation for that column. Must contain at least one selector.

**Returns**: A string containing a `<table>` element with `<thead>` and `<tbody>` sections.

**Throws**:
- `ArgumentNullException` if `items` is `null`.
- `ArgumentException` if `columnSelectors` is empty or `null`.

### `CreateHeading`

```csharp
public string CreateHeading(string text, int level = 1)
```

**Purpose**: Creates an HTML heading element (`<h1>` through `<h6>`) with the specified text.

**Parameters**:
- `text` – The heading text. Must not be `null`.
- `level` – The heading level (1–6). Defaults to 1.

**Returns**: A string containing the heading element, e.g., `<h1>text</h1>`.

**Throws**:
- `ArgumentNullException` if `text` is `null`.
- `ArgumentOutOfRangeException` if `level` is less than 1 or greater than 6.

### `CreateParagraph`

```csharp
public string CreateParagraph(string text)
```

**Purpose**: Creates an HTML `<p>` element containing the given text.

**Parameters**:
- `text` – The paragraph text. Must not be `null`.

**Returns**: A string containing `<p>text</p>`.

**Throws**:
- `ArgumentNullException` if `text` is `null`.

### `CreateAlert`

```csharp
public string CreateAlert(string message, AlertType type)
```

**Purpose**: Creates an HTML `<div>` element styled as an alert (e.g., success, warning, error). The `AlertType` enum defines the CSS class applied (e.g., `alert-success`, `alert-warning`, `alert-danger`).

**Parameters**:
- `message` – The alert message text. Must not be `null`.
- `type` – A value from the `AlertType` enum indicating the severity or style.

**Returns**: A string containing a `<div class="alert alert-{type}">` element.

**Throws**:
- `ArgumentNullException` if `message` is `null`.
- `ArgumentException` if `type` is not a defined `AlertType` value.

### `HtmlEncode`

```csharp
public string HtmlEncode(string value)
```

**Purpose**: Encodes a plain-text string so that it is safe for inclusion in HTML content. Replaces characters like `<`, `>`, `&`, `"`, and `'` with their corresponding HTML entities.

**Parameters**:
- `value` – The string to encode. May be `null`.

**Returns**: The HTML-encoded string. If `value` is `null`, returns an empty string.

**Throws**: None.

## Usage

### Example 1: Generating a migration diff report

```csharp
var formatter = new HtmlFormatter();

string heading = formatter.CreateHeading("Migration Diff Report", 2);
string summary = formatter.CreateParagraph("The following changes were detected:");

var changes = new[]
{
    new { Table = "Users", Action = "Add column" },
    new { Table = "Orders", Action = "Drop column" }
};

string table = formatter.GenerateTable(changes,
    item => formatter.HtmlEncode(item.Table),
    item => formatter.HtmlEncode(item.Action));

string alert = formatter.CreateAlert("Review these changes before applying.", AlertType.Warning);

string body = heading + summary + table + alert;
string document = formatter.CreateDocument("Migration Diff", body);

Console.WriteLine(document);
```

### Example 2: Encoding user-provided input for safe display

```csharp
var formatter = new HtmlFormatter();

string userInput = "<script>alert('xss')</script>";
string safeInput = formatter.HtmlEncode(userInput);

string paragraph = formatter.CreateParagraph("User comment: " + safeInput);
string heading = formatter.CreateHeading("Comments", 3);

string output = heading + paragraph;
Console.WriteLine(output);
// Output: <h3>Comments</h3><p>User comment: &lt;script&gt;alert(&#39;xss&#39;)&lt;/script&gt;</p>
```

## Notes

- All methods that accept a `string` parameter throw `ArgumentNullException` when that parameter is `null`, except `HtmlEncode`, which treats `null` as an empty string.
- `GenerateTable<T>` requires at least one column selector; passing an empty array or `null` as `columnSelectors` throws `ArgumentException`.
- `CreateHeading` validates the `level` parameter and throws `ArgumentOutOfRangeException` for values outside 1–6.
- `CreateAlert` expects a valid `AlertType` enum value; undefined values cause an `ArgumentException`.
- The class is stateless: no instance fields are modified by any public method. Therefore, all members are thread-safe as long as the input parameters are not shared mutable state.
- HTML output from `CreateDocument`, `GenerateTable`, `CreateHeading`, `CreateParagraph`, and `CreateAlert` is not automatically encoded. Callers should use `HtmlEncode` on any user-supplied or untrusted text before passing it to these methods to prevent XSS vulnerabilities.
- The `AlertType` enum is assumed to be defined elsewhere in the project (e.g., `public enum AlertType { Success, Warning, Danger }`).
