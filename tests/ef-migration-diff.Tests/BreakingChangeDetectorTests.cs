#nullable enable

using EfMigrationDiff.Models;
using EfMigrationDiff.Services;
using FluentAssertions;
using Microsoft.Extensions.Logging;
using Moq;

namespace EfMigrationDiff.Tests;

/// <summary>
/// Unit tests for <see cref="BreakingChangeDetector"/> service that classifies schema changes
/// as Breaking vs Safe based on backward compatibility analysis.
/// </summary>
public class BreakingChangeDetectorTests
{
    private readonly BreakingChangeDetector _detector;
    private readonly Mock<ILogger<BreakingChangeDetector>> _loggerMock;

    public BreakingChangeDetectorTests()
    {
        _loggerMock = new Mock<ILogger<BreakingChangeDetector>>();
        _detector = new BreakingChangeDetector(_loggerMock.Object);
    }

    #region Drop Column/Table Tests - Breaking Changes

    [Fact]
    public void ClassifyChange_DropColumn_IsBreaking()
    {
        _loggerMock.Object.LogInformation("Starting test {TestName} with change {ChangeType} on table {TableName}",
            nameof(ClassifyChange_DropColumn_IsBreaking), SqlChangeType.DropColumn, "Users");

        var change = new SchemaChange("test1", SqlChangeType.DropColumn, "DROP COLUMN [Email]")
        {
            TableName = "Users",
            ColumnName = "Email"
        };

        var result = _detector.ClassifyChange(change);

        _loggerMock.Object.LogInformation("Completed test {TestName} with result {Severity} and reason {Reason}",
            nameof(ClassifyChange_DropColumn_IsBreaking), result.Severity, result.Reason);

        result.Severity.Should().Be(BreakingChangeSeverity.Breaking);
        result.Reason.Should().Contain("column 'Email' is dropped");
    }

    [Fact]
    public void ClassifyChange_DropTable_IsBreaking()
    {
        _loggerMock.Object.LogInformation("Starting test {TestName} with change {ChangeType} on table {TableName}",
            nameof(ClassifyChange_DropTable_IsBreaking), SqlChangeType.DropTable, "LegacyData");

        var change = new SchemaChange("test2", SqlChangeType.DropTable, "DROP TABLE [LegacyData]")
        {
            TableName = "LegacyData"
        };

        var result = _detector.ClassifyChange(change);

        _loggerMock.Object.LogInformation("Completed test {TestName} with result {Severity} and reason {Reason}",
            nameof(ClassifyChange_DropTable_IsBreaking), result.Severity, result.Reason);

        result.Severity.Should().Be(BreakingChangeSeverity.Breaking);
        result.Reason.Should().Contain("table 'LegacyData' is dropped");
    }

    [Fact]
    public void ClassifyChange_DropIndex_IsBreaking()
    {
        _loggerMock.Object.LogInformation("Starting test {TestName} with change {ChangeType} on table {TableName}",
            nameof(ClassifyChange_DropIndex_IsBreaking), SqlChangeType.DropIndex, "Users");

        var change = new SchemaChange("test3", SqlChangeType.DropIndex, "DROP INDEX [IX_Users_Email]")
        {
            TableName = "Users"
        };

        var result = _detector.ClassifyChange(change);

        _loggerMock.Object.LogInformation("Completed test {TestName} with result {Severity} and reason {Reason}",
            nameof(ClassifyChange_DropIndex_IsBreaking), result.Severity, result.Reason);

        result.Severity.Should().Be(BreakingChangeSeverity.Breaking);
        result.Reason.Should().Contain("index on table 'Users' is dropped");
    }

    [Fact]
    public void ClassifyChange_DropForeignKey_IsBreaking()
    {
        _loggerMock.Object.LogInformation("Starting test {TestName} with change {ChangeType} on table {TableName}",
            nameof(ClassifyChange_DropForeignKey_IsBreaking), SqlChangeType.DropForeignKey, "Users");

        var change = new SchemaChange("test4", SqlChangeType.DropForeignKey, "DROP FOREIGN KEY [FK_Users_Orders]")
        {
            TableName = "Users"
        };

        var result = _detector.ClassifyChange(change);

        _loggerMock.Object.LogInformation("Completed test {TestName} with result {Severity} and reason {Reason}",
            nameof(ClassifyChange_DropForeignKey_IsBreaking), result.Severity, result.Reason);

        result.Severity.Should().Be(BreakingChangeSeverity.Breaking);
        result.Reason.Should().Contain("foreign key on table 'Users' is dropped");
    }

