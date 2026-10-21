using EfMigrationDiff.Configuration;
using EfMigrationDiff.Exceptions;
using FluentAssertions;

namespace EfMigrationDiff.Tests;

/// <summary>
/// Tests for <see cref="DiffOptions"/> startup validation: input paths, branch names and ignore-pattern globs.
/// </summary>
public class DiffOptionsTests : IDisposable
{
    private readonly string _root =
        Path.Combine(Path.GetTempPath(), "efmd-diffoptions-" + Guid.NewGuid().ToString("N"));

    public DiffOptionsTests()
    {
        Directory.CreateDirectory(Path.Combine(_root, "Migrations"));
    }

    public void Dispose()
    {
        if (Directory.Exists(_root))
        {
            Directory.Delete(_root, recursive: true);
        }
    }

    private DiffOptions ValidOptions(
        bool requiresMigrations = false,
        string? source = "develop",
        string? target = "main",
        params string[] ignored) => new()
        {
            RepositoryPath = _root,
            MigrationsPath = "Migrations",
            RequiresMigrationsDirectory = requiresMigrations,
            SourceBranch = source,
            TargetBranch = target,
            IgnoredMigrations = ignored
        };

    [Fact]
    public void Validate_ValidOptions_ReturnsNoErrors()
    {
        var errors = ValidOptions(requiresMigrations: true, ignored: new[] { "InitialCreate", "Add*" }).Validate();

        errors.Should().BeEmpty();
    }

    [Fact]
    public void Validate_BlankRepositoryPath_ReportsRequired()
    {
        var errors = new DiffOptions
        {
            RepositoryPath = "   ",
            MigrationsPath = "Migrations",
            SourceBranch = "develop",
            TargetBranch = "main"
        }.Validate();

        errors.Should().ContainSingle().Which.Should().Be("RepositoryPath is required.");
    }

    [Fact]
    public void Validate_MissingRepositoryDirectory_ReportsDoesNotExist()
    {
        var missing = Path.Combine(_root, "does-not-exist");

        var errors = new DiffOptions
        {
            RepositoryPath = missing,
            SourceBranch = "develop",
            TargetBranch = "main"
        }.Validate();

        errors.Should().ContainSingle().Which.Should().Be($"Repository directory does not exist: {missing}");
    }

    [Fact]
    public void Validate_UnreadableRepositoryDirectory_ReportsNotReadable()
    {
        // Root bypasses file permissions, so the directory stays readable there and no error is expected.
        // Permission bits only apply to Linux, so the check is limited to that platform.
        var locked = Path.Combine(_root, "locked");
        Directory.CreateDirectory(locked);
        var canLock = OperatingSystem.IsLinux();
        if (canLock)
        {
            File.SetUnixFileMode(locked, UnixFileMode.None);
        }

        try
        {
            var errors = new DiffOptions
            {
                RepositoryPath = locked,
                SourceBranch = "develop",
                TargetBranch = "main"
            }.Validate();

            if (!canLock || Environment.UserName == "root")
            {
                errors.Should().BeEmpty();
            }
            else
            {
                errors.Should().ContainSingle().Which.Should().StartWith("Repository directory is not readable:");
            }
        }
        finally
        {
            if (canLock)
            {
                File.SetUnixFileMode(locked, UnixFileMode.UserRead | UnixFileMode.UserWrite | UnixFileMode.UserExecute);
            }
        }
    }

    [Fact]
    public void Validate_MissingMigrationsDirectoryWhenRequired_ReportsDoesNotExist()
    {
        var options = ValidOptions(requiresMigrations: true) with { MigrationsPath = "NoSuchMigrations" };

        var errors = options.Validate();

        errors.Should().ContainSingle()
            .Which.Should().Be($"Migrations directory does not exist: {Path.Combine(_root, "NoSuchMigrations")}");
    }

    [Fact]
    public void Validate_MissingMigrationsDirectoryWhenNotRequired_ReportsNoError()
    {
        var options = ValidOptions(requiresMigrations: false) with { MigrationsPath = "NoSuchMigrations" };

        options.Validate().Should().BeEmpty();
    }

    [Fact]
    public void Validate_BlankMigrationsPathWhenRequired_ReportsRequired()
    {
        var options = ValidOptions(requiresMigrations: true) with { MigrationsPath = " " };

        options.Validate().Should().ContainSingle().Which.Should().Be("MigrationsPath is required.");
    }

