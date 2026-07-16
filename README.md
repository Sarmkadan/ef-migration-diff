// ... existing content ...

## HtmlFormatter

The `HtmlFormatter` class provides a set of methods for generating styled HTML documents and tables. It allows for customizable styling, tables, and structured document generation.

Here's a realistic usage example based on the class's public members:

```csharp
using EfMigrationDiff.Formatters;

class Program
{
    static void Main()
    {
        // Create a new HTML document with a title and body content
        var htmlFormatter = new HtmlFormatter();
        var document = htmlFormatter.CreateDocument("My Document", "This is the body content.");
        System.Console.WriteLine(document);

        // Generate an HTML table from a collection of objects
        var items = new[] { new { Name = "John", Age = 30 }, new { Name = "Jane", Age = 25 } };
        var table = htmlFormatter.GenerateTable(items);
        System.Console.WriteLine(table);

        // Create a heading element
        var heading = htmlFormatter.CreateHeading("My Heading", 2);
        System.Console.WriteLine(heading);

        // Create a paragraph element
        var paragraph = htmlFormatter.CreateParagraph("This is a paragraph of text.");
        System.Console.WriteLine(paragraph);

        // Create an alert/notification box
        var alert = htmlFormatter.CreateAlert("This is an alert message.");
        System.Console.WriteLine(alert);

        // HTML-encode a string to prevent XSS
        var encodedString = htmlFormatter.HtmlEncode("<script>alert('XSS')</script>");
        System.Console.WriteLine(encodedString);
    }
}
```

// ... rest of file content ...
