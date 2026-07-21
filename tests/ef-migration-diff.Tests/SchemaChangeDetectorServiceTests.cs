#nullable enable
using System.Collections.Generic;
using EfMigrationDiff.Models;
using EfMigrationDiff.Services;
using Xunit;

namespace EfMigrationDiff.Tests
{
    /// <summary>
    /// Unit tests for <see cref="SchemaChangeDetectorService"/>.
    /// The tests cover detection of added, removed and modified entities as expressed
    /// in migration builder calls and raw SQL statements.
    /// </summary>
    public class SchemaChangeDetectorServiceTests
    {
        private readonly SchemaChangeDetectorService _service = new();

        private static Migration CreateMigration(string id, string content) =>
            new Migration
            {
                Id = id,
                Content = content
            };

        [Fact]
        public void Detect_Create_And_Drop_Table()
        {
            // Arrange
            var migration = CreateMigration(
                "20230101010101_AddAndRemoveTable",
                @"
                    migrationBuilder.CreateTable(
                        name: ""Users"",
                        columns: table => new
                        {
                            Id = table.Column<int>(nullable: false)
                                .Annotation(""SqlServer:Identity"", ""1, 1""),
                            Name = table.Column<string>(nullable: true)
                        },
                        constraints: table =>
                        {
                            table.PrimaryKey(""PK_Users"", x => x.Id);
                        });

                    migrationBuilder.DropTable(
                        name: ""ObsoleteTable"");
                ");

            // Act
            var changes = _service.DetectChanges(migration);

            // Assert
            Assert.Contains(changes, c => c.ChangeType == SqlChangeType.CreateTable && c.TableName == "Users");
            Assert.Contains(changes, c => c.ChangeType == SqlChangeType.DropTable && c.TableName == "ObsoleteTable");
            Assert.Equal(2, changes.Count);
        }

        [Fact]
        public void Detect_Add_And_Drop_Column_With_Metadata()
        {
            // Arrange
            var migration = CreateMigration(
                "20230202020202_ColumnChanges",
                @"
                    migrationBuilder.AddColumn<int>(
                        name: ""Age"",
                        table: ""Users"",
                        nullable: false,
                        defaultValue: 0);

                    migrationBuilder.DropColumn(
                        name: ""MiddleName"",
                        table: ""Users"");
                ");

            // Act
            var changes = _service.DetectChanges(migration);

            // Assert
            var addColumn = Assert.Single(changes, c => c.ChangeType == SqlChangeType.AddColumn);
            Assert.Equal("Age", addColumn.ColumnName);
            Assert.Equal("Users", addColumn.TableName);
            Assert.Equal("0", addColumn.DefaultValue);
            Assert.Equal("false", addColumn.GetMetadata("Nullable")); // nullable:false => false

            var dropColumn = Assert.Single(changes, c => c.ChangeType == SqlChangeType.DropColumn);
            Assert.Equal("MiddleName", dropColumn.ColumnName);
            Assert.Equal("Users", dropColumn.TableName);
        }

        [Fact]
        public void Detect_Create_And_Drop_Index()
        {
            // Arrange
            var migration = CreateMigration(
                "20230303030303_IndexChanges",
                @"
                    migrationBuilder.CreateIndex(
                        name: ""IX_Users_Email"",
                        table: ""Users"",
                        column: ""Email"",
                        unique: true);

                    migrationBuilder.DropIndex(
                        name: ""IX_Users_OldIndex"",
                        table: ""Users"");
                ");

            // Act
            var changes = _service.DetectChanges(migration);

            // Assert
            var createIdx = Assert.Single(changes, c => c.ChangeType == SqlChangeType.CreateIndex);
            Assert.Equal("Users", createIdx.TableName);
            Assert.Equal("IX_Users_Email", createIdx.GetMetadata("IndexName"));

            var dropIdx = Assert.Single(changes, c => c.ChangeType == SqlChangeType.DropIndex);
            Assert.Equal("Users", dropIdx.TableName);
            Assert.Equal("IX_Users_OldIndex", dropIdx.GetMetadata("IndexName"));
        }

        [Fact]
        public void Detect_Rename_Table()
        {
            // Arrange
            var migration = CreateMigration(
                "20230404040404_RenameTable",
                @"
                    migrationBuilder.RenameTable(
                        name: ""OldTableName"",
                        newName: ""NewTableName"");
                ");

            // Act
            var changes = _service.DetectChanges(migration);

            // Assert
            var rename = Assert.Single(changes);
            Assert.Equal(SqlChangeType.Rename, rename.ChangeType);
            Assert.Equal("OldTableName", rename.OldValue);
            Assert.Equal("NewTableName", rename.NewValue);
        }

        [Fact]
        public void Detect_Add_And_Drop_ForeignKey()
        {
            // Arrange
            var migration = CreateMigration(
                "20230505050505_FkChanges",
                @"
                    migrationBuilder.AddForeignKey(
                        name: ""FK_Orders_Users_UserId"",
                        table: ""Orders"",
                        column: ""UserId"",
                        principalTable: ""Users"",
                        principalColumn: ""Id"",
                        onDelete: ReferentialAction.Cascade);

                    migrationBuilder.DropForeignKey(
                        name: ""FK_Orders_Users_UserId"",
                        table: ""Orders"");
                ");

            // Act
            var changes = _service.DetectChanges(migration);

            // Assert
            var addFk = Assert.Single(changes, c => c.ChangeType == SqlChangeType.AddForeignKey);
            Assert.Equal("Orders", addFk.TableName);

            var dropFk = Assert.Single(changes, c => c.ChangeType == SqlChangeType.DropForeignKey);
            Assert.Equal("Orders", dropFk.TableName);
        }

        [Fact]
        public void Detect_Raw_Sql_Create_Table()
        {
            // Arrange
            var migration = CreateMigration(
                "20230606060606_RawSql",
                @"
                    migrationBuilder.Sql(
                        ""CREATE TABLE dbo.RawTable (Id INT PRIMARY KEY, Name NVARCHAR(100) NOT NULL)"");
                ");

            // Act
            var changes = _service.DetectChanges(migration);

            // Assert
            var raw = Assert.Single(changes);
            Assert.Equal(SqlChangeType.CreateTable, raw.ChangeType);
            // Table name extraction from raw SQL is not performed; we only verify the type detection.
        }

        [Fact]
        public void Get_Affected_Tables_Returns_Distinct_List()
        {
            // Arrange
            var migration = CreateMigration(
                "20230707070707_MultipleTables",
                @"
                    migrationBuilder.CreateTable(name: ""TableA"", columns: table => new { });
                    migrationBuilder.AddColumn<int>(name: ""Col1"", table: ""TableA"", nullable: true);
                    migrationBuilder.CreateTable(name: ""TableB"", columns: table => new { });
                    migrationBuilder.DropTable(name: ""TableC"");
                ");

            // Act
            var tables = _service.GetAffectedTables(migration);

            // Assert
            var expected = new HashSet<string> { "TableA", "TableB", "TableC" };
            Assert.Equal(expected, new HashSet<string>(tables));
        }
    }
}
