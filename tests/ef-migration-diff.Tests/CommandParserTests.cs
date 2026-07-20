using System;
using System.Collections.Generic;
using System.IO;
using EfMigrationDiff.CLI;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace EfMigrationDiff.Tests
{
    public class CommandParserTests
    {
        private readonly CommandParser _parser;
        private readonly IServiceProvider _serviceProvider;

        public CommandParserTests()
        {
            _parser = new CommandParser();
            _serviceProvider = new ServiceCollection().BuildServiceProvider();
        }

        [Fact]
        public void Parse_ValidCommandWithAllFlags_ShouldPopulateOptionsAndArguments()
        {
            // Arrange
            var commandName = "test";
            var args = new[]
            {
                "--format=json",   // long option with value
                "-f", "csv",       // short option with separate value
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
        public void Parse_MissingOptionValue_ShouldTreatAsFlag()
        {
            // Arrange
            var commandName = "test";
            var args = new[]
            {
                "--format"   // flag style, no value
            };

            // Act
            var context = _parser.Parse(commandName, args, _serviceProvider, TextWriter.Null, TextWriter.Null);

            // Assert
            Assert.True(context.ParsedOptions.ContainsKey("format"));
            Assert.Equal("true", context.ParsedOptions["format"]);
        }

        [Fact]
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
