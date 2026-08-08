#nullable enable
namespace EfMigrationDiff.Formatters;

/// <summary>
/// HTML formatter for generating styled HTML reports and tables.
/// Supports customizable styling, tables, and structured document generation.
/// </summary>
public class HtmlFormatter
{
    private const string DocTypeHtml5 = "<!DOCTYPE html>";

    /// <summary>
    /// Gets the document type declaration used for HTML documents.
    /// </summary>
    public string DocumentType { get; } = DocTypeHtml5;

    /// <summary>
    /// Gets the default language for HTML documents.
    /// </summary>
    public string Language { get; } = "en";

    /// <summary>
    /// Gets the default CSS styles for HTML output.
    /// </summary>
    public string DefaultStyles { get; }

    /// <summary>
    /// Initializes a new instance of the <see cref="HtmlFormatter"/> class.
    /// </summary>
    public HtmlFormatter()
    {
        DefaultStyles = GetDefaultStyles();
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="HtmlFormatter"/> class with custom settings.
    /// </summary>
    /// <param name="language">The language code for the HTML document (e.g., "en", "ru").</param>
    /// <param name="customStyles">Custom CSS styles to include in the HTML output.</param>
    public HtmlFormatter(string language, string? customStyles = null)
    {
        ArgumentException.ThrowIfNullOrEmpty(language);
        Language = language;
        DefaultStyles = customStyles ?? GetDefaultStyles();
    }

    /// <summary>
    /// Generates a complete HTML document with title and body content.
    /// </summary>
    public string CreateDocument(string title, string bodyContent, string? customCss = null)
    {
        ArgumentException.ThrowIfNullOrEmpty(title);
        ArgumentException.ThrowIfNullOrEmpty(bodyContent);
        var html = new System.Text.StringBuilder();

        html.AppendLine(DocumentType);
        html.AppendLine($"<html lang=\"{Language}\">");
        html.AppendLine("<head>");
        html.AppendLine($"  <title>{HtmlEncode(title)}</title>");
        html.AppendLine("  <meta charset=\"UTF-8\">");
        html.AppendLine("  <meta name=\"viewport\" content=\"width=device-width, initial-scale=1.0\">");
        html.AppendLine("  <style>");
        html.AppendLine(GetDefaultStyles());

        if (!string.IsNullOrEmpty(customCss))
        {
            html.AppendLine(customCss);
        }

        html.AppendLine("  </style>");
        html.AppendLine("</head>");
        html.AppendLine("<body>");
        html.AppendLine(bodyContent);
        html.AppendLine("</body>");
        html.AppendLine("</html>");

        return html.ToString();
    }

    /// <summary>
    /// Generates an HTML table from a collection of objects.
    /// </summary>
    public string GenerateTable<T>(IEnumerable<T> items, string? tableClass = null) where T : class
    {
        ArgumentNullException.ThrowIfNull(items);
        var itemList = items.ToList();
        if (!itemList.Any())
            return "<p>No data to display</p>";

        var type = typeof(T);
        var properties = type.GetProperties(System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Instance);

        var html = new System.Text.StringBuilder();
        var cssClass = string.IsNullOrEmpty(tableClass) ? "data-table" : tableClass;

        html.AppendLine($"<table class=\"{cssClass}\">");
        html.AppendLine("  <thead>");
        html.AppendLine("    <tr>");

        foreach (var prop in properties)
        {
            html.AppendLine($"      <th>{HtmlEncode(prop.Name)}</th>");
        }

        html.AppendLine("    </tr>");
        html.AppendLine("  </thead>");
        html.AppendLine("  <tbody>");

        foreach (var item in itemList)
        {
            html.AppendLine("    <tr>");
            foreach (var prop in properties)
            {
                var value = prop.GetValue(item)?.ToString() ?? string.Empty;
                html.AppendLine($"      <td>{HtmlEncode(value)}</td>");
            }
            html.AppendLine("    </tr>");
        }

        html.AppendLine("  </tbody>");
        html.AppendLine("</table>");

        return html.ToString();
    }

    /// <summary>
    /// Creates a heading element.
    /// </summary>
    public string CreateHeading(string text, int level = 1)
    {
        if (level < 1 || level > 6)
            level = 1;
        return $"<h{level}>{HtmlEncode(text)}</h{level}>";
    }

    /// <summary>
    /// Creates a paragraph element.
    /// </summary>
    public string CreateParagraph(string text, string? cssClass = null)
    {
        var classAttr = string.IsNullOrEmpty(cssClass) ? string.Empty : $" class=\"{cssClass}\"";
        return $"<p{classAttr}>{HtmlEncode(text)}</p>";
    }

    /// <summary>
    /// Creates an alert/notification box.
    /// </summary>
    public string CreateAlert(string message, AlertType type = AlertType.Info)
    {
        var cssClass = type switch
        {
            AlertType.Success => "alert alert-success",
            AlertType.Warning => "alert alert-warning",
            AlertType.Error => "alert alert-danger",
            _ => "alert alert-info"
        };

        return $"<div class=\"{cssClass}\">{HtmlEncode(message)}</div>";
    }

    /// <summary>
    /// HTML-encodes a string to prevent XSS.
    /// </summary>
    public string HtmlEncode(string text)
    {
        if (string.IsNullOrEmpty(text))
            return text;

        return System.Net.WebUtility.HtmlEncode(text);
    }

    /// <summary>
    /// Gets default CSS styling for HTML output.
    /// </summary>
    private string GetDefaultStyles()
    {
        return @"
    body {
      font-family: 'Segoe UI', Tahoma, Geneva, Verdana, sans-serif;
      line-height: 1.6;
      color: #333;
      margin: 20px;
      background-color: #f5f5f5;
    }
    h1, h2, h3 {
      color: #2c3e50;
      border-bottom: 2px solid #3498db;
      padding-bottom: 10px;
    }
    table.data-table {
      width: 100%;
      border-collapse: collapse;
      background-color: white;
      box-shadow: 0 2px 4px rgba(0,0,0,0.1);
      margin: 20px 0;
    }
    table.data-table th {
      background-color: #3498db;
      color: white;
      padding: 12px;
      text-align: left;
      font-weight: bold;
    }
    table.data-table td {
      padding: 10px 12px;
      border-bottom: 1px solid #ddd;
    }
    table.data-table tr:hover {
      background-color: #f9f9f9;
    }
    .alert {
      padding: 15px;
      margin: 15px 0;
      border-radius: 4px;
    }
    .alert-success {
      background-color: #d4edda;
      color: #155724;
      border: 1px solid #c3e6cb;
    }
    .alert-warning {
      background-color: #fff3cd;
      color: #856404;
      border: 1px solid #ffeeba;
    }
    .alert-danger {
      background-color: #f8d7da;
      color: #721c24;
      border: 1px solid #f5c6cb;
    }
    .alert-info {
      background-color: #d1ecf1;
      color: #0c5460;
      border: 1px solid #bee5eb;
    }
";
    }
}

public enum AlertType
{
    Info,
    Success,
    Warning,
    Error
}
