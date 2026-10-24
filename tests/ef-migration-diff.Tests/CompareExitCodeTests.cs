using EfMigrationDiff.CLI;
using EfMigrationDiff.CLI.Commands;
using EfMigrationDiff.Configuration;
using EfMigrationDiff.Utilities;
using FluentAssertions;
using LibGit2Sharp;

namespace EfMigrationDiff.Tests;

/// <summary>
/// Collection that shares one <see cref="CompareRepositoryFixture"/> and runs without parallelization,
/// because the fixture changes the process working directory that the compare command reads.
/// </summary>
[CollectionDefinition(Name, DisableParallelization = true)]
public sealed class CompareExitCodeCollection : ICollectionFixture<CompareRepositoryFixture>
{
    public const string Name = "CompareExitCodes";
}

/// <summary>
/// Builds a throwaway git repository with a shared base migration on <c>main</c> and one branch per scenario.
/// The working directory is switched to the repository for the lifetime of the fixture.
/// </summary>
public sealed class CompareRepositoryFixture : IDisposable
{
    public const string MainBranch = "main";
    public const string NoChangesBranch = "no-changes";
    public const string AdditiveBranch = "additive";
    public const string DestructiveBranch = "destructive";
    public const string ConflictSourceBranch = "conflict-source";
    public const string ConflictTargetBranch = "conflict-target";

    private const string MigrationsDirectory = "Migrations";
    private const string BaseMigrationFile = "20240101000000_Initial.cs";

    private static readonly Signature Author =
        new("ef-migration-diff tests", "tests@example.invalid", DateTimeOffset.UnixEpoch);

    private readonly string _previousDirectory = Environment.CurrentDirectory;
    private readonly string _repositoryPath = Path.Combine(
        Path.GetTempPath(),
        $"ef-migration-diff-compare-{Guid.NewGuid():N}");

    public CompareRepositoryFixture()
    {
        Directory.CreateDirectory(_repositoryPath);
        CreateBranches();
        Environment.CurrentDirectory = _repositoryPath;
    }

    /// <summary>
    /// Creates a service provider from the application composition root for the temporary repository.
    /// Each call returns a fresh provider, because the migration store is a singleton that must not leak between runs.
    /// </summary>
    /// <returns>A provider the caller disposes.</returns>
    public Microsoft.Extensions.DependencyInjection.ServiceProvider CreateServices() =>
        DependencyInjection.CreateServiceProviderWithConfig(_repositoryPath, new EfMigrationDiffOptions());

    public void Dispose()
    {
        Environment.CurrentDirectory = _previousDirectory;
        DeleteRepository();
    }

    private void CreateBranches()
    {
        using var repository = new Repository(Repository.Init(_repositoryPath));

        // Make the first commit land on "main" regardless of the host's init.defaultBranch setting.
        repository.Refs.UpdateTarget("HEAD", $"refs/heads/{MainBranch}");
        CommitMigration(repository, MainBranch, BaseMigrationFile, MigrationSource(
            BaseMigrationFile,
            "migrationBuilder.CreateTable(name: \"Users\", columns: table => new { "
                + "Id = table.Column<int>(nullable: false), Name = table.Column<string>(nullable: true) });"));

        var mainTip = repository.Branches[MainBranch].Tip;

        // Same tip as main: nothing differs between the two branches.
        repository.CreateBranch(NoChangesBranch, mainTip);

        CreateBranchWithMigration(
            repository,
            AdditiveBranch,
            "20240102000000_AddOrders.cs",
            "migrationBuilder.CreateTable(name: \"Orders\", columns: table => new { "
                + "Id = table.Column<int>(nullable: false) });");

        CreateBranchWithMigration(
            repository,
            DestructiveBranch,
            "20240102000100_DropUserName.cs",
            "migrationBuilder.DropColumn(name: \"Name\", table: \"Users\");");

        CreateBranchWithMigration(
            repository,
            ConflictSourceBranch,
            "20240103000000_UsersIntKey.cs",
            "migrationBuilder.CreateTable(name: \"Users\", columns: table => new { "
                + "Id = table.Column<int>(nullable: false) });");

        CreateBranchWithMigration(
            repository,
            ConflictTargetBranch,
            "20240103000100_UsersLongKey.cs",
            "migrationBuilder.CreateTable(name: \"Users\", columns: table => new { "
                + "Id = table.Column<long>(nullable: false) });");

        Commands.Checkout(repository, repository.Branches[MainBranch]);
    }

    private static void CreateBranchWithMigration(
        Repository repository,
        string branchName,
        string fileName,
        string upBody)
    {
        Commands.Checkout(repository, repository.Branches[MainBranch]);
        var branch = repository.CreateBranch(branchName, repository.Branches[MainBranch].Tip);
        Commands.Checkout(repository, branch);
        CommitMigration(repository, branchName, fileName, MigrationSource(fileName, upBody));
    }

