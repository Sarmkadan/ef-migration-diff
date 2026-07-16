// ... existing content ...

## ValidationHelper

The `ValidationHelper` class provides utility methods for validating and sanitizing various types of input data, ensuring that it conforms to expected formats and patterns. This includes checks for migration timestamps, IDs, table and column names, file paths, and more. It's useful for preprocessing and validating data before using it in the application.

Here's a realistic usage example based on the class's public members:

```csharp
using EfMigrationDiff.Utilities;

class Program
{
    static void Main()
    {
        // Validate a migration timestamp
        bool isValidTimestamp = ValidationHelper.IsValidMigrationTimestamp("20220101123456");
        Console.WriteLine(isValidTimestamp); // Output: True

        // Validate a migration ID
        bool isValidMigrationId = ValidationHelper.IsValidMigrationId("20220101123456");
        Console.WriteLine(isValidMigrationId); // Output: True

        // Validate a table name
        bool isValidTableName = ValidationHelper.IsValidTableName("[MyTable]");
        Console.WriteLine(isValidTableName); // Output: True

        // Sanitize input to prevent SQL injection
        string sanitizedInput = ValidationHelper.SanitizeInput("SELECT * FROM users");
        Console.WriteLine(sanitizedInput); // Output: 

        // Validate an email address
        bool isValidEmail = ValidationHelper.IsValidEmail("user@example.com");
        Console.WriteLine(isValidEmail); // Output: True

        // Check if a string is alphanumeric
        bool isAlphanumeric = ValidationHelper.IsAlphanumeric("HelloWorld123");
        Console.WriteLine(isAlphanumeric); // Output: True
    }
}
```
// ... rest of file content ...
