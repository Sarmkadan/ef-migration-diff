using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace EfMigrationDiff.Tests.Fixtures.Migrations
{
    /// <inheritdoc />
    public partial class MalformedMigration : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "BadTable",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Name = table.Column<string>(type: "nvarchar(max)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_BadTable", x => x.Id);
                });
        }

        // Missing Down method intentionally - this is a malformed migration
    }
}