    private static void CommitMigration(Repository repository, string branchName, string fileName, string content)
    {
        var migrationsPath = Path.Combine(repository.Info.WorkingDirectory, MigrationsDirectory);
        Directory.CreateDirectory(migrationsPath);
        File.WriteAllText(Path.Combine(migrationsPath, fileName), content);

        Commands.Stage(repository, Path.Combine(MigrationsDirectory, fileName));
        repository.Commit($"add {fileName} on {branchName}", Author, Author);
    }

    private static string MigrationSource(string fileName, string upBody)
    {
        // EF Core file names are "<timestamp>_<Name>.cs"; the class name is the part after the underscore.
        var stem = Path.GetFileNameWithoutExtension(fileName);
        var className = stem[(stem.IndexOf('_') + 1)..];
        return $$"""
            using Microsoft.EntityFrameworkCore.Migrations;

            namespace Sample.Migrations
            {
                public partial class {{className}} : Migration
                {
                    protected override void Up(MigrationBuilder migrationBuilder)
                    {
                        {{upBody}}
                    }
                }
            }
            """;
    }

    private void DeleteRepository()
    {
        if (!Directory.Exists(_repositoryPath))
            return;

        try
        {
            // Git pack and index files can be read-only; clear the attribute so the tree can be removed.
            foreach (var file in Directory.EnumerateFiles(_repositoryPath, "*", SearchOption.AllDirectories))
            {
                File.SetAttributes(file, FileAttributes.Normal);
            }

            Directory.Delete(_repositoryPath, recursive: true);
        }
        catch (IOException)
        {
            // Best-effort cleanup of a temp directory; the OS removes it eventually.
        }
        catch (UnauthorizedAccessException)
        {
            // Same as above: a locked temp file must not fail the test run.
        }
    }
}

/// <summary>
/// Asserts each exit code of the compare command contract by running the command through <see cref="CommandExecutor"/>
/// against a real git repository.
/// </summary>
[Collection(CompareExitCodeCollection.Name)]
public class CompareExitCodeTests
{
    private readonly CompareRepositoryFixture _fixture;

    public CompareExitCodeTests(CompareRepositoryFixture fixture)
    {
        ArgumentNullException.ThrowIfNull(fixture);
        _fixture = fixture;
    }

    [Fact]
    public async Task Compare_IdenticalBranches_ReturnsNoDiffCode()
    {
        var result = await RunCompareAsync(
            CompareRepositoryFixture.NoChangesBranch,
            CompareRepositoryFixture.MainBranch);

        result.ExitCode.Should().Be(Constants.ExitCodes.NoDiff);
        result.Success.Should().BeTrue();
    }

    [Fact]
    public async Task Compare_OnlyNonBreakingChanges_ReturnsDiffFoundCode()
    {
        var result = await RunCompareAsync(
            CompareRepositoryFixture.AdditiveBranch,
            CompareRepositoryFixture.MainBranch);

        result.ExitCode.Should().Be(Constants.ExitCodes.DiffFound);
        result.Success.Should().BeTrue("non-breaking changes are a successful outcome for the gate");
    }

    [Fact]
    public async Task Compare_BreakingSchemaChange_ReturnsBreakingChangesCode()
    {
        var result = await RunCompareAsync(
            CompareRepositoryFixture.DestructiveBranch,
            CompareRepositoryFixture.MainBranch);

        result.ExitCode.Should().Be(Constants.ExitCodes.BreakingChangesFound);
        result.Success.Should().BeFalse();
    }

    [Fact]
    public async Task Compare_BreakingSchemaChangeWithSummary_ReturnsBreakingChangesCode()
    {
        var result = await RunCompareAsync(
            CompareRepositoryFixture.DestructiveBranch,
            CompareRepositoryFixture.MainBranch,
            "--summary");

        result.ExitCode.Should().Be(Constants.ExitCodes.BreakingChangesFound);
    }

    [Fact]
    public async Task Compare_ConflictingBranches_ReturnsConflictsCode()
    {
        var result = await RunCompareAsync(
            CompareRepositoryFixture.ConflictSourceBranch,
            CompareRepositoryFixture.ConflictTargetBranch);

        result.ExitCode.Should().Be(Constants.ExitCodes.ConflictsFound);
        result.Success.Should().BeFalse();
    }

    [Fact]
    public async Task Compare_UnknownBranch_ReturnsUsageErrorCode()
    {
        var result = await RunCompareAsync("does-not-exist", CompareRepositoryFixture.MainBranch);

        result.ExitCode.Should().Be(Constants.ExitCodes.UsageError);
        result.Success.Should().BeFalse();
    }

    [Fact]
    public async Task Compare_UnknownOption_ReturnsUsageErrorCode()
    {
        var result = await RunCompareAsync(
            CompareRepositoryFixture.AdditiveBranch,
            CompareRepositoryFixture.MainBranch,
            "--no-such-option");

        result.ExitCode.Should().Be(Constants.ExitCodes.UsageError);
        result.Success.Should().BeFalse();
    }

    private async Task<CommandResult> RunCompareAsync(params string[] args)
    {
        using var services = _fixture.CreateServices();
        var executor = new CommandExecutor().RegisterCommand("compare", new CompareCommand());
        return await executor.ExecuteAsync("compare", args, services, TextWriter.Null, TextWriter.Null);
    }
}
