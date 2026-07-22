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
    }
}