    [Fact]
    public void Validate_BlankSourceBranch_ReportsError()
    {
        var errors = ValidOptions(source: " ").Validate();

        errors.Should().ContainSingle().Which.Should().Be("Source branch must not be empty.");
    }

    [Fact]
    public void Validate_BlankTargetBranch_ReportsError()
    {
        var errors = ValidOptions(target: "").Validate();

        errors.Should().ContainSingle().Which.Should().Be("Target branch must not be empty.");
    }

    [Fact]
    public void Validate_SourceEqualsTarget_ReportsError()
    {
        var errors = ValidOptions(source: "main", target: "main").Validate();

        errors.Should().ContainSingle()
            .Which.Should().Be("Source and target branch must differ, but both are 'main'.");
    }

    [Fact]
    public void Validate_BranchesDifferingOnlyInCase_ReportsNoError()
    {
        // Git branch names are case-sensitive, so "Main" and "main" are distinct branches.
        var errors = ValidOptions(source: "Main", target: "main").Validate();

        errors.Should().BeEmpty();
    }

    [Fact]
    public void Validate_NullBranches_SkipsBranchRules()
    {
        // Commands that do not compare branches leave both branch names unset.
        var errors = ValidOptions(source: null, target: null).Validate();

        errors.Should().BeEmpty();
    }

    [Theory]
    [InlineData("InitialCreate")]
    [InlineData("Add*")]
    [InlineData("Add?Users")]
    [InlineData("Add[A-Z]*")]
    [InlineData("[!A]*")]
    public void Validate_WellFormedIgnoredGlob_ReportsNoError(string pattern)
    {
        var errors = ValidOptions(ignored: pattern).Validate();

        errors.Should().BeEmpty();
    }

    [Theory]
    [InlineData("Broken[")]
    [InlineData("Empty[]")]
    [InlineData("Reversed[z-a]")]
    [InlineData("Negated[!]")]
    public void Validate_MalformedIgnoredGlob_ReportsPatternThatFailed(string pattern)
    {
        var errors = ValidOptions(ignored: pattern).Validate();

        errors.Should().ContainSingle()
            .Which.Should().StartWith($"IgnoredMigrations pattern '{pattern}' is invalid:");
    }

    [Fact]
    public void Validate_BlankIgnoredPattern_ReportsEmptyPattern()
    {
        var errors = ValidOptions(ignored: "   ").Validate();

        errors.Should().ContainSingle().Which.Should().Be("IgnoredMigrations contains an empty pattern.");
    }

    [Fact]
    public void Validate_SeveralBadIgnoredPatterns_ReportsEachFailingPattern()
    {
        var errors = ValidOptions(ignored: new[] { "Good*", "Broken[", "Empty[]" }).Validate();

        errors.Should().HaveCount(2);
        errors.Should().Contain(e => e.Contains("'Broken['"));
        errors.Should().Contain(e => e.Contains("'Empty[]'"));
    }

    [Fact]
    public void Validate_AllRulesViolated_ReportsEveryError()
    {
        var options = new DiffOptions
        {
            RepositoryPath = Path.Combine(_root, "missing-repo"),
            MigrationsPath = "Migrations",
            RequiresMigrationsDirectory = true,
            SourceBranch = "main",
            TargetBranch = "main",
            IgnoredMigrations = new[] { "Broken[" }
        };

        var errors = options.Validate();

        errors.Should().HaveCount(4);
        errors.Should().Contain(e => e.StartsWith("Repository directory does not exist:"));
        errors.Should().Contain(e => e.StartsWith("Migrations directory does not exist:"));
        errors.Should().Contain(e => e.StartsWith("Source and target branch must differ"));
        errors.Should().Contain(e => e.Contains("'Broken['"));
    }

    [Fact]
    public void ValidateAndThrow_InvalidOptions_ThrowsWithAllViolations()
    {
        var options = ValidOptions(source: "main", target: "main", ignored: "Broken[");

        var act = () => options.ValidateAndThrow();

        act.Should().Throw<ConfigurationValidationException>()
            .Which.Violations.Should().HaveCount(2);
    }

    [Fact]
    public void ValidateAndThrow_ValidOptions_DoesNotThrow()
    {
        var act = () => ValidOptions(requiresMigrations: true).ValidateAndThrow();

        act.Should().NotThrow();
    }
}
