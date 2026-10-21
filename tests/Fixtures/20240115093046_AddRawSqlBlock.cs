using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace EfMigrationDiff.Tests.Fixtures.Migrations
{
    /// <inheritdoc />
    public partial class AddRawSqlBlock : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(@"
                CREATE INDEX IX_Users_Email ON Users(Email);

                CREATE TABLE Logs (
                    Id INT IDENTITY(1,1) PRIMARY KEY,
                    Message NVARCHAR(MAX) NOT NULL,
                    CreatedAt DATETIME2 NOT NULL DEFAULT GETUTCDATE()
                );

                ALTER TABLE Users ADD PhoneNumber NVARCHAR(50) NULL;
            ");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(@"
                DROP TABLE Logs;

                ALTER TABLE Users DROP COLUMN PhoneNumber;

                DROP INDEX IX_Users_Email ON Users;
            ");
        }
    }
}