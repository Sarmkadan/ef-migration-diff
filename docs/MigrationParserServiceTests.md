# MigrationParserServiceTests

The `MigrationParserServiceTests` class contains unit tests for the `MigrationParserService`, which is responsible for parsing Entity Framework Core migration files (`.cs` and `.Designer.cs` files) into structured `Migration` objects. Each test method validates a specific aspect of the parsing logic, including handling of valid files, designer files, invalid timestamps, empty content, various naming conventions, null inputs, and complex file content.

## API

All test methods are parameterless, return `void`, and are intended to be executed by a test framework (e.g., xUnit, NUnit). They do not accept arguments and do not return values; instead, they assert expected behavior using standard assertion libraries.

| Method | Purpose | Throws |
|--------|---------|--------|
| `ParseMigrationFile_WithValidMigrationFile_ReturnsMigrationObject` | Verifies that a well-formed migration file produces a non-null `Migration` object with correct metadata. | – |
| `ParseMigrationFile_WithDesignerFile_ExtractsCorrectMigrationId` | Ensures that a `.Designer.cs` file is parsed and the migration ID (timestamp) is extracted correctly. | – |
| `ParseMigrationFile_WithInvalidTimestamp_ReturnsNull` | Confirms that a migration file with an unparseable timestamp (e.g., non-numeric prefix) causes the parser to return `null`. | – |
| `ParseMigrationFile_WithEmptyContent_StillParsesMetadata` | Checks that an empty or minimal file content still yields a `Migration` object with default or empty metadata (e.g., empty name, zero timestamp). | – |
| `ParseMigrationFile_WithVariousValidNames_ExtractionSucceeds` | Validates that different valid migration name formats (e.g., with underscores, numbers, mixed case) are parsed without error. | – |
| `ParseMigrationFile_WithNullMigrationFile_ThrowsException` | Asserts that passing a `null` migration file argument throws an `ArgumentNullException`. | `ArgumentNullException` |
| `ParseMigrationFile_WithComplexContent_ParsesSuccessfully` | Tests parsing of a migration file containing complex C# code (e.g., multiple operations, nested classes, comments) to ensure the parser handles it robustly. | – |

## Usage

The following examples demonstrate how to use `MigrationParserServiceTests` in a test project.

### Example 1: Running a specific test with xUnit

```csharp
using Xunit;

public class MigrationTestRunner
{
    [Fact]
    public void RunParseMigrationFile_WithValidMigrationFile_ReturnsMigrationObject()
    {
        var testClass = new MigrationParserServiceTests();
        // The test method itself performs all setup and assertions.
        testClass.ParseMigrationFile_WithValidMigrationFile_ReturnsMigrationObject();
    }
}
```

### Example 2: Writing a new test that follows the same pattern

```csharp
using Xunit;

public class CustomMigrationParserTests
{
    private readonly MigrationParserService _service = new MigrationParserService();

    [Fact]
    public void ParseMigrationFile_WithCustomContent_ReturnsExpectedMigration()
    {
        // Arrange
        var migrationFile = new MigrationFile
        {
            FilePath = "20250315_MyMigration.cs",
            Content = "namespace Migrations { public partial class MyMigration : Migration { ... } }"
        };

        // Act
        var result = _service.Parse(migrationFile);

        // Assert
        Assert.NotNull(result);
        Assert.Equal("20250315_MyMigration", result.Name);
    }
}
```

## Notes

- **Edge cases**: The tests cover scenarios where migration files have empty content, invalid timestamps, or null file references. These conditions are handled gracefully by the parser (returning `null` or throwing `ArgumentNullException`). Complex content with nested classes and comments is also verified to not break parsing.
- **Thread safety**: `MigrationParserServiceTests` is a test class and is not designed to be thread-safe. Each test method should be executed in isolation. The underlying `MigrationParserService` is assumed to be stateless and can be used concurrently, but the test class itself does not enforce any synchronization.
- **Test isolation**: No shared state exists between test methods; each method creates its own instances of the service and test data. This ensures that tests can be run in any order without interference.
