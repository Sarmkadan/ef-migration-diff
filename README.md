## CommandParserTests

The CommandParserTests class contains tests for the CommandParser class. These tests cover various scenarios, including:

* Parsing valid commands with all flags
* Parsing commands with missing option values
* Parsing commands with unknown flags
* Parsing help invocations

Example usage:
```csharp
public CommandParserTests
public void Parse_ValidCommandWithAllFlags_ShouldPopulateOptionsAndArguments
public void Parse_MissingOptionValue_ShouldTreatAsFlag
public void Parse_UnknownFlag_ShouldBeAddedAsFlag
public void Parse_HelpInvocation_ShouldBeRecognizedAsFlag
```
