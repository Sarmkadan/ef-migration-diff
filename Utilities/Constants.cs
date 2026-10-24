#nullable enable
namespace EfMigrationDiff.Utilities;

/// <summary>
/// Application-wide constants and configuration values.
/// </summary>
public static class Constants
{
    public const string ApplicationName = "EF Migration Diff";
    public const string ApplicationVersion = "1.0.0";
    public const string Author = "Vladyslav Zaiets";

    public static class Paths
    {
        public const string DefaultMigrationsFolder = "Migrations";
        public const string DefaultReportsFolder = "reports";
        public const string MigrationDesignerSuffix = ".Designer";
        public const string MigrationExtension = ".cs";
    }

    public static class Migration
    {
        public const int TimestampLength = 14;
        public const string TimestampFormat = "yyyyMMddHHmmss";
        public const int MaxNameLength = 255;
        public const int MaxTableNameLength = 128;
        public const int MaxColumnNameLength = 128;
        public const int MaxIndexNameLength = 128;
    }

    public static class Patterns
    {
        public const string MigrationFilePattern = "*.cs";
        public const string MigrationIdPattern = @"^\d{14}_";
        public const string UpMethodPattern = @"protected\s+override\s+void\s+Up\s*\(MigrationBuilder\s+migrationBuilder\)";
        public const string DownMethodPattern = @"protected\s+override\s+void\s+Down\s*\(MigrationBuilder\s+migrationBuilder\)";
    }

    public static class Git
    {
        public const string GitDirectoryName = ".git";
        public const string MainBranch = "main";
        public const string MasterBranch = "master";
        public const string DevelopBranch = "develop";
    }

    public static class ReportFormats
    {
        public const string Text = "text";
        public const string Json = "json";
        public const string Html = "html";
        public const string Xml = "xml";
    }

    public static class OutputMessages
    {
        public const string AnalysisStarted = "Starting migration analysis...";
        public const string AnalysisCompleted = "Analysis completed successfully.";
        public const string ConflictsDetected = "Conflicts detected during analysis.";
        public const string NoConflicts = "No conflicts detected.";
        public const string NoMigrations = "No migrations found.";
    }

    public static class ErrorMessages
    {
        public const string InvalidRepository = "Invalid or not a git repository.";
        public const string MigrationsNotFound = "Migrations directory not found.";
        public const string InvalidMigrationFile = "Invalid migration file format.";
        public const string BranchNotFound = "The specified branch was not found.";
        public const string NoMigrationsInBranch = "No migrations found in the specified branch.";
    }

    public static class SqlStatements
    {
        public const string CreateTableKeyword = "CreateTable";
        public const string DropTableKeyword = "DropTable";
        public const string AddColumnKeyword = "AddColumn";
        public const string DropColumnKeyword = "DropColumn";
        public const string AlterColumnKeyword = "AlterColumn";
        public const string CreateIndexKeyword = "CreateIndex";
        public const string DropIndexKeyword = "DropIndex";
    }

    public static class FileSize
    {
        public const long MaxMigrationFileSize = 10 * 1024 * 1024; // 10 MB
        public const long MaxReportFileSize = 50 * 1024 * 1024;   // 50 MB
    }

    public static class Timeouts
    {
        public const int GitOperationTimeoutMs = 30000;      // 30 seconds
        public const int FileOperationTimeoutMs = 10000;     // 10 seconds
        public const int AnalysisTimeoutMs = 300000;         // 5 minutes
    }

    public static class Concurrency
    {
        public const int DefaultMaxDegreeOfParallelism = 4;
        public const int MinDegreeOfParallelism = 1;
        public const int MaxDegreeOfParallelism = 16;
    }

    /// <summary>
    /// Process exit codes returned by CLI commands. The <c>compare</c> command uses the full contract so CI
    /// pipelines can gate on the result: 0 no changes, 1 non-breaking changes, 2 breaking changes,
    /// 3 conflicts, 64 and above for usage, parse and internal errors.
    /// </summary>
    public static class ExitCodes
    {
        /// <summary>No schema changes and no conflicts between the branches.</summary>
        public const int NoDiff = 0;

        /// <summary>Changes were found, none of them breaking.</summary>
        public const int DiffFound = 1;

        /// <summary>At least one schema change is classified as breaking.</summary>
        public const int BreakingChangesFound = 2;

        /// <summary>At least one blocking conflict was detected between the branches.</summary>
        public const int ConflictsFound = 3;

        /// <summary>Invalid arguments, options, branch names or configuration.</summary>
        public const int UsageError = 64;

        /// <summary>Unexpected internal failure, including migration analysis that throws.</summary>
        public const int Error = 70;
    }
}
