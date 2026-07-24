using System;
using System.Collections.Generic;
using System.IO;
using EfMigrationDiff.CLI;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace EfMigrationDiff.Tests
{
    /// <summary>
    /// Contains unit tests for the <see cref="CommandParser"/> class that verify command line argument parsing behavior.
    /// </summary>
    public class CommandParserTests
    {
        private readonly CommandParser _parser;

        private readonly IServiceProvider _serviceProvider;

        /// <summary>
        /// Initializes a new instance of the <see cref="CommandParserTests"/> class.
        /// </summary>
        public CommandParserTests()
        {
            _parser = new CommandParser();
            _serviceProvider = new ServiceCollection().BuildServiceProvider();
        }

        [Fact]
        /// <summary>
        /// Tests that the parser correctly handles a valid command with all supported flag types and positional arguments.
        /// Verifies that both long options (--format=json) and short options (-f csv) are properly parsed,
        /// and that positional arguments are correctly captured.
        /// </summary>
        public void Parse_ValidCommandWithAllFlags_ShouldPopulateOptionsAndArguments()
        {
            // Arrange
            var commandName = "test";
            var args = new[]
            {
                "--format=json", // long option with value
                "-f", "csv", // short option with separate value
                "pos1",
                "pos2"
            };

            // Act
            var context = _parser.Parse(commandName, args, _serviceProvider, TextWriter.Null, TextWriter.Null);

            // Assert
            Assert.Equal(commandName, context.CommandName);
            Assert.Equal(2, context.ParsedOptions.Count);
            Assert.True(context.ParsedOptions.ContainsKey("format"));
            Assert.True(context.ParsedOptions.ContainsKey("f"));
            Assert.Equal("json", context.ParsedOptions["format"]);
            Assert.Equal("csv", context.ParsedOptions["f"]);
            Assert.Equal(2, context.ParsedArguments.Count);
            Assert.Contains("pos1", context.ParsedArguments);
            Assert.Contains("pos2", context.ParsedArguments);
        }

        [Fact]
        /// <summary>
        /// Tests that the parser treats a missing option value as a boolean flag set to "true".
        /// Verifies that when an option is specified without a value (e.g., --format),
        /// it is added to the parsed options with a value of "true".
        /// </summary>
        public void Parse_MissingOptionValue_ShouldTreatAsFlag()
        {
            // Arrange
            var commandName = "test";
            var args = new[]
            {
                "--format" // flag style, no value
            };

            // Act
            var context = _parser.Parse(commandName, args, _serviceProvider, TextWriter.Null, TextWriter.Null);

            // Assert
            Assert.True(context.ParsedOptions.ContainsKey("format"));
            Assert.Equal("true", context.ParsedOptions["format"]);
        }

        [Fact]
        /// <summary>
        /// Tests that the parser handles unknown flags by adding them as boolean flags.
        /// Verifies that any unrecognized command line flag (e.g., --unknown-flag) is
        /// added to the parsed options with a value of "true".
        /// </summary>
        public void Parse_UnknownFlag_ShouldBeAddedAsFlag()
        {
            // Arrange
            var commandName = "test";
            var args = new[]
            {
                "--unknown-flag"
            };

            // Act
            var context = _parser.Parse(commandName, args, _serviceProvider, TextWriter.Null, TextWriter.Null);

            // Assert
            Assert.True(context.ParsedOptions.ContainsKey("unknown-flag"));
            Assert.Equal("true", context.ParsedOptions["unknown-flag"]);
        }

        [Fact]
        /// <summary>
        /// Tests that the parser recognizes the help flag.
        /// Verifies that the --help flag is properly detected and added to the parsed options
        /// with a value of "true".
        /// </summary>
        public void Parse_HelpInvocation_ShouldBeRecognizedAsFlag()
        {
            // Arrange
            var commandName = "test";
            var args = new[]
            {
                "--help"
            };

            // Act
            var context = _parser.Parse(commandName, args, _serviceProvider, TextWriter.Null, TextWriter.Null);

            // Assert
            Assert.True(context.ParsedOptions.ContainsKey("help"));
            Assert.Equal("true", context.ParsedOptions["help"]);
        }

        [Fact]
        /// <summary>
        /// Tests that the parser detects duplicate flag options.
        /// Verifies that when the same flag is specified multiple times (e.g., --summary --summary),
        /// validation correctly identifies this as an error.
        /// </summary>
        public void Validate_DuplicateFlags_ShouldReturnError()
        {
            // Arrange
            var commandName = "compare";
            var args = new[] { "--summary", "--summary"};
            var context = _parser.Parse(commandName, args, _serviceProvider, TextWriter.Null, TextWriter.Null);

            // Act
            var validationError = _parser.Validate(context, commandName, args);

            // Assert
            Assert.NotNull(validationError);
            Assert.Contains("Duplicate flag(s) specified", validationError);
            Assert.Contains("--summary", validationError);
        }

        [Fact]
        /// <summary>
        /// Tests that the parser detects conflicting options.
        /// Verifies that when mutually exclusive options like --summary and --dot are both specified,
        /// validation correctly identifies this as an error.
        /// </summary>
        public void Validate_ConflictingOptions_ShouldReturnError()
        {
            // Arrange
            var commandName = "compare";
            var args = new[] { "--summary", "--dot", "output.dot"};
            var context = _parser.Parse(commandName, args, _serviceProvider, TextWriter.Null, TextWriter.Null);

            // Act
            var validationError = _parser.Validate(context, commandName, args);

            // Assert
            Assert.NotNull(validationError);
            Assert.Contains("mutually exclusive", validationError);
            Assert.Contains("--summary", validationError);
            Assert.Contains("--dot", validationError);
        }

        [Fact]
        /// <summary>
        /// Tests that the parser handles paths with spaces correctly.
        /// Verifies that arguments containing spaces are properly parsed as positional arguments.
        /// </summary>
        public void Parse_PathWithSpaces_ShouldBeHandledCorrectly()
        {
            // Arrange
            var commandName = "compare";
            var args = new[] { "develop", "feature/branch with spaces", "--format", "json"};

            // Act
            var context = _parser.Parse(commandName, args, _serviceProvider, TextWriter.Null, TextWriter.Null);

            // Assert
            Assert.Equal(2, context.ParsedArguments.Count);
            Assert.Contains("develop", context.ParsedArguments);
            Assert.Contains("feature/branch with spaces", context.ParsedArguments);
            Assert.True(context.ParsedOptions.ContainsKey("format"));
            Assert.Equal("json", context.ParsedOptions["format"]);
        }

        [Fact]
        /// <summary>
        /// Tests that the parser generates helpful usage information.
        /// Verifies that GenerateUsage() returns a formatted string with expected sections.
        /// </summary>
        public void GenerateUsage_ShouldReturnFormattedUsageString()
        {
            // Arrange
            var commandName = "ef-migration-diff";

            // Act
            var usage = _parser.GenerateUsage(commandName);

            // Assert
            Assert.NotNull(usage);
            Assert.Contains("Usage:", usage);
            Assert.Contains(commandName, usage);
            Assert.Contains("Options:", usage);
            Assert.Contains("--format", usage);
            Assert.Contains("--dot", usage);
            Assert.Contains("--summary", usage);
            Assert.Contains("Examples:", usage);
        }

        [Fact]
        /// <summary>
        /// Tests that the parser detects unknown options and reports them clearly.
        /// Verifies that unknown flags like --unknown-option are identified in error messages.
        /// </summary>
        public void Validate_UnknownOption_ShouldReturnHelpfulErrorMessage()
        {
            // Arrange
            var commandName = "compare";
            var args = new[] { "develop", "main", "--unknown-option", "--another-bad-flag" };
            var context = _parser.Parse(commandName, args, _serviceProvider, TextWriter.Null, TextWriter.Null);

            // Act
            var validationError = _parser.Validate(context, commandName, args);

            // Assert
            Assert.NotNull(validationError);
            Assert.Contains("Unknown option(s) specified", validationError);
            Assert.Contains("--unknown-option", validationError);
            Assert.Contains("--another-bad-flag", validationError);
        }

        [Fact]
        /// <summary>
        /// Tests that the parser detects duplicate flags with multiple occurrences.
        /// Verifies that --summary --summary --summary is caught and reported clearly.
        /// </summary>
        public void Validate_MultipleDuplicateFlags_ShouldReturnError()
        {
            // Arrange
            var commandName = "compare";
            var args = new[] { "--summary", "--summary", "--summary", "develop", "main" };
            var context = _parser.Parse(commandName, args, _serviceProvider, TextWriter.Null, TextWriter.Null);

            // Act
            var validationError = _parser.Validate(context, commandName, args);

            // Assert
            Assert.NotNull(validationError);
            Assert.Contains("Duplicate flag(s) specified", validationError);
            Assert.Contains("--summary", validationError);
        }

        [Fact]
        /// <summary>
        /// Tests that the parser detects conflicting options --summary and --dot together.
        /// Verifies that mutually exclusive options are properly detected.
        /// </summary>
        public void Validate_ConflictingOptions_WithMultipleArgs_ShouldReturnError()
        {
            // Arrange
            var commandName = "compare";
            var args = new[] { "develop", "main", "--summary", "--dot", "output.dot" };
            var context = _parser.Parse(commandName, args, _serviceProvider, TextWriter.Null, TextWriter.Null);

            // Act
            var validationError = _parser.Validate(context, commandName, args);

            // Assert
            Assert.NotNull(validationError);
            Assert.Contains("mutually exclusive", validationError);
            Assert.Contains("--summary", validationError);
            Assert.Contains("--dot", validationError);
        }

        [Fact]
        /// <summary>
        /// Tests that the parser detects missing required positional arguments.
        /// Verifies that commands with fewer than 2 arguments are rejected.
        /// </summary>
        public void Validate_MissingRequiredArguments_ShouldReturnError()
        {
            // Arrange
            var commandName = "compare";
            var args = new[] { "--format", "json" };
            var context = _parser.Parse(commandName, args, _serviceProvider, TextWriter.Null, TextWriter.Null);

            // Act
            var validationError = _parser.Parse(commandName, args, _serviceProvider, TextWriter.Null, TextWriter.Null);
            var validationResult = _parser.Validate(context, commandName, args);

            // Assert
            Assert.NotNull(validationResult);
            Assert.Contains("Missing required arguments", validationResult);
            Assert.Contains("Expected at least 2 positional arguments", validationResult);
        }

        [Fact]
        /// <summary>
        /// Tests that the parser handles paths with multiple spaces correctly.
        /// Verifies that complex paths like "feature/my branch with spaces" are handled properly.
        /// </summary>
        public void Parse_ComplexPathWithMultipleSpaces_ShouldBeHandledCorrectly()
        {
            // Arrange
            var commandName = "compare";
            var args = new[] { "develop", "feature/my branch with multiple   spaces", "--format", "json" };

            // Act
            var context = _parser.Parse(commandName, args, _serviceProvider, TextWriter.Null, TextWriter.Null);

            // Assert
            Assert.Equal(2, context.ParsedArguments.Count);
            Assert.Contains("develop", context.ParsedArguments);
            Assert.Contains("feature/my branch with multiple   spaces", context.ParsedArguments);
            Assert.True(context.ParsedOptions.ContainsKey("format"));
            Assert.Equal("json", context.ParsedOptions["format"]);
        }

        [Fact]
        /// <summary>
        /// Tests that the parser handles short option with equals format (-f=json) as unknown option.
        /// Short options with equals are treated as unknown since they're not in the expected format.
        /// </summary>
        public void Parse_ShortOptionWithEquals_ShouldBeTreatedAsUnknown()
        {
            // Arrange
            var commandName = "compare";
            var args = new[] { "develop", "main", "-f=json" };

            // Act
            var context = _parser.Parse(commandName, args, _serviceProvider, TextWriter.Null, TextWriter.Null);

            // Assert - -f=json is treated as unknown option "f=json"
            Assert.Equal(2, context.ParsedArguments.Count);
            Assert.Contains("develop", context.ParsedArguments);
            Assert.Contains("main", context.ParsedArguments);
            // The parser treats -f=json as a combined short option, so "f=json" becomes the value
            Assert.True(context.ParsedOptions.ContainsKey("f"));
            Assert.Equal("=json", context.ParsedOptions["f"]);
        }

        [Fact]
        /// <summary>
        /// Tests that the parser handles combined short options (-fjson).
        /// Verifies that -fjson is parsed as -f with value "json".
        /// </summary>
        public void Parse_CombinedShortOption_ShouldParseCorrectly()
        {
            // Arrange
            var commandName = "compare";
            var args = new[] { "develop", "main", "-fjson" };

            // Act
            var context = _parser.Parse(commandName, args, _serviceProvider, TextWriter.Null, TextWriter.Null);

            // Assert
            Assert.Equal(2, context.ParsedArguments.Count);
            Assert.Contains("develop", context.ParsedArguments);
            Assert.Contains("main", context.ParsedArguments);
            Assert.True(context.ParsedOptions.ContainsKey("f"));
            Assert.Equal("json", context.ParsedOptions["f"]);
        }

        [Fact]
        /// <summary>
        /// Tests that the parser handles unknown short options (-x) and reports them clearly.
        /// </summary>
        public void Validate_UnknownShortOption_ShouldReturnHelpfulErrorMessage()
        {
            // Arrange
            var commandName = "compare";
            var args = new[] { "develop", "main", "-x", "-y" };
            var context = _parser.Parse(commandName, args, _serviceProvider, TextWriter.Null, TextWriter.Null);

            // Act
            var validationError = _parser.Validate(context, commandName, args);

            // Assert
            Assert.NotNull(validationError);
            Assert.Contains("Unknown option(s) specified", validationError);
            Assert.Contains("--x", validationError);
            Assert.Contains("--y", validationError);
        }

        [Fact]
        /// <summary>
        /// Tests that validation handles null arguments gracefully.
        /// </summary>
        public void Validate_NullArgs_ShouldThrowArgumentNullException()
        {
            // Arrange
            var commandName = "compare";
            var context = new CommandContext(commandName, Array.Empty<string>(), _serviceProvider);

            // Act & Assert
            Assert.Throws<ArgumentNullException>(() => _parser.Validate(context, commandName, null!));
        }

        [Fact]
        /// <summary>
        /// Tests that validation handles empty command name gracefully.
        /// </summary>
        public void Validate_EmptyCommandName_ShouldThrowArgumentException()
        {
            // Arrange
            var commandName = "";
            var args = Array.Empty<string>();
            var context = new CommandContext(commandName, args, _serviceProvider);

            // Act & Assert
            Assert.Throws<ArgumentException>(() => _parser.Validate(context, commandName, args));
        }
    }
}