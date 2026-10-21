#nullable enable
using System;
using System.Collections.Generic;
using EfMigrationDiff.Models;
using EfMigrationDiff.Services;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using EfMigrationDiff.Configuration;

namespace TestSqlGen
{
    class Program
    {
        static void Main(string[] args)
        {
            var services = new Microsoft.Extensions.DependencyInjection.ServiceCollection();
            services.AddLogging(builder => builder.AddConsole().SetMinimumLevel(LogLevel.Information));
            services.Configure<EfMigrationDiffOptions>(opts => { });
            services.AddSingleton<SqlGenerator>();

            var provider = services.BuildServiceProvider();
            var generator = provider.GetRequiredService<SqlGenerator>();

            // Create sample schema changes
            var changes = new List<SchemaChange>
            {
                new SchemaChange
                {
                    Id = Guid.NewGuid().ToString(),
                    MigrationId = "20240101000000_CreateUsersTable",
                    ChangeType = SqlChangeType.CreateTable,
                    TableName = "Users",
                    Sql = "[Id] INT IDENTITY(1,1) NOT NULL,\n    [Email] NVARCHAR(256) NOT NULL,\n    [Name] NVARCHAR(100) NOT NULL,\n    [CreatedAt] DATETIME2 NOT NULL DEFAULT GETUTCDATE(),\n    CONSTRAINT [PK_Users] PRIMARY KEY CLUSTERED ([Id] ASC)",
                    LineNumber = 10
                },
                new SchemaChange
                {
                    Id = Guid.NewGuid().ToString(),
                    MigrationId = "20240102000000_AddEmailIndex",
                    ChangeType = SqlChangeType.CreateIndex,
                    TableName = "Users",
                    Sql = "[IX_Users_Email] ON [Users] ([Email])",
                    LineNumber = 15
                },
                new SchemaChange
                {
                    Id = Guid.NewGuid().ToString(),
                    MigrationId = "20240103000000_AddPhoneColumn",
                    ChangeType = SqlChangeType.AddColumn,
                    TableName = "Users",
                    ColumnName = "PhoneNumber",
                    NewValue = "NVARCHAR(20)",
                    DefaultValue = "NULL",
                    LineNumber = 20
                },
                new SchemaChange
                {
                    Id = Guid.NewGuid().ToString(),
                    MigrationId = "20240104000000_RenameEmailColumn",
                    ChangeType = SqlChangeType.Rename,
                    TableName = "Users",
                    OldValue = "Email",
                    NewValue = "EmailAddress",
                    LineNumber = 25
                },
                new SchemaChange
                {
                    Id = Guid.NewGuid().ToString(),
                    MigrationId = "20240105000000_DropPhoneColumn",
                    ChangeType = SqlChangeType.DropColumn,
                    TableName = "Users",
                    ColumnName = "PhoneNumber",
                    LineNumber = 30
                }
            };

            Console.WriteLine("=== Generating SQL Script ===");
            var script = generator.GenerateScript(changes, includeTransaction: true);
            Console.WriteLine(script);

            Console.WriteLine("\n=== Generating Rollback Script ===");
            var rollback = generator.GenerateRollback(changes);
            Console.WriteLine(rollback);

            Console.WriteLine("\n=== Validating Changes ===");
            var errors = generator.ValidateChanges(changes);
            if (errors.Count == 0)
            {
                Console.WriteLine("All changes are valid!");
            }
            else
            {
                foreach (var error in errors)
                {
                    Console.WriteLine($"ERROR: {error}");
                }
            }

            generator.Dispose();
        }
    }
}