    #endregion

    #region Add Column Tests - Safe vs Breaking

    [Fact]
    public void ClassifyChange_AddNullableColumn_IsSafe()
    {
        _loggerMock.Object.LogInformation("Starting test {TestName}", nameof(ClassifyChange_AddNullableColumn_IsSafe));
        var change = new SchemaChange("test5", SqlChangeType.AddColumn, "ADD COLUMN [OptionalField] nvarchar(255) NULL")
        {
            TableName = "Users",
            ColumnName = "OptionalField",
            Metadata = new Dictionary<string, object?> { { "Nullable", "true" } }
        };

        var result = _detector.ClassifyChange(change);

        _loggerMock.Object.LogInformation("Completed test {TestName} with result {Severity}", nameof(ClassifyChange_AddNullableColumn_IsSafe), result.Severity);
        result.Severity.Should().Be(BreakingChangeSeverity.Safe);
        result.Reason.Should().Contain("added nullable column 'OptionalField' - backward compatible");
    }

    [Fact]
    public void ClassifyChange_AddNonNullableColumnWithoutDefault_IsBreaking()
    {
        _loggerMock.Object.LogInformation("Starting test {TestName}", nameof(ClassifyChange_AddNonNullableColumnWithoutDefault_IsBreaking));
        var change = new SchemaChange("test6", SqlChangeType.AddColumn, "ADD COLUMN [RequiredField] nvarchar(255) NOT NULL")
        {
            TableName = "Users",
            ColumnName = "RequiredField",
            Metadata = new Dictionary<string, object?> { { "Nullable", "false" } }
        };

        var result = _detector.ClassifyChange(change);

        _loggerMock.Object.LogInformation("Completed test {TestName} with result {Severity}", nameof(ClassifyChange_AddNonNullableColumnWithoutDefault_IsBreaking), result.Severity);
        result.Severity.Should().Be(BreakingChangeSeverity.Breaking);
        result.Reason.Should().Contain("added non-nullable column 'RequiredField' without default value");
    }

    [Fact]
    public void ClassifyChange_AddNonNullableColumnWithDefault_IsSafe()
    {
        _loggerMock.Object.LogInformation("Starting test {TestName}", nameof(ClassifyChange_AddNonNullableColumnWithDefault_IsSafe));
        var change = new SchemaChange("test7", SqlChangeType.AddColumn, "ADD COLUMN [RequiredField] nvarchar(255) NOT NULL")
        {
            TableName = "Users",
            ColumnName = "RequiredField",
            Metadata = new Dictionary<string, object?> { { "Nullable", "false" } },
            DefaultValue = "'default'"
        };

        var result = _detector.ClassifyChange(change);

        _loggerMock.Object.LogInformation("Completed test {TestName} with result {Result}", nameof(ClassifyChange_AddNonNullableColumnWithDefault_IsSafe), result.Severity);
        result.Severity.Should().Be(BreakingChangeSeverity.Safe);
        result.Reason.Should().Be("backward compatible change");
    }

    #endregion

    #region Type Narrowing Tests - Breaking Changes

    [Fact]
    public void ClassifyChange_ModifyColumn_NarrowingVarcharMaxToVarchar_IsBreaking()
    {
        _loggerMock.Object.LogInformation("Starting test {TestName}", nameof(ClassifyChange_ModifyColumn_NarrowingVarcharMaxToVarchar_IsBreaking));
        var change = new SchemaChange("test8", SqlChangeType.ModifyColumn, "ALTER COLUMN [Description] varchar(MAX) -> varchar(255)")
        {
            TableName = "Products",
            ColumnName = "Description"
        };
        change.AddMetadata("OldType", "varchar(max)");
        change.AddMetadata("NewType", "varchar(255)");

        var result = _detector.ClassifyChange(change);

        _loggerMock.Object.LogInformation("Completed test {TestName} with result {Result}", nameof(ClassifyChange_ModifyColumn_NarrowingVarcharMaxToVarchar_IsBreaking), result.Severity);
        result.Severity.Should().Be(BreakingChangeSeverity.Breaking);
        result.Reason.Should().Contain("column type narrowed from varchar(max) to varchar(255)");
    }

