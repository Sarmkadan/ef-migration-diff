# StringAndCollectionExtensionsTestsExtensions
The `StringAndCollectionExtensionsTestsExtensions` class provides a set of static methods for testing string conversion and collection batching functionality. It offers a way to run all string conversion tests, all collection batching tests, retrieve a list of all test names, and assert that all tests pass. This class is useful for ensuring the correctness and reliability of string and collection extension methods.

## API
* `public static void RunAllStringConversionTests`: Runs all tests related to string conversion. This method does not take any parameters and does not return a value. It may throw exceptions if any of the tests fail.
* `public static void RunAllCollectionBatchingTests`: Runs all tests related to collection batching. This method does not take any parameters and does not return a value. It may throw exceptions if any of the tests fail.
* `public static List<string> GetAllTestNames`: Returns a list of names of all available tests. This method does not take any parameters and returns a list of strings. It does not throw any exceptions.
* `public static bool AssertAllTestsPass`: Asserts that all tests pass and returns a boolean indicating whether all tests were successful. This method does not take any parameters. It may throw exceptions if any of the assertions fail.

## Usage
```csharp
// Example 1: Running all string conversion tests
StringAndCollectionExtensionsTestsExtensions.RunAllStringConversionTests();

// Example 2: Retrieving all test names and asserting all tests pass
var allTestNames = StringAndCollectionExtensionsTestsExtensions.GetAllTestNames();
bool allTestsPassed = StringAndCollectionExtensionsTestsExtensions.AssertAllTestsPass();
Console.WriteLine($"All tests passed: {allTestsPassed}");
```

## Notes
The `StringAndCollectionExtensionsTestsExtensions` class is designed to be used in a testing context. When using this class, be aware that running all tests may take significant time and resources. Additionally, the `AssertAllTestsPass` method will throw an exception if any test fails, so it should be used with caution in production code. This class is thread-safe, as all methods are static and do not rely on any shared state. However, the tests themselves may not be thread-safe, so running them concurrently may lead to unexpected results. Edge cases, such as empty collections or null strings, should be handled carefully when using the methods provided by this class.
