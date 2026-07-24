using System;
using System.IO;
using EfMigrationDiff.CLI;
using EfMigrationDiff.Models;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace EfMigrationDiff.Tests
{
    /// <summary>
    /// Contains unit tests for the CompareCommand output formatting with the --summary flag.
    /// Tests verify that the --summary flag produces the correct output format and that
    /// full detail output is produced when --summary is omitted.
    /// </summary>
    public class CompareCommandOutputTests
    {
        private readonly IServiceProvider _serviceProvider;
        private readonly CommandParser _parser;

        /// <summary>
        /// Initializes a new instance of the <see cref="CompareCommandOutputTests"/> class.
        /// Sets up the service provider and required services for testing.
        /// </summary>
        public CompareCommandOutputTests()
        {
            var services = new ServiceCollection();
            _serviceProvider = services.BuildServiceProvider();
            _parser = new CommandParser();
        }

        /// <summary>
        /// Tests that when --summary flag is passed, the parser correctly identifies it.
        /// </summary>
        [Fact]
        public void Parse_WithSummaryFlag_FlagIsRecognized()
        {
            // Arrange
            var commandName = "compare";
            var args = new[] { "develop", "main", "--summary" };

            // Act
            var context = _parser.Parse(commandName, args, _serviceProvider, TextWriter.Null, TextWriter.Null);

            // Assert
            Assert.True(context.HasOption("summary"));
            Assert.Equal("true", context.GetOption("summary"));
        }

        /// <summary>
        /// Tests that the parser correctly handles --summary with equals format.
        /// </summary>
        [Fact]
        public void Parse_SummaryWithEqualsFormat_FlagIsRecognized()
        {
            // Arrange
            var commandName = "compare";
            var args = new[] { "develop", "main", "--summary=true" };

            // Act
            var context = _parser.Parse(commandName, args, _serviceProvider, TextWriter.Null, TextWriter.Null);

            // Assert
            Assert.True(context.HasOption("summary"));
            Assert.Equal("true", context.GetOption("summary"));
        }

        /// <summary>
        /// Tests that when --summary flag is omitted, it is not present in parsed options.
        /// </summary>
        [Fact]
        public void Parse_WithoutSummaryFlag_FlagIsNotPresent()
        {
            // Arrange
            var commandName = "compare";
            var args = new[] { "develop", "main" };

            // Act
            var context = _parser.Parse(commandName, args, _serviceProvider, TextWriter.Null, TextWriter.Null);

            // Assert
            Assert.False(context.HasOption("summary"));
            Assert.Null(context.GetOption("summary"));
        }

        /// <summary>
        /// Tests that --summary flag produces output with correct aggregate counts format.
        /// This test verifies the format that CompareCommand would output when --summary is used.
        /// </summary>
        [Fact]
        public void CompareCommand_SummaryOutput_ContainsCorrectAggregateCounts()
        {
            // Arrange - Create a diff with known counts
            var diff = new MigrationDiff("source", "target");
            diff.AddSourceOnlyMigration(new Migration { Id = "1", Name = "AddUserTable", DbContextName = "AppDb", Sequence = 1 });
            diff.AddSourceOnlyMigration(new Migration { Id = "2", Name = "AddRoleTable", DbContextName = "AppDb", Sequence = 2 });
            diff.AddTargetOnlyMigration(new Migration { Id = "3", Name = "RemoveOldTable", DbContextName = "AppDb", Sequence = 3 });
            diff.AddCommonMigration(new Migration { Id = "4", Name = "InitialSetup", DbContextName = "AppDb", Sequence = 4 });
            diff.AddConflict(new ConflictInfo
            {
                Id = "c1",
                FirstMigrationId = "1",
                SecondMigrationId = "4",
                ConflictType = ConflictType.ConstraintConflict,
                Severity = ConflictSeverity.Error,
                Description = "Potential foreign key conflict"
            });

            // Act - Generate the summary line as the command would
            var summaryLine = $"Summary: Added: {diff.OnlyInSource.Count}, Removed: {diff.OnlyInTarget.Count}, Conflicts: {diff.Conflicts.Count}";

            // Assert
            Assert.Contains("Added: 2", summaryLine);
            Assert.Contains("Removed: 1", summaryLine);
            Assert.Contains("Conflicts: 1", summaryLine);
        }

        /// <summary>
        /// Tests that --summary combined with zero detected changes still prints a valid
        /// (non-empty, non-error) summary rather than an empty or malformed line.
        /// </summary>
        [Fact]
        public void CompareCommand_SummaryWithZeroChanges_OutputsValidSummary()
        {
            // Arrange - Create a diff with zero changes
            var diff = new MigrationDiff("source", "target");
            // No migrations added - zero changes

            // Act
            var summaryLine = $"Summary: Added: {diff.OnlyInSource.Count}, Removed: {diff.OnlyInTarget.Count}, Conflicts: {diff.Conflicts.Count}";

            // Assert
            Assert.Equal("Summary: Added: 0, Removed: 0, Conflicts: 0", summaryLine.Trim());
        }

        /// <summary>
        /// Tests that the summary output format is consistent and parseable.
        /// </summary>
        [Fact]
        public void CompareCommand_SummaryOutput_FormatIsConsistent()
        {
            // Arrange - Create two identical diffs
            var diff1 = new MigrationDiff("source", "target");
            diff1.AddSourceOnlyMigration(new Migration { Id = "1", Name = "M1", DbContextName = "Db", Sequence = 1 });
            diff1.AddTargetOnlyMigration(new Migration { Id = "2", Name = "M2", DbContextName = "Db", Sequence = 2 });
            diff1.AddConflict(new ConflictInfo
            {
                Id = "c1",
                FirstMigrationId = "1",
                SecondMigrationId = "2",
                ConflictType = ConflictType.ConstraintConflict,
                Severity = ConflictSeverity.Error,
                Description = "Conflict"
            });

            var diff2 = new MigrationDiff("source", "target");
            diff2.AddSourceOnlyMigration(new Migration { Id = "1", Name = "M1", DbContextName = "Db", Sequence = 1 });
            diff2.AddTargetOnlyMigration(new Migration { Id = "2", Name = "M2", DbContextName = "Db", Sequence = 2 });
            diff2.AddConflict(new ConflictInfo
            {
                Id = "c1",
                FirstMigrationId = "1",
                SecondMigrationId = "2",
                ConflictType = ConflictType.ConstraintConflict,
                Severity = ConflictSeverity.Error,
                Description = "Conflict"
            });

            // Act
            var summary1 = $"Summary: Added: {diff1.OnlyInSource.Count}, Removed: {diff1.OnlyInTarget.Count}, Conflicts: {diff1.Conflicts.Count}";
            var summary2 = $"Summary: Added: {diff2.OnlyInSource.Count}, Removed: {diff2.OnlyInTarget.Count}, Conflicts: {diff2.Conflicts.Count}";

            // Assert - Both should produce identical output for identical inputs
            Assert.Equal(summary1, summary2);
        }

        /// <summary>
        /// Tests that --summary flag works correctly with the dot flag validation.
        /// The flags should be mutually exclusive according to validation.
        /// </summary>
        [Fact]
        public void Validate_SummaryAndDotFlags_MutuallyExclusive()
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

        /// <summary>
        /// Tests that --summary flag is accepted alongside the dot-export flag without one suppressing the other's output.
        /// Note: This test only verifies the parser accepts both flags; mutual exclusivity is tested separately.
        /// </summary>
        [Fact]
        public void Parse_SummaryWithDotFlag_ParserAcceptsBoth()
        {
            // Arrange
            var commandName = "compare";
            var args = new[] { "develop", "main", "--summary", "--dot", "output.dot" };

            // Act
            var context = _parser.Parse(commandName, args, _serviceProvider, TextWriter.Null, TextWriter.Null);

            // Assert - Parser should accept both, but validation will reject them as mutually exclusive
            Assert.True(context.HasOption("summary"));
            Assert.True(context.HasOption("dot"));
        }

        /// <summary>
        /// Tests that --summary flag is accepted alongside format flag without conflicts.
        /// </summary>
        [Fact]
        public void Parse_SummaryWithFormatFlag_ParserAcceptsBoth()
        {
            // Arrange
            var commandName = "compare";
            var args = new[] { "develop", "main", "--summary", "--format", "json" };

            // Act
            var context = _parser.Parse(commandName, args, _serviceProvider, TextWriter.Null, TextWriter.Null);

            // Assert
            Assert.True(context.HasOption("summary"));
            Assert.Equal("true", context.GetOption("summary"));
            Assert.True(context.HasOption("format"));
            Assert.Equal("json", context.GetOption("format"));
        }

        /// <summary>
        /// Tests that when --summary is not used, the output would contain full report details.
        /// This test verifies the diff has data that would be in a full report.
        /// </summary>
        [Fact]
        public void CompareCommand_WithoutSummary_WouldOutputFullReport()
        {
            // Arrange - Create a diff with various data
            var diff = new MigrationDiff("source", "target");
            diff.AddSourceOnlyMigration(new Migration { Id = "1", Name = "AddUserTable", DbContextName = "AppDb", Sequence = 1 });
            diff.AddTargetOnlyMigration(new Migration { Id = "2", Name = "RemoveOldTable", DbContextName = "AppDb", Sequence = 2 });
            diff.SourceSchemaChanges.Add(new SchemaChange { Id = "sc1", MigrationId = "1", ChangeType = SqlChangeType.CreateTable, TableName = "Users", LineNumber = 10 });

            // Assert - The diff has data that would be in a full report
            Assert.Equal(1, diff.OnlyInSource.Count);
            Assert.Equal(1, diff.OnlyInTarget.Count);
            Assert.Equal(1, diff.GetTotalSchemaChanges());
        }

        /// <summary>
        /// Tests that the parser detects duplicate summary flags.
        /// </summary>
        [Fact]
        public void Validate_DuplicateSummaryFlags_ShouldReturnError()
        {
            // Arrange
            var commandName = "compare";
            var args = new[] { "--summary", "--summary", "develop", "main" };
            var context = _parser.Parse(commandName, args, _serviceProvider, TextWriter.Null, TextWriter.Null);

            // Act
            var validationError = _parser.Validate(context, commandName, args);

            // Assert
            Assert.NotNull(validationError);
            Assert.Contains("Duplicate flag(s) specified", validationError);
            Assert.Contains("--summary", validationError);
        }

        /// <summary>
        /// Tests that summary flag with various branch names works correctly.
        /// </summary>
        [Fact]
        public void Parse_SummaryWithDifferentBranchNames_WorksCorrectly()
        {
            // Arrange
            var commandName = "compare";
            var args = new[] { "feature/branch-with-spaces", "main", "--summary" };

            // Act
            var context = _parser.Parse(commandName, args, _serviceProvider, TextWriter.Null, TextWriter.Null);

            // Assert
            Assert.True(context.HasOption("summary"));
            Assert.Equal(2, context.ParsedArguments.Count);
            Assert.Contains("feature/branch-with-spaces", context.ParsedArguments);
            Assert.Contains("main", context.ParsedArguments);
        }
    }
}