    [Fact]
    public void ClassifyChange_ModifyColumn_NarrowingNvarcharMaxToNvarchar_IsBreaking()
    {
        _loggerMock.Object.LogInformation("Starting test {TestName}", nameof(ClassifyChange_ModifyColumn_NarrowingNvarcharMaxToNvarchar_IsBreaking));
        var change = new SchemaChange("test9", SqlChangeType.ModifyColumn, "ALTER COLUMN [LongText] nvarchar(MAX) -> nvarchar(500)")
        {
            TableName = "Posts",
            ColumnName = "LongText"
        };
        change.AddMetadata("OldType", "nvarchar(max)");
        change.AddMetadata("NewType", "nvarchar(500)");

        var result = _detector.ClassifyChange(change);

        _loggerMock.Object.LogInformation("Completed test {TestName} with result {Result}", nameof(ClassifyChange_ModifyColumn_NarrowingNvarcharMaxToNvarchar_IsBreaking), result.Severity);
        result.Severity.Should().Be(BreakingChangeSeverity.Breaking);
        result.Reason.Should().Contain("column type narrowed from nvarchar(max) to nvarchar(500)");
    }

    [Fact]
    public void ClassifyChange_ModifyColumn_NarrowingIntToSmallint_IsBreaking()
    {
        _loggerMock.Object.LogInformation("Starting test {TestName}", nameof(ClassifyChange_ModifyColumn_NarrowingIntToSmallint_IsBreaking));
        var change = new SchemaChange("test10", SqlChangeType.ModifyColumn, "ALTER COLUMN [StatusId] int -> smallint")
        {
            TableName = "Orders",
            ColumnName = "StatusId"
        };
        change.AddMetadata("OldType", "int");
        change.AddMetadata("NewType", "smallint");

        var result = _detector.ClassifyChange(change);

        _loggerMock.Object.LogInformation("Completed test {TestName} with result {Result}", nameof(ClassifyChange_ModifyColumn_NarrowingIntToSmallint_IsBreaking), result.Severity);
        result.Severity.Should().Be(BreakingChangeSeverity.Breaking);
        result.Reason.Should().Contain("column type narrowed from int to smallint");
    }

    [Fact]
    public void ClassifyChange_ModifyColumn_NarrowingDecimalPrecision_IsBreaking()
    {
        _loggerMock.Object.LogInformation("Starting test {TestName}", nameof(ClassifyChange_ModifyColumn_NarrowingDecimalPrecision_IsBreaking));
        var change = new SchemaChange("test11", SqlChangeType.ModifyColumn, "ALTER COLUMN [Price] decimal(18,2) -> decimal(10,2)")
        {
            TableName = "Products",
            ColumnName = "Price"
        };
        change.AddMetadata("OldType", "decimal(18,2)");
        change.AddMetadata("NewType", "decimal(10,2)");
        change.AddMetadata("OldPrecision", "18");
        change.AddMetadata("NewPrecision", "10");

        var result = _detector.ClassifyChange(change);

        _loggerMock.Object.LogInformation("Completed test {TestName} with result {Result}", nameof(ClassifyChange_ModifyColumn_NarrowingDecimalPrecision_IsBreaking), result.Severity);
        result.Severity.Should().Be(BreakingChangeSeverity.Breaking);
        result.Reason.Should().Contain("column precision reduced from 18 to 10");
    }

    [Fact]
    public void ClassifyChange_ModifyColumn_WideningDecimal_IsSafe()
    {
        _loggerMock.Object.LogInformation("Starting test {TestName}", nameof(ClassifyChange_ModifyColumn_WideningDecimal_IsSafe));
        var change = new SchemaChange("test12", SqlChangeType.ModifyColumn, "ALTER COLUMN [Price] decimal(10,2) -> decimal(18,2)")
        {
            TableName = "Products",
            ColumnName = "Price"
        };
        change.AddMetadata("OldType", "decimal(10,2)");
        change.AddMetadata("NewType", "decimal(18,2)");
        change.AddMetadata("OldPrecision", "10");
        change.AddMetadata("NewPrecision", "18");

        var result = _detector.ClassifyChange(change);

        _loggerMock.Object.LogInformation("Completed test {TestName} with result {Result}", nameof(ClassifyChange_ModifyColumn_WideningDecimal_IsSafe), result.Severity);
        result.Severity.Should().Be(BreakingChangeSeverity.Safe);
        result.Reason.Should().Be("backward compatible change");
    }

    #endregion

    #region Rename Tests - Warning Changes

