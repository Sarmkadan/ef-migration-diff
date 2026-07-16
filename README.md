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
