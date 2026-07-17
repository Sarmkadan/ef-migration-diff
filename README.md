// ... existing content ...

## JsonFormatter

The `JsonFormatter` class provides a set of methods for serializing and deserializing objects to and from JSON format. It allows for customizable serialization options, such as pretty-printing and null handling.

Here's a realistic usage example based on the class's public members:

```csharp
using EfMigrationDiff.Formatters;

class Program
{
    static void Main()
    {
        // Create a new JSON formatter with pretty-printing enabled
        var jsonFormatter = new JsonFormatter(true);

        // Serialize an object to JSON
        var person = new { Name = "John", Age = 30 };
        var json = jsonFormatter.Format(person);
        System.Console.WriteLine(json);

        // Deserialize JSON to an object
        var deserializedPerson = jsonFormatter.Deserialize<person>(json);
        System.Console.WriteLine(deserializedPerson.Name);

        // Write JSON to a file
        jsonFormatter.WriteToFile("person.json", person);

        // Read JSON from a file
        var deserializedPersonFromFile = jsonFormatter.ReadFromFile<person>("person.json");
        System.Console.WriteLine(deserializedPersonFromFile.Name);
    }
}
```

// ... rest of file content ...

## MigrationParserServiceValidation

The `MigrationParserServiceValidation` class provides a comprehensive set of validation extension methods for `MigrationParserService` that ensure migration files and their contents are properly structured and valid before parsing and processing. It validates file existence, structure, naming conventions, timestamps, and content integrity to prevent runtime errors during migration comparison and analysis.

Here's a realistic usage example based on the class's public members:


```csharp
using EfMigrationDiff.Models;
using EfMigrationDiff.Services;

class Program
{
    static void Main()
    {
        var parserService = new MigrationParserService();
        
        // Validate service instance itself
        var serviceErrors = parserService.Validate();
        if (serviceErrors.Count > 0)
        {
            Console.WriteLine("Service validation failed:");
            foreach (var error in serviceErrors)
            {
                Console.WriteLine($"- {error}");
            }
        }
        
        // Validate a migration file
        var migrationFile = new MigrationFile
        {
            FilePath = "/path/to/20240615123045_AddUsersTable.cs",
            FileName = "20240615123045_AddUsersTable.cs",
            DbContextName = "ApplicationDbContext",
            FileSize = 2048,
            LastModified = DateTime.Now,
            Content = "public partial class AddUsersTable : Migration { ... }"
        };
        
        var fileErrors = parserService.Validate(migrationFile);
        if (fileErrors.Count > 0)
        {
            Console.WriteLine("Migration file validation failed:");
            foreach (var error in fileErrors)
            {
                Console.WriteLine($"- {error}");
            }
        }
        
        // Validate migration file content
        var contentErrors = parserService.ValidateMigrationFile(migrationFile);
        if (contentErrors.Count > 0)
        {
            Console.WriteLine("Migration content validation failed:");
            foreach (var error in contentErrors)
            {
                Console.WriteLine($"- {error}");
            }
        }
        
        // Validate a migration object
        var migration = new Migration
        {
            Id = "20240615123045",
            Name = "AddUsersTable",
            DbContextName = "ApplicationDbContext",
            CreatedAt = DateTime.Now,
            Content = "public partial class AddUsersTable : Migration { ... }",
            Sequence = 42
        };
        
        var migrationErrors = parserService.Validate(migration);
        if (migrationErrors.Count > 0)
        {
            Console.WriteLine("Migration validation failed:");
            foreach (var error in migrationErrors)
            {
                Console.WriteLine($"- {error}");
            }
        }
        
        // Use EnsureValid to throw on validation failure
        try
        {
            parserService.EnsureValid();
            parserService.EnsureValid(migrationFile);
            Console.WriteLine("All validations passed!");
        }
        catch (ArgumentException ex)
        {
            Console.WriteLine($"Validation error: {ex.Message}");
        }
    }
}
```