    [Fact]
    public void ClassifyChange_Rename_IsWarning()
    {
        _loggerMock.Object.LogInformation("Starting test {TestName}", nameof(ClassifyChange_Rename_IsWarning));
        var change = new SchemaChange("test13", SqlChangeType.Rename, "sp_rename 'OldProcedure', 'NewProcedure'")
        {
            OldValue = "OldProcedure",
            NewValue = "NewProcedure"
        };

        var result = _detector.ClassifyChange(change);

        _loggerMock.Object.LogInformation("Completed test {TestName} with result {Result}", nameof(ClassifyChange_Rename_IsWarning), result.Severity);
        result.Severity.Should().Be(BreakingChangeSeverity.Warning);
        result.Reason.Should().Contain("object renamed - may affect application code that references old name");
    }

    #endregion

    #region Foreign Key Tests - Warning Changes

    [Fact]
    public void ClassifyChange_AddForeignKey_IsWarning()
    {
        _loggerMock.Object.LogInformation("Starting test {TestName}", nameof(ClassifyChange_AddForeignKey_IsWarning));
        var change = new SchemaChange("test14", SqlChangeType.AddForeignKey, "ADD CONSTRAINT [FK_Orders_Customers] FOREIGN KEY")
        {
            TableName = "Orders"
        };

        var result = _detector.ClassifyChange(change);

        _loggerMock.Object.LogInformation("Completed test {TestName} with result {Result}", nameof(ClassifyChange_AddForeignKey_IsWarning), result.Severity);
        result.Severity.Should().Be(BreakingChangeSeverity.Warning);
        result.Reason.Should().Contain("foreign key added - may affect data integrity constraints");
    }

    #endregion

    #region ClassifyChanges Tests - Multiple Changes

    [Fact]
    public void ClassifyChanges_ProcessesMultipleChanges()
    {
        _loggerMock.Object.LogInformation("Starting test {TestName}", nameof(ClassifyChanges_ProcessesMultipleChanges));
        var changes = new List<SchemaChange>
        {
            new SchemaChange("test15", SqlChangeType.DropColumn, "DROP COLUMN [OldField]")
            {
                TableName = "Users",
                ColumnName = "OldField"
            },
            new SchemaChange("test16", SqlChangeType.AddColumn, "ADD COLUMN [NewField] nvarchar(100) NULL")
            {
                TableName = "Users",
                ColumnName = "NewField",
                Metadata = new Dictionary<string, object?> { { "Nullable", "true" } }
            },
            new SchemaChange("test17", SqlChangeType.ModifyColumn, "ALTER COLUMN [Count] int -> bigint")
            {
                TableName = "Stats",
                ColumnName = "Count",
                Metadata = new Dictionary<string, object?>
                {
                    { "OldType", "int" },
                    { "NewType", "bigint" }
                }
            }
        };

        var results = _detector.ClassifyChanges(changes);

        _loggerMock.Object.LogInformation("Completed test {TestName} with count {Count}", nameof(ClassifyChanges_ProcessesMultipleChanges), results.Count);
        results.Should().HaveCount(3);
        results[0].Severity.Should().Be(BreakingChangeSeverity.Breaking);
        results[1].Severity.Should().Be(BreakingChangeSeverity.Safe);
        results[2].Severity.Should().Be(BreakingChangeSeverity.Safe);
    }

    #endregion

    #region ClassifyDiffResult Tests - Summary Statistics

    [Fact]
    public void ClassifyDiffResult_CalculatesCorrectCounts()
    {
        _loggerMock.Object.LogInformation("Starting test {TestName}", nameof(ClassifyDiffResult_CalculatesCorrectCounts));
        var diffResult = new SchemaDiffResult
        {
            Id = Guid.NewGuid(),
            SourceLabel = "main",
            TargetLabel = "feature",
            SourceOnlyChanges = new List<SchemaChange>
            {
                new SchemaChange("test18", SqlChangeType.DropColumn, "DROP COLUMN [OldField]")
                {
                    TableName = "Users",
                    ColumnName = "OldField"
                }
            },
            TargetOnlyChanges = new List<SchemaChange>
            {
                new SchemaChange("test19", SqlChangeType.AddColumn, "ADD COLUMN [NewField] nvarchar(100) NULL")
                {
                    TableName = "Users",
                    ColumnName = "NewField",
                    Metadata = new Dictionary<string, object?> { { "Nullable", "true" } }
                }
            },
            ModifiedChanges = new List<SchemaChange>
            {
                new SchemaChange("test20", SqlChangeType.ModifyColumn, "ALTER COLUMN [Count] int -> bigint")
                {
                    TableName = "Stats",
                    ColumnName = "Count",
                    Metadata = new Dictionary<string, object?>
                    {
                        { "OldType", "int" },
                        { "NewType", "bigint" }
                    }
                }
            }
        };

        var summary = _detector.ClassifyDiffResult(diffResult);

        _loggerMock.Object.LogInformation("Completed test {TestName} with result {Result}", nameof(ClassifyDiffResult_CalculatesCorrectCounts), summary.IsSafe);
        summary.TotalChanges.Should().Be(3);
        summary.BreakingChanges.Should().Be(1);
        summary.SafeChanges.Should().Be(2);
        summary.WarningChanges.Should().Be(0);
        summary.HasBreakingChanges.Should().BeTrue();
        summary.IsSafe.Should().BeFalse();
    }

    [Fact]
    public void ClassifyDiffResult_IsSafe_WhenNoBreakingOrWarnings()
    {
        _loggerMock.Object.LogInformation("Starting test {TestName}", nameof(ClassifyDiffResult_IsSafe_WhenNoBreakingOrWarnings));
        var diffResult = new SchemaDiffResult
        {
            Id = Guid.NewGuid(),
            SourceLabel = "main",
            TargetLabel = "feature",
            SourceOnlyChanges = new List<SchemaChange>(),
            TargetOnlyChanges = new List<SchemaChange>
            {
                new SchemaChange("test21", SqlChangeType.AddColumn, "ADD COLUMN [NewField] nvarchar(100) NULL")
                {
                    TableName = "Users",
                    ColumnName = "NewField",
                    Metadata = new Dictionary<string, object?> { { "Nullable", "true" } }
                }
            },
            ModifiedChanges = new List<SchemaChange>()
        };

        var summary = _detector.ClassifyDiffResult(diffResult);

        _loggerMock.Object.LogInformation("Completed test {TestName} with result {Result}", nameof(ClassifyDiffResult_IsSafe_WhenNoBreakingOrWarnings), summary.IsSafe);
        summary.HasBreakingChanges.Should().BeFalse();
        summary.IsSafe.Should().BeTrue();
    }

    #endregion

    #region Edge Cases

    [Fact]
    public void ClassifyChange_UnknownChangeType_DefaultsToSafe()
    {
        _loggerMock.Object.LogInformation("Starting test {TestName}", nameof(ClassifyChange_UnknownChangeType_DefaultsToSafe));
        var change = new SchemaChange("test22", SqlChangeType.Unknown, "SOME UNKNOWN OPERATION")
        {
            TableName = "TestTable"
        };

        var result = _detector.ClassifyChange(change);

        _loggerMock.Object.LogInformation("Completed test {TestName} with result {Result}", nameof(ClassifyChange_UnknownChangeType_DefaultsToSafe), result.Severity);
        result.Severity.Should().Be(BreakingChangeSeverity.Safe);
        result.Reason.Should().Be("backward compatible change");
    }

    [Fact]
    public void ClassifyChange_DropProcedure_IsBreaking()
    {
        _loggerMock.Object.LogInformation("Starting test {TestName}", nameof(ClassifyChange_DropProcedure_IsBreaking));
        var change = new SchemaChange("test23", SqlChangeType.DropProcedure, "DROP PROCEDURE [OldProc]")
        {
            TableName = "Test"
        };

        var result = _detector.ClassifyChange(change);

        _loggerMock.Object.LogInformation("Completed test {TestName} with result {Result}", nameof(ClassifyChange_DropProcedure_IsBreaking), result.Severity);
        result.Severity.Should().Be(BreakingChangeSeverity.Breaking);
    }

    [Fact]
    public void ClassifyChange_DropView_IsBreaking()
    {
        _loggerMock.Object.LogInformation("Starting test {TestName}", nameof(ClassifyChange_DropView_IsBreaking));
        var change = new SchemaChange("test24", SqlChangeType.DropView, "DROP VIEW [OldView]")
        {
            TableName = "Test"
        };

        var result = _detector.ClassifyChange(change);

        _loggerMock.Object.LogInformation("Completed test {TestName} with result {Result}", nameof(ClassifyChange_DropView_IsBreaking), result.Severity);
        result.Severity.Should().Be(BreakingChangeSeverity.Breaking);
    }

    #endregion